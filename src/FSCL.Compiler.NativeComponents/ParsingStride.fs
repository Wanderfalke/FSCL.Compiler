namespace FSCL.Compiler.NativeComponents.ParsingStride

open System
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Quotations
open FSCL.Compiler

[<Stride("FSCL_PARSING_STRIDE")>] 
type ParsingStride(tm: TypeManager,
                   steps: ICompilerStep list) = 
    inherit CompilerStride<obj, CompilationUnit>(tm, steps)
    
    override this.Run(expr, opt) =
        let mutable result = expr
        let mutable si = 0
        let mutable stop = false
        while not stop && si < steps.Length do
            match steps.[si].Execute(result, this, opt) with
            | ContinueCompilation(r) ->
                result <- r
                si <- si + 1
            | StopCompilation(e) ->
                result <- e
                stop <- true
        if not stop then
            ContinueCompilation(result :?> CompilationUnit)
        else
            StopCompilation(result)
        

        

