module Composition

open FSCL.Compiler
open FSCL.Language

[<ReflectedDefinition>]
let VectorAddCurriedReturn(wi:WorkItemInfo) (a: float32[]) (b:float32[]) =
    let c = Array.zeroCreate<float32> a.Length
    let gid = wi.GlobalID(0)
    c.[gid] <- a.[gid] + b.[gid]
    c
    
[<ReflectedDefinition>]
let VectorMultCurried(wi:WorkItemInfo) (a: float32[]) (b:float32[]) (c:float32[]) =
    let gid = wi.GlobalID(0)
    c.[gid] <- a.[gid] * b.[gid]
    
let Test() =
    let compiler = new Compiler()
    
    let a = Array.create 16 1.0f
    let b = Array.create 16 1.0f
    let c = Array.zeroCreate<float32> 16
    let wi = new WorkSize(16L, 16L)
    
    // Test compilation kernel calls
    let result1 = compiler.Compile(<@ VectorMultCurried wi (VectorAddCurriedReturn wi a b) b c  @>) :?> ICompilationUnit        
    let result2 = compiler.Compile(<@ VectorMultCurried wi (VectorAddCurriedReturn wi (VectorAddCurriedReturn wi a b) b) b c  @>) :?> ICompilationUnit    
    ()