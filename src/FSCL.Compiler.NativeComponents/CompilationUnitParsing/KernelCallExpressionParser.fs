namespace FSCL.Compiler.NativeComponents.ParsingStride.CompilationUnitParsing

open FSCL.Compiler
open FSCL.Compiler.Util
open System.Collections.Generic
open System.Reflection
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Linq.RuntimeHelpers
open FSCL.Language

[<StepProcessor("FSCL_CALL_EXPRESSION_PARSING_PROCESSOR", "FSCL_CU_PARSING_STEP")>]
type KernelCallExpressionParser() =      
    inherit CompilationUnitParsingProcessor()
              
    let rec ParseExpression(e:Expr, 
                            step:CompilationUnitParsingStep, 
                            isRoot:bool) =      
        // First clean possible metadata
        let expr, kernelAttrs, returnAttrs = QuotationAnalysis.ParseKernelMetadata(e)

        // Now check is this is a call to a kernel
        match expr with
        | Patterns.Call (e, mi, a) ->
            match mi with
            | DerivedPatterns.MethodWithReflectedDefinition(body) -> 
                // Kernel Call in the form K(args)
                // Check if Lambda(this, body) for instance methods
                let b = match body with
                        | Patterns.Lambda(v, b) ->
                            if (v.Name = "this") then
                                b
                            else
                                body
                        | _ ->
                            body               
                // Extract parameters vars
                match QuotationAnalysis.GetCurriedOrTupledArgs(b) with
                | Some(paramVars) ->                    
                    QuotationAnalysis.MergeWithStaticKernelMeta(kernelAttrs, returnAttrs, mi)
                    let methodParams = mi.GetParameters()

                    // Generate parameters, arguments and param placeholders   
                    let parameters = new List<ParameterInfo>()
                    let arguments = new List<Expr>()
                    let parameterVars = new List<Var>()    
                    let parameterAttrs = new List<ParamMetaCollection>()
                    let nodeChildren = new Dictionary<string, ICompilationFlowGraphNode>()
                    let mutable workItemInfoArg = None
                    for i = 0 to methodParams.Length - 1 do
                        if methodParams.[i].ParameterType <> typeof<WorkItemInfo> then
                            parameters.Add(methodParams.[i])
                            parameterVars.Add(paramVars.[i])
                                
                            let cleanArg, paramAttrs = QuotationAnalysis.ParseParameterMetadata(a.[i])
                            QuotationAnalysis.MergeWithStaticParameterMeta(paramAttrs, methodParams.[i])
                            arguments.Add(cleanArg)
                            parameterAttrs.Add(paramAttrs)

                            // Recursively parse arguments (may be calls to kernels)
                            let child = step.Process(cleanArg, false)
                            if child.IsSome then
                                nodeChildren.Add(methodParams.[i].Name, child.Value)
                        else
                            workItemInfoArg <- Some(a.[i])
                            
                    // Filter and finalize metadata
                    let finalMeta = step.ProcessMeta(kernelAttrs, returnAttrs, parameterAttrs, new Dictionary<string, obj>())

                    // Create module
                    let kernel = new KernelInfo(mi, parameters |> Seq.toList, parameterVars |> Seq.toList, body, finalMeta, false)
                    let kernelModule = new KernelModule(kernel)

                    // Create flow graph node
                    let node = new KernelFlowGraphNode(kernelModule, workItemInfoArg, arguments |> Seq.toList |> Some, isRoot)
                    for item in nodeChildren do
                        node.Input.Add(item.Key, item.Value)
                    Some(node :> ICompilationFlowGraphNode)
                | _ ->
                    None
            | _ ->
                // Regular function call
                QuotationAnalysis.MergeWithStaticKernelMeta(kernelAttrs, returnAttrs, mi)
                let methodParams = mi.GetParameters()

                // Generate parameters and arguments  
                let parameters = new List<ParameterInfo>()
                let arguments = new List<Expr>()
                let parameterAttrs = new List<ParamMetaCollection>()
                let nodeChildren = new Dictionary<string, ICompilationFlowGraphNode>()
                for i = 0 to methodParams.Length - 1 do
                    parameters.Add(methodParams.[i])
                                
                    let cleanArg, paramAttrs = QuotationAnalysis.ParseParameterMetadata(a.[i])
                    QuotationAnalysis.MergeWithStaticParameterMeta(paramAttrs, methodParams.[i])
                    arguments.Add(cleanArg)
                    parameterAttrs.Add(paramAttrs)

                    // Recursively parse arguments (may be calls to kernels)
                    let child = step.Process(cleanArg, false)
                    if child.IsSome then
                        nodeChildren.Add(methodParams.[i].Name, child.Value)
                            
                // Filter and finalize metadata
                let finalMeta = step.ProcessMeta(kernelAttrs, returnAttrs, parameterAttrs, new Dictionary<string, obj>())

                // Create module
                let f = new RegularFunctionInfo(mi, finalMeta)
                let functionModule = new RegularFunctionModule(f)

                // Create flow graph node
                let node = new RegularFunctionFlowGraphNode(functionModule, e, arguments |> Seq.toList |> Some, isRoot)
                for item in nodeChildren do
                    node.Input.Add(item.Key, item.Value)
                Some(node :> ICompilationFlowGraphNode)
        | _ ->
            None

    override this.Run(o, s, opts) =
        let step = s :?> CompilationUnitParsingStep
        let e, bool = o :?> obj * bool
        if (e :? Expr) then
            ParseExpression(e :?> Expr, step, true)
        else
            None
            