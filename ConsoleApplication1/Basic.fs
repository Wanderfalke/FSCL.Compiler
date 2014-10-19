module Basic

open FSCL.Compiler
open FSCL.Language

[<ReflectedDefinition>]
let VectorAddCurried(wi:WorkItemInfo) (a: float32[]) (b:float32[]) (c:float32[]) =
    let gid = wi.GlobalID(0)
    c.[gid] <- a.[gid] + b.[gid]
    
[<ReflectedDefinition>]
let VectorAddTupled(wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =
    let gid = wi.GlobalID(0)
    c.[gid] <- (a.[gid] + b.[gid]) * a.[gid]
    
[<ReflectedDefinition>]
let VectorAddCurriedReturn(wi:WorkItemInfo) (a: float32[]) (b:float32[]) =
    let c = Array.zeroCreate<float32> (a.Length)
    let gid = wi.GlobalID(0)
    c.[gid] <- a.[gid] + b.[gid]
    c

[<ReflectedDefinition>]
let VectorAddCurriedReturnParam(wi:WorkItemInfo) (a: float32[]) (b:float32[]) (c:float32[]) =
    let gid = wi.GlobalID(0)
    c.[gid] <- a.[gid] + b.[gid]
    c

let Test() =
    let compiler = new Compiler()
    
    let a = Array.create 16 1.0f
    let b = Array.create 16 1.0f
    let c = Array.zeroCreate<float32> 16
    let wi = new WorkSize(16L, 16L)

    // Test compilation kernel references
    let result1 = compiler.Compile(<@ VectorAddTupled @>) :?> ICompilationUnit    
    let result2 = compiler.Compile(<@ VectorAddCurried @>) :?> ICompilationUnit    
    let result3 = compiler.Compile(<@ VectorAddCurriedReturn  @>) :?> ICompilationUnit    
    let result4 = compiler.Compile(<@ VectorAddCurriedReturnParam  @>) :?> ICompilationUnit
    let result5 = compiler.Compile(<@ fun(a:float32[], b:float32[], c:float32[], wi:WorkItemInfo) ->
                                        let gid = wi.GlobalID(0)
                                        c.[gid] <- a.[gid] + b.[gid]
                                   @>) :?> ICompilationUnit
    let result6 = compiler.Compile(<@ fun (a:float32[]) (b:float32[]) (c:float32[]) (wi:WorkItemInfo) ->
                                        let gid = wi.GlobalID(0)
                                        c.[gid] <- a.[gid] + b.[gid]
                                   @>) :?> ICompilationUnit

    // Test compilation kernel calls
    let result7 = compiler.Compile(<@ VectorAddTupled(wi, a, b, c) @>) :?> ICompilationUnit    
    let result8 = compiler.Compile(<@ VectorAddCurried wi a b c  @>) :?> ICompilationUnit    
    let result9 = compiler.Compile(<@ VectorAddCurriedReturn wi a b  @>) :?> ICompilationUnit    
    let result10 = compiler.Compile(<@ VectorAddCurriedReturnParam wi a b c @>) :?> ICompilationUnit
    ()