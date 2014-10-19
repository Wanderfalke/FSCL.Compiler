namespace FSCL.Compiler.NativeComponents.MainStride

open System
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Quotations
open FSCL.Compiler

[<Stride("FSCL_MAIN_STRIDE", Dependencies = [| "FSCL_PARSING_STRIDE" |])>] 
type MainStride(tm: TypeManager,
                steps: ICompilerStep list) = 
    inherit CompilerStride<CompilationUnit, CompilationUnit>(tm, steps)
    
    member private this.FullCompile(kmod: KernelModule, opt) =
        // Full compile
        let mutable si = 0
        let mutable stop = false
        let mutable result = kmod :> obj
        while not stop && si < steps.Length do
            match steps.[si].Execute(result, this, opt) with
            | ContinueCompilation(r) ->
                result <- r
                si <- si + 1
            | StopCompilation(e) ->
                result <- e
                stop <- true
        result, stop

    override this.Run(cu, opt) =
        let cache = opt.[CompilerOptions.UseCache] :?> CompilerCache
        let mutable globalResult = ContinueCompilation(cu)
        
        if cache <> null then
            for i = 0 to cu.KernelModules.Count - 1 do
                let kmod = cu.KernelModules.[i] :?> KernelModule
                // Check if cache has a compatible module
                match cache.TryFindCompatibleOpenCLCachedKernel(kmod.Kernel.ID, kmod.Meta) with
                | Some(compatibleMod) ->
                    compatibleMod.CloneTo(kmod)
                | _ ->
                    let result, stop = this.FullCompile(kmod, opt)
                    if not stop then
                        // Replace module in KernelModules
                        cu.KernelModules.[i] <- result :?> KernelModule
                        // Cache this module
                        if not(cache.Kernels.ContainsKey(kmod.Kernel.ID)) then
                            cache.Kernels.Add(kmod.Kernel.ID, new List<KernelModule>())
                        cache.Kernels.[kmod.Kernel.ID].Add(result :?> KernelModule)
                    else
                        // Replace module in KernelModules
                        cu.KernelModules.[i] <- result :?> KernelModule
                        // Do not cache this module (may be incomplete)
                        globalResult <- StopCompilation(cu)
            else
                // Not using cache
                for i = 0 to cu.KernelModules.Count - 1 do
                    let kmod = cu.KernelModules.[i] :?> KernelModule
                    let result, stop = this.FullCompile(kmod, opt)
                    // Replace module in KernelModules
                    if not stop then
                        // Replace module in KernelModules
                        cu.KernelModules.[i] <- result :?> KernelModule
                    else
                        // Replace module in KernelModules
                        cu.KernelModules.[i] <- result :?> KernelModule
                        // Do not cache this module (may be incomplete)
                        globalResult <- StopCompilation(cu)

        globalResult
        

        

