namespace FSCL.Compiler.NativeComponents.ParsingStride.CompilationUnitParsing

open System
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Quotations
open FSCL.Compiler

[<Step("FSCL_CU_PARSING_STEP", "FSCL_PARSING_STRIDE")>] 
type CompilationUnitParsingStep(tm: TypeManager,
                                procs: ICompilerStepProcessor list) = 
    inherit CompilerStep<obj, CompilationUnit>(tm, procs)
    let mutable (compilerCache:CompilerCache) = null

    let parsingProcessors = List.map(fun (p:ICompilerStepProcessor) ->
                                        p :?> CompilationUnitParsingProcessor) (List.filter (fun (p:ICompilerStepProcessor) -> 
                                                                                 typeof<CompilationUnitParsingProcessor>.IsAssignableFrom(p.GetType())) procs)
    let metadataProcessors = List.map(fun (p:ICompilerStepProcessor) ->
                                        p :?> MetadataFinalizerProcessor) (List.filter (fun (p:ICompilerStepProcessor) -> 
                                                                            typeof<MetadataFinalizerProcessor>.IsAssignableFrom(p.GetType())) procs)
    let mutable opts = null
                 
    member this.CompilerCache
        with get() =
            compilerCache
        and private set(v) =
            compilerCache <- v

    member this.Process(expr:obj, isRoot:bool) =
        let mutable index = 0
        let mutable output = None
        while (output.IsNone) && (index < parsingProcessors.Length) do
            output <- parsingProcessors.[index].Execute((box expr, isRoot), this, opts) :?> ICompilationFlowGraphNode option
            index <- index + 1
        output
            
    member this.ProcessMeta(kmeta: KernelMetaCollection, rmeta: ParamMetaCollection, pmeta: List<ParamMetaCollection>, parsingInfo: Dictionary<string, obj>) =
        let mutable output = (kmeta, rmeta, pmeta)
        for p in metadataProcessors do
            output <- p.Execute((kmeta, rmeta, pmeta, parsingInfo), this, opts) :?> KernelMetaCollection * ParamMetaCollection * List<ParamMetaCollection>
        ReadOnlyMetaCollection(kmeta, rmeta, pmeta)

    override this.Run(expr, stride, opt) =
        opts <- opt
        this.CompilerCache <- opts.[CompilerOptions.UseCache] :?> CompilerCache
        let parsingResult = this.Process(expr, true)
        match parsingResult with
        | Some(fg) ->
            if opts.ContainsKey(CompilerOptions.ParseOnly) then
                StopCompilation(new CompilationUnit(fg))
            else
                ContinueCompilation(new CompilationUnit(fg))
        | _ ->
            raise (new CompilerException("Cannot parse expression " + expr.ToString()))
        

        

