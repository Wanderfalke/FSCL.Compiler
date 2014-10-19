namespace FSCL.Compiler.NativeComponents.MainStride.ModulePreprocessing

open System
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Quotations
open FSCL.Compiler


[<assembly:DefaultComponentAssembly>]
do()

[<Step("FSCL_MODULE_PREPROCESSING_STEP",
       "FSCL_MAIN_STRIDE")>]
type ModulePreprocessingStep(tm: TypeManager,
                             processors: ICompilerStepProcessor list) = 
    inherit CompilerStep<KernelModule, KernelModule>(tm, processors)
           
    member private this.Process(km, opts) =
        for p in processors do
            p.Execute(km, this, opts) |> ignore
        km

    override this.Run(data, stride, opts) =
        let r = ContinueCompilation(this.Process(data, opts))
        r

        

