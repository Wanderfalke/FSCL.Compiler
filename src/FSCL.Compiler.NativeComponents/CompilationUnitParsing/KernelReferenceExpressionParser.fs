namespace FSCL.Compiler.NativeComponents.ParsingStride.CompilationUnitParsing

open System
open FSCL.Compiler
open FSCL.Compiler.Util
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Quotations
open FSCL.Language
open FSCL

[<assembly:DefaultComponentAssembly>]
do()

[<StepProcessor("FSCL_REFERENCE_PARSING_PROCESSOR", 
                "FSCL_CU_PARSING_STEP", 
                Dependencies = [| "FSCL_CALL_EXPRESSION_PARSING_PROCESSOR" |])>]
type KernelReferenceParser() =      
    inherit CompilationUnitParsingProcessor()
    
    override this.Run(o, s, opts) =
        let step = s :?> CompilationUnitParsingStep
        let expr, isRoot = o :?> obj * bool
        if (expr :? Expr) then
            match QuotationAnalysis.GetKernelFromName(expr :?> Expr) with
            | Some(mi, paramInfo, paramVars, b, kMeta, rMeta, pMeta) ->              
                // Filter and finalize metadata
                let finalMeta = step.ProcessMeta(kMeta, rMeta, pMeta, null)

                let kernelModule = new KernelModule(new KernelInfo(mi, paramInfo, paramVars, b, finalMeta, false))
                
                // Create module
                let kernelNode = new KernelFlowGraphNode(kernelModule, None, None, isRoot)                
                Some(kernelNode :> ICompilationFlowGraphNode)
            | _ ->
                None
        else
            None
            
[<StepProcessor("FSCL_LAMBDA_PARSING_PROCESSOR", 
                "FSCL_CU_PARSING_STEP", 
                Dependencies = [| "FSCL_REFERENCE_PARSING_PROCESSOR" |])>]
type KernelLambdaParser() =      
    inherit CompilationUnitParsingProcessor()
        
    override this.Run(o, s, opts) =
        let step = s :?> CompilationUnitParsingStep
        let mi, isRoot = o :?> obj * bool
        if (mi :? Expr) then
            match QuotationAnalysis.LambdaToMethod(mi :?> Expr, true) with
            | Some(mi, paramInfo, paramVars, b, kMeta, rMeta, pMeta) -> 
                // Filter and finalize metadata
                let finalMeta = step.ProcessMeta(kMeta, rMeta, pMeta, null)

                // Create signleton kernel call graph
                let kernelModule = new KernelModule(new KernelInfo(mi, paramInfo, paramVars, b, finalMeta, true))
                let kernelNode = new KernelFlowGraphNode(kernelModule, None, None, isRoot)
                
                Some(kernelNode :> ICompilationFlowGraphNode)
            | _ ->
                None
        else
            None
            
[<StepProcessor("FSCL_METHOD_INFO_PARSING_PROCESSOR", 
                "FSCL_CU_PARSING_STEP")>]
type KernelMethodInfoParser() =      
    inherit CompilationUnitParsingProcessor() 
        
    override this.Run(o, s, opts) =
        let step = s :?> CompilationUnitParsingStep
        let mi, isRoot = o :?> obj * bool
        if (mi :? MethodInfo) then
            match QuotationAnalysis.GetKernelFromMethodInfo(mi :?> MethodInfo) with
            | Some(mi, paramInfo, paramVars, b, kMeta, rMeta, pMeta) -> 
                // Filter and finalize metadata
                let finalMeta = step.ProcessMeta(kMeta, rMeta, pMeta, null)

                // Create singleton kernel call graph
                let kernelModule = new KernelModule(new KernelInfo(mi, paramInfo, paramVars, b, finalMeta, false))
                let kernelNode = new KernelFlowGraphNode(kernelModule, None, None, isRoot)
                
                Some(kernelNode :> ICompilationFlowGraphNode)
            | _ ->
                None
        else
            None
            