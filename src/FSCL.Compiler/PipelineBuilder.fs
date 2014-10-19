namespace FSCL.Compiler.Configuration

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open FSCL.Compiler
open System.Reflection
open GraphUtil

exception PipelineBuildException of string
    
type internal PipelineBuilder() =
    static member Build(conf: PipelineConfiguration) =
        let typeHandlers = new Dictionary<string, TypeHandlerConfiguration>()
        let strides = new Dictionary<string, StrideConfiguration>()
        let steps = new Dictionary<string, StepConfiguration>()
        let processors = new Dictionary<string, StepProcessorConfiguration>()
        
        let structure = new Dictionary<string, Dictionary<string, List<string>>>()

        // Explode sources and group by component type (conf must be explicit)
        for s in conf.Sources do
            for th in s.TypeHandlers do
                if not (typeHandlers.ContainsKey(th.ID)) then
                    typeHandlers.Add(th.ID, th)
            for st in s.Strides do
                if not (strides.ContainsKey(st.ID)) then
                    strides.Add(st.ID, st)
            for st in s.Steps do
                if not (steps.ContainsKey(st.ID)) then
                    steps.Add(st.ID, st)
            for sp in s.StepProcessors do
                if not (processors.ContainsKey(sp.ID)) then
                    processors.Add(sp.ID, sp)

        // Create hierarchical structure
        for stride in strides do
            structure.Add(stride.Key, new Dictionary<string, List<String>>())
        for step in steps do
            structure.[step.Value.Stride].Add(step.Key, new List<String>())
        for proc in processors do
            structure.[steps.[proc.Value.Step].Stride].[proc.Value.Step].Add(proc.Key)
                    
        // Create graph of type handlers
        let thGraph = new Graph<TypeHandlerConfiguration, string>()
        for t in typeHandlers do 
            thGraph.Add(t.Key, t.Value) |> ignore
        for t in typeHandlers do 
            for d in t.Value.Dependencies do
                thGraph.Connect(d, t.Value.ID)
            for d in t.Value.Before do
                if typeHandlers.ContainsKey(d) then
                    thGraph.Connect(t.Value.ID, d)
        let sortedTypeHandlers = thGraph.Sorted.Value

        // Build type handlers and type manager
        let th = seq { 
                        for s,t in sortedTypeHandlers do 
                            yield t.Type.GetConstructor([||]).Invoke([||]) :?> TypeHandler 
                     }
        let tm = new TypeManager(List.ofSeq th)
                    
        // Collect metadata affecting compilation
        let usedMetadata = new Dictionary<Type, MetadataComparer list>()
        
        // Check that each stride has required trides
        for s in strides do
            for ty, comp in s.Value.UsedMetadata do
                if not (usedMetadata.ContainsKey(ty)) then
                    usedMetadata.Add(ty, [ comp ])
                else
                    usedMetadata.[ty] <- usedMetadata.[ty] @ [ comp ]

            for rs in s.Value.Dependencies do
                if not (strides.ContainsKey(rs)) then
                    raise (PipelineBuildException("The stride " + s.Key + " requires stride " + rs + " but this stride has not been found"))
        
        // Check that each step has required steps
        for s in steps do
            for ty, comp in s.Value.UsedMetadata do
                if not (usedMetadata.ContainsKey(ty)) then
                    usedMetadata.Add(ty, [ comp ])
                else
                    usedMetadata.[ty] <- usedMetadata.[ty] @ [ comp ]
            
            for rs in s.Value.Dependencies do
                if not (steps.ContainsKey(rs)) then
                    raise (PipelineBuildException("The step " + s.Key + " requires step " + rs + " but this step has not been found"))
        
        // Check that each processors has and owner step and a before/after processor
        for p in processors do
            for ty, comp in p.Value.UsedMetadata do
                if not (usedMetadata.ContainsKey(ty)) then
                    usedMetadata.Add(ty, [ comp ])
                else
                    usedMetadata.[ty] <- usedMetadata.[ty] @ [ comp ]

            for dep in p.Value.Dependencies do
                if not (processors.ContainsKey(dep)) then
                    raise (PipelineBuildException("The processor " + p.Key + " requires processor " + dep + " but this step has not been found"))
       
        // Create graph of strides
        let stridegraph = new Graph<StrideConfiguration, string>()
        for s in strides do
            stridegraph.Add(s.Key, s.Value) |> ignore
        for s in strides do              
            for d in s.Value.Dependencies do
                stridegraph.Connect(d, s.Value.ID)
            for d in s.Value.Before do
                if steps.ContainsKey(d) then
                    stridegraph.Connect(s.Value.ID, d)
                    
        // Sort strides graph topological
        let sorted = stridegraph.Sorted
        if sorted.IsNone then
            raise (PipelineBuildException("Cannot build the strides since there is a cycle in strides dependencies"))
                                        
        // Foreach stride, create graph of steps and processors
        let strides = seq {
            for (strideId, strideData) in sorted.Value do
                let stepGraph = new Graph<StepConfiguration, string>()
                for step in structure.[strideId] do
                    stepGraph.Add(step.Key, steps.[step.Key]) |> ignore
                for step in structure.[strideId] do
                    for d in steps.[step.Key].Dependencies do
                        stepGraph.Connect(d, step.Key)
                    for d in steps.[step.Key].Before do
                        if steps.ContainsKey(d) then
                            stepGraph.Connect(step.Key, d)

                // Sort steps graph topological
                let sorted = stepGraph.Sorted
                if sorted.IsNone then
                    raise (PipelineBuildException("Cannot build the stride " + strideId + " using the specified steps since there is a cycle in steps dependencies"))
                
                let steps = seq {
                    // Consider steps in the topological order
                    for (stepId, stepData) in sorted.Value do
                        let procGraph = new Graph<StepProcessorConfiguration, string>()
                        for proc in structure.[strideId].[stepId] do
                            procGraph.Add(proc, processors.[proc]) |> ignore
                        for proc in structure.[strideId].[stepId] do
                            for d in processors.[proc].Dependencies do
                                procGraph.Connect(d, proc)
                            for d in processors.[proc].Before do
                                if processors.ContainsKey(d) then
                                    procGraph.Connect(proc, d)

                        // Sort proc graph topological
                        let sorted = procGraph.Sorted
                        if sorted.IsNone then
                            raise (PipelineBuildException("Cannot build the step " + stepId + " using the specified processors since there is a cycle in processors dependencies"))
                        
                        // Instantiate a list of proper processors via reflection
                        let processors = new List<ICompilerStepProcessor>()
                        for (id, procNode) in sorted.Value do
                            processors.Add(procNode.Type.GetConstructor([||]).Invoke([||]) :?> ICompilerStepProcessor) |> ignore
                
                        // Build step
                        yield stepData.Type.GetConstructors().[0].Invoke([| tm; List.ofSeq(processors) |]) :?> ICompilerStep
                }
                yield strideData.Type.GetConstructors().[0].Invoke([| tm; List.ofSeq(steps) |]) :?> ICompilerStride
            }
        
        let flatStrides = Array.ofSeq(strides)
        flatStrides, usedMetadata
      
            
        
        




