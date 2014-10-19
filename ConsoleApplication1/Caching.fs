module Caching

open FSCL
open FSCL.Compiler
open FSCL.Language
open System.Diagnostics
open System

[<ReflectedDefinition>]
let VectorAddCurried(wi:WorkItemInfo) (a: float32[]) (b:float32[]) (c:float32[]) =
    let gid = wi.GlobalID(0)
    c.[gid] <- a.[gid] + b.[gid]
    
[<ReflectedDefinition>]
let VectorAddTupled(wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =
    let gid = wi.GlobalID(0)
    c.[gid] <- a.[gid] + b.[gid]
    
let Test() =
    let compiler = new Compiler()
    
    let a = Array.create 16 1.0f
    let b = Array.create 16 1.0f
    let c = Array.zeroCreate<float32> 16
    let wi = new WorkSize(16L, 16L)

    let watch = new Stopwatch()

    // Test cached compilation of kernel reference
    compiler.Compile(<@ VectorAddTupled @>) |> ignore
    watch.Start()
    for i = 1 to 10 do
        compiler.Compile(<@ DEVICE_TYPE(DeviceType.Accelerator, VectorAddTupled) @>) |> ignore
    watch.Stop()
    Console.WriteLine("No caching: " + ((double)(watch.ElapsedMilliseconds)/10.0).ToString() + " ms")    
    watch.Restart()
    for i = 1 to 10 do
        compiler.Compile(<@ VectorAddTupled @>, (CompilerOptions.UseCache, null)) |> ignore
    watch.Stop()
    Console.WriteLine("Force no caching: " + ((double)(watch.ElapsedMilliseconds)/10.0).ToString() + " ms")    
    watch.Restart()
    for i = 1 to 100 do
        compiler.Compile(<@ VectorAddTupled @>) |> ignore
    watch.Restart()
    Console.WriteLine("Caching: " + ((double)(watch.ElapsedMilliseconds)/10.0).ToString() + " ms")
        
    compiler.Compile(<@ VectorAddCurried @>) |> ignore
    watch.Restart()
    for i = 1 to 100 do
        compiler.Compile(<@ DEVICE_TYPE(DeviceType.Accelerator, VectorAddCurried) @>) |> ignore
    watch.Stop()
    Console.WriteLine("No caching: " + ((double)(watch.ElapsedMilliseconds)/10.0).ToString() + " ms")
    watch.Restart()
    for i = 1 to 100 do
        compiler.Compile(<@ VectorAddCurried @>, (CompilerOptions.UseCache, null)) |> ignore
    watch.Stop()
    Console.WriteLine("Force no caching: " + ((double)(watch.ElapsedMilliseconds)/10.0).ToString() + " ms")    
    watch.Restart()
    for i = 1 to 100 do
        compiler.Compile(<@ VectorAddCurried @>) |> ignore
    watch.Stop()
    Console.WriteLine("Caching: " + ((double)(watch.ElapsedMilliseconds)/10.0).ToString() + " ms")

    ()