namespace FSCL.Compiler.Configuration

open System
open System.IO
open System.Reflection
open FSCL.Compiler
open System.Collections.Generic
open System.Xml
open System.Xml.Linq
open Microsoft.FSharp.Reflection
open System.Reflection.Emit

///
///<summary>
///Kind of compiler source: assembly object or file path
///</summary>
///
type ComponentSource =
| AssemblySource of Assembly
| FileSource of string

///
///<summary>
///Configuration of a compiler step
///</summary>
///<remarks>
///A step configuration contains all the information about a step that must be provided when the step is specified in a configuration file or object
///</remarks>
///
type ComponentConfiguration(i: string, t: Type, dependencies: string array, before: string array, metadata: (Type * MetadataComparer) array) =
    ///
    ///<summary>
    ///The ID of the step
    ///</summary>
    ///
    new() = 
        new ComponentConfiguration("", typeof<Object>, [||], [||], [||])

    member val ID = i with get
    ///
    ///<summary>
    ///The step dependencies. See <see cref="FSCL.Compiler.StepAttribute"/>
    ///</summary>
    ///
    member val Dependencies = dependencies
    ///
    ///<summary>
    ///The set of steps that must be executed after this
    ///</summary>
    ///
    member val Before = before
    ///
    ///<summary>
    ///The set of metadata that affect the result of this step
    ///</summary>
    /// 
    member val UsedMetadata = metadata
    ///
    ///<summary>
    ///The runtime .NET type of the step
    ///</summary>
    ///
    member val Type = t with get
    member this.ToXml() =
        let el = new XElement(XName.Get(this.GetType().Name),
                    new XAttribute(XName.Get("ID"), this.ID),
                    new XAttribute(XName.Get("Type"), this.Type.FullName),
                    new XElement(XName.Get("Dependencies"),
                        Array.ofSeq(seq {
                            for item in this.Dependencies do
                                yield XElement(XName.Get("Item"), XAttribute(XName.Get("ID"), item))
                        })),
                    new XElement(XName.Get("Before"),
                        Array.ofSeq(seq {
                            for item in this.Before do
                                yield XElement(XName.Get("Item"), XAttribute(XName.Get("ID"), item))
                        })),                        
                    new XElement(XName.Get("UsedMetadata"),
                        Array.ofSeq(seq {
                            for item in this.UsedMetadata do
                                yield XElement(XName.Get("Item"),
                                    XAttribute(XName.Get("Type"), fst(item).ToString()),
                                    XAttribute(XName.Get("Comparer"), snd(item).GetType().ToString()))
                        }))) 
        el
    static member internal FromXml(el:XElement, a:Assembly) =
        let deps = List<string>()
        let bef = List<string>()
        let meta = List<Type * MetadataComparer>()
        for d in el.Elements(XName.Get("Dependencies")) do
            for item in d.Elements(XName.Get("Item")) do
                deps.Add(item.Attribute(XName.Get("ID")).Value)
        for d in el.Elements(XName.Get("Before")) do
            for item in d.Elements(XName.Get("Item")) do
                bef.Add(item.Attribute(XName.Get("ID")).Value)
        for d in el.Elements(XName.Get("UsedMetadata")) do
            for item in d.Elements(XName.Get("Item")) do
                let metaType, comparerType = item.Attribute(XName.Get("Type")).Value, item.Attribute(XName.Get("Comparer")).Value
                let asm = Assembly.GetExecutingAssembly();
                let metaT = asm.GetType(metaType);
                let comparerT = asm.GetType(comparerType)
                meta.Add(metaT, Activator.CreateInstance(comparerT) :?> MetadataComparer) 
        ComponentConfiguration(el.Attribute(XName.Get("ID")).Value,
                              a.GetType(el.Attribute(XName.Get("Type")).Value), 
                              deps.ToArray(), bef.ToArray(), meta.ToArray())

    override this.Equals(o) =
        if o.GetType() <> this.GetType() then
            false
        else
            let oth = o :?> ComponentConfiguration
            let equal = ref(this.ID = oth.ID && this.Type = oth.Type && this.Dependencies.Length = oth.Dependencies.Length && this.Before.Length = oth.Before.Length && this.UsedMetadata.Length = oth.UsedMetadata.Length)
            if !equal then
                Array.iter (fun (item1:string) ->
                    equal := !equal && (Array.tryFind(fun (item2:string) ->
                                        item1 = item2) oth.Dependencies).IsSome) this.Dependencies
            if !equal then
                Array.iter (fun (item1:string) ->
                    equal := !equal && (Array.tryFind(fun (item2:string) ->
                                        item1 = item2) oth.Before).IsSome) this.Before
            !equal
    override this.GetHashCode() =
        let depString = this.Dependencies |> String.concat ";"
        let befString = this.Before |> String.concat ";"
        this.Type.GetHashCode() + this.ID.GetHashCode() + depString.GetHashCode() + befString.GetHashCode()
    
///
///<summary>
///Configuration of a compiler step
///</summary>
///<remarks>
///A step configuration contains all the information about a step that must be provided when the step is specified in a configuration file or object
///</remarks>
///
type StrideConfiguration(i: string, t: Type, dependencies: string array, before: string array, metadata: (Type * MetadataComparer) array) =
    inherit ComponentConfiguration(i, t, dependencies, before, metadata)
    ///
    ///<summary>
    ///The ID of the step
    ///</summary>
    ///
    new() = 
        new StrideConfiguration("", typeof<Object>, [||], [||], [||])
    static member internal FromXml(el:XElement, a:Assembly) =
        let comp = ComponentConfiguration.FromXml(el, a)
        StrideConfiguration(comp.ID,
                            comp.Type, 
                            comp.Dependencies, comp.Before, comp.UsedMetadata)

///
///<summary>
///Configuration of a compiler step
///</summary>
///<remarks>
///A step configuration contains all the information about a step that must be provided when the step is specified in a configuration file or object
///</remarks>
///
type StepConfiguration(i: string, stride: string, t: Type, dependencies: string array, before: string array, metadata: (Type * MetadataComparer) array) =
    inherit ComponentConfiguration(i, t, dependencies, before, metadata)
    ///
    ///<summary>
    ///The ID of the step
    ///</summary>
    ///
    new() = 
        new StepConfiguration("", "", typeof<Object>, [||], [||], [||])

    member val Stride = stride with get
    member internal this.ToXml() =
        let el = base.ToXml()
        el.Add(new XAttribute(XName.Get("Stride"), this.Stride))
        el
    static member internal FromXml(el:XElement, a:Assembly) =
        let comp = ComponentConfiguration.FromXml(el, a)
        StepConfiguration(comp.ID,
                          el.Attribute(XName.Get("Stride")).Value, 
                          comp.Type, 
                          comp.Dependencies, comp.Before, comp.UsedMetadata)

    override this.Equals(o) =
        base.Equals(o) && (o :? StepConfiguration) && (this.Stride = (o :?> StepConfiguration).Stride)

    override this.GetHashCode() =
        base.GetHashCode() + this.Stride.GetHashCode()
            
///
///<summary>
///Configuration of a compiler step processor
///</summary>
///
type StepProcessorConfiguration(i: string, step: string, t: Type, dependencies, before, metadata: (Type * MetadataComparer) array) =
    inherit ComponentConfiguration(i, t, dependencies, before, metadata)
    ///
    ///<summary>
    ///The ID of the step
    ///</summary>
    ///
    new() = 
        new StepProcessorConfiguration("", "", typeof<Object>, [||], [||], [||])

    member val Step = step with get
    member this.ToXml() =
        let el = base.ToXml()
        el.Add(new XAttribute(XName.Get("Step"), this.Step))
        el
    static member internal FromXml(el:XElement, a:Assembly) =
        let comp = ComponentConfiguration.FromXml(el, a)
        StepProcessorConfiguration(comp.ID,
                                   el.Attribute(XName.Get("Step")).Value, 
                                   comp.Type, 
                                   comp.Dependencies, comp.Before, comp.UsedMetadata)

    override this.Equals(o) =
        base.Equals(o) && (o :? StepProcessorConfiguration) && (this.Step = (o :?> StepProcessorConfiguration).Step)

    override this.GetHashCode() =
        base.GetHashCode() + this.Step.GetHashCode()

///
///<summary>
///Configuration of a type handler
///</summary>
///
type TypeHandlerConfiguration(i:string, t:Type, dependencies, before: string array) =
    inherit ComponentConfiguration(i, t, dependencies, before, [||])
    static member internal FromXml(el:XElement, a:Assembly) =
        let comp = ComponentConfiguration.FromXml(el, a)
        TypeHandlerConfiguration(comp.ID,
                                 comp.Type, 
                                 comp.Dependencies, comp.Before)
        
///
///<summary>
///Configuration of a compiler source, which is a set of steps, processors and type handlers contained to the same assembly
///</summary>
///
type SourceConfiguration(src: ComponentSource, 
                         th: TypeHandlerConfiguration array, 
                         sr: StrideConfiguration array, 
                         st: StepConfiguration array, 
                         sp: StepProcessorConfiguration array) =     
    ///
    ///<summary>
    ///The path of the source file or the instance of the source assembly
    ///</summary>
    ///
    member val Source = 
        match src with
        | AssemblySource(a) ->
            AssemblySource(a)
        | FileSource(s) ->
            if (Path.IsPathRooted(s)) then
                FileSource(s)
            else
               FileSource(Path.Combine(Directory.GetCurrentDirectory(), s))               
    ///
    ///<summary>
    ///The set of configurations of type handlers
    ///</summary>
    ///
    member val TypeHandlers = th with get
    ///
    ///<summary>
    ///The set of configurations of steps
    ///</summary>
    ///
    member val Strides = sr with get
    ///
    ///<summary>
    ///The set of configurations of steps
    ///</summary>
    ///
    member val Steps = st with get
    ///
    ///<summary>
    ///The set of configurations of step processors
    ///</summary>
    ///
    member val StepProcessors = sp with get    
    ///
    ///<summary>
    ///Whether this source is explicit or not
    ///</summary>
    ///<remarks>
    ///An explicit source is a source where all the components in the assembly/file and all the related information are explicitated. 
    ///In an implicit source the set of steps, processors and type handlers are instead to be found and parsed through reflection,
    ///investigating the types declared in the assembly and their eventual step, processor or type handler attributes.
    ///</remarks>
    ///
    member this.IsExplicit
        with get() =
            (this.TypeHandlers.Length > 0)  || (this.Strides.Length > 0) || (this.Steps.Length > 0) || (this.StepProcessors.Length > 0)
            
    member internal this.IsDefault 
        with get() =
            match this.Source with
            | FileSource(s) ->
                let a = Assembly.LoadFile(s)
                a.GetCustomAttribute<DefaultComponentAssembly>() <> null
            | AssemblySource(a) ->
                a.GetCustomAttribute<DefaultComponentAssembly>() <> null     
    ///
    ///<summary>
    ///Constructor to instantiate an implicit compiler source
    ///</summary>
    ///
    new(src: ComponentSource) = 
        SourceConfiguration(src, [||], [||], [||], [||])

    member internal this.ToXml() =
        let el = new XElement(XName.Get(this.GetType().Name),
                    match this.Source with
                    | FileSource(s) ->
                        XAttribute(XName.Get("FileSource"), s)
                    | AssemblySource(s) ->
                        XAttribute(XName.Get("AssemblySource"), s.FullName))
        if (this.IsExplicit) then
            el.Add(new XElement(XName.Get("Components"),
                        new XElement(XName.Get("TypeHandlers"), 
                            Array.ofSeq(seq {
                                for item in this.TypeHandlers do
                                    yield item.ToXml()
                            })),
                        new XElement(XName.Get("Strides"), 
                            Array.ofSeq(seq {
                                for item in this.Strides do
                                    yield item.ToXml()
                            })),
                        new XElement(XName.Get("Steps"), 
                            Array.ofSeq(seq {
                                for item in this.Steps do
                                    yield item.ToXml()
                            })),
                        new XElement(XName.Get("StepProcessors"),
                            Array.ofSeq(seq {
                                for item in this.StepProcessors do
                                    yield item.ToXml()
                            }))))
        el
        
    static member internal FromXml(el: XElement, srcRoot: string) =
        let mutable source = FileSource("")
        let mutable assembly = Assembly.GetExecutingAssembly()
        let root = srcRoot

        // Determine source type (file or assembly name) and load assembly
        if (el.Attribute(XName.Get("AssemblySource")) <> null) then
            assembly <- Assembly.Load(el.Attribute(XName.Get("AssemblySource")).Value)
            source <- AssemblySource(assembly)
        else            
            if Path.IsPathRooted(el.Attribute(XName.Get("FileSource")).Value) then
                source <- FileSource(el.Attribute(XName.Get("FileSource")).Value)
                assembly <- Assembly.LoadFile(el.Attribute(XName.Get("FileSource")).Value)
            else
                source <- FileSource(Path.Combine(root, el.Attribute(XName.Get("FileSource")).Value))
                assembly <- Assembly.LoadFile(Path.Combine(root, el.Attribute(XName.Get("FileSource")).Value))

        // Check if explicit or implicit
        if (el.Element(XName.Get("Components")) <> null) then
            let th = new List<TypeHandlerConfiguration>()
            let sr = new List<StrideConfiguration>()
            let st = new List<StepConfiguration>()
            let sp = new List<StepProcessorConfiguration>()

            if (el.Element(XName.Get("Components")).Element(XName.Get("TypeHandlers")) <> null) then
                for item in el.Element(XName.Get("Components")).Element(XName.Get("TypeHandlers")).Elements() do
                    th.Add(TypeHandlerConfiguration.FromXml(item, assembly))
            if (el.Element(XName.Get("Components")).Element(XName.Get("Strides")) <> null) then
                for item in el.Element(XName.Get("Components")).Element(XName.Get("Strides")).Elements() do
                    sr.Add(StrideConfiguration.FromXml(item, assembly))
            if (el.Element(XName.Get("Components")).Element(XName.Get("Steps")) <> null) then
                for item in el.Element(XName.Get("Components")).Element(XName.Get("Steps")).Elements() do
                    st.Add(StepConfiguration.FromXml(item, assembly))
            if (el.Element(XName.Get("Components")).Element(XName.Get("StepsProcessors")) <> null) then
                for item in el.Element(XName.Get("Components")).Element(XName.Get("StepProcessors")).Elements() do
                    sp.Add(StepProcessorConfiguration.FromXml(item, assembly))
            
            let conf = new SourceConfiguration(source, th.ToArray(), sr.ToArray(), st.ToArray(), sp.ToArray())
            conf
        else
            let conf = new SourceConfiguration(source)
            conf
            
    member internal this.MakeExplicit() =
        if not this.IsExplicit then            
            let assembly = 
                match this.Source with
                | AssemblySource(a) ->
                    a
                | FileSource(f) ->
                    Assembly.LoadFile(f)

            // Load assembly and analyze content
            let th = new List<TypeHandlerConfiguration>()
            let sr = new List<StrideConfiguration>()
            let st = new List<StepConfiguration>()
            let sp = new List<StepProcessorConfiguration>()
            for t in assembly.GetTypes() do
                let dep = List<string>()
                let bef = List<string>()
                let meta = List<Type * MetadataComparer>()

                let thAttribute = t.GetCustomAttribute<TypeHandlerAttribute>()
                if thAttribute <> null then
                    for item in thAttribute.Before do
                        bef.Add(item)
                    for item in thAttribute.Dependencies do
                        dep.Add(item)
                    th.Add(TypeHandlerConfiguration(thAttribute.ID, t, dep.ToArray(), bef.ToArray()))
                dep.Clear()
                bef.Clear()
                
                let srAttribute = t.GetCustomAttribute<StrideAttribute>()
                if srAttribute <> null then
                    for item in srAttribute.Before do
                        bef.Add(item)
                    for item in srAttribute.Dependencies do
                        dep.Add(item)
                    for item in t.GetCustomAttributes<UseMetadataAttribute>() do
                        meta.Add(item.MetadataType, 
                                    Activator.CreateInstance(item.Comparer) :?> MetadataComparer)
                    sr.Add(StrideConfiguration(srAttribute.ID, t, dep.ToArray(), bef.ToArray(), meta.ToArray()))
                dep.Clear()
                bef.Clear()
                meta.Clear()

                let stAttribute = t.GetCustomAttribute<StepAttribute>()
                if stAttribute <> null then
                    for item in stAttribute.Before do
                        bef.Add(item)
                    for item in stAttribute.Dependencies do
                        dep.Add(item)
                    for item in t.GetCustomAttributes<UseMetadataAttribute>() do
                        meta.Add(item.MetadataType, 
                                    Activator.CreateInstance(item.Comparer) :?> MetadataComparer)
                    st.Add(StepConfiguration(stAttribute.ID, stAttribute.Stride, t, dep.ToArray(), bef.ToArray(), meta.ToArray()))
                dep.Clear()
                bef.Clear()
                meta.Clear()
                        
                let spAttribute = t.GetCustomAttribute<StepProcessorAttribute>()
                if spAttribute <> null then
                    for item in spAttribute.Before do
                        bef.Add(item)
                    for item in spAttribute.Dependencies do
                        dep.Add(item)
                    for item in t.GetCustomAttributes<UseMetadataAttribute>() do
                        meta.Add(item.MetadataType, 
                                    Activator.CreateInstance(item.Comparer) :?> MetadataComparer)
                    sp.Add(StepProcessorConfiguration(spAttribute.ID, spAttribute.Step, t, dep.ToArray(), bef.ToArray(), meta.ToArray()))

            // Return
            SourceConfiguration(this.Source, th.ToArray(), sr.ToArray(), st.ToArray(), sp.ToArray())
        else
            this  
             
///
///<summary>
///Configuration of a compiler instance
///</summary>
///
[<AllowNullLiteral>]
type PipelineConfiguration(defStrides, sources: SourceConfiguration array) =
    ///
    ///<summary>
    ///The set of components sources
    ///</summary>
    ///
    member val Sources = sources    
    ///
    ///<summary>
    //Instantiates a default configuration with the set of sources of the native compiler components
    ///</summary>
    ///
    new() =
        PipelineConfiguration(true, [||])    
    ///
    ///<summary>
    ///Instantiates a default configuration with or without native compiler components
    ///</summary>
    ///
    new(defSteps) =
        PipelineConfiguration(defSteps, [||])        
            
    ///
    ///<summary>
    ///Instantiates a configuration with explicit stepe, processors and type handlers
    ///</summary>
    ///
    new(defStrides, strides:ICompilerStride list, handlers:TypeHandler list) =
        let sourceDic = new Dictionary<Assembly, List<TypeHandlerConfiguration> * List<StrideConfiguration> * List<StepConfiguration> * List<StepProcessorConfiguration>>()
        // Fake ids to produce dependency
        let mutable strideID = 0
        for strideIdx = 0 to strides.Length do
            let stride = strides.[strideIdx]
            let mutable stepID = 0
            for stepIdx = 0 to stride.Steps.Length do
                let step = stride.Steps.[stepIdx]
                let mutable stepProcID = 0
                // Build processors configuration
                for i = 0 to step.Processors.Length do
                    let proc = step.Processors.[i]
                    let assembly = proc.GetType().Assembly
                    if not (sourceDic.ContainsKey(assembly)) then
                        sourceDic.Add(assembly, (new List<TypeHandlerConfiguration>(), new List<StrideConfiguration>(), new List<StepConfiguration>(), new List<StepProcessorConfiguration>()))
                    let _, _, _, spc = sourceDic.[assembly]
                    spc.Add(new StepProcessorConfiguration("sp" + stepProcID.ToString(),
                                                            "s" + stepID.ToString(), 
                                                            proc.GetType(), 
                                                            (if stepProcID = 0 then [||] else [| "sp" + (stepProcID - 1).ToString() |]),
                                                            [||],
                                                            [||]))
                    stepProcID <- stepProcID + 1

                // Add step configuration
                let assembly = step.GetType().Assembly
                if not (sourceDic.ContainsKey(assembly)) then
                    sourceDic.Add(assembly, (new List<TypeHandlerConfiguration>(), new List<StrideConfiguration>(), new List<StepConfiguration>(), new List<StepProcessorConfiguration>()))
                let _, _, sc, _ = sourceDic.[assembly]
                sc.Add(new StepConfiguration("s" + stepID.ToString(),
                                             "st" + strideID.ToString(),
                                             step.GetType(), 
                                             (if stepID = 0 then [||] else [| "s" + (stepProcID - 1).ToString() |]),
                                             [||],
                                             [||]))
                stepID <- stepID + 1
            
            // Add stride configuration
            let assembly = stride.GetType().Assembly
            if not (sourceDic.ContainsKey(assembly)) then
                sourceDic.Add(assembly, (new List<TypeHandlerConfiguration>(), new List<StrideConfiguration>(), new List<StepConfiguration>(), new List<StepProcessorConfiguration>()))
            let _, stc, _, _ = sourceDic.[assembly]
            stc.Add(new StrideConfiguration("st" + strideID.ToString(),
                                            stride.GetType(),
                                            (if strideID = 0 then [||] else [| "st" + (strideID - 1).ToString() |]),
                                            [||],
                                            [||]))
            strideID <- strideID + 1

        // Type handlers
        let mutable typeHandlerID = 0
        for i = 0 to handlers.Length do
            let th = handlers.[i]
            let assembly = th.GetType().Assembly
            if not (sourceDic.ContainsKey(assembly)) then
                sourceDic.Add(assembly, (new List<TypeHandlerConfiguration>(), new List<StrideConfiguration>(), new List<StepConfiguration>(), new List<StepProcessorConfiguration>()))
            let thc, _, _, _ = sourceDic.[assembly]
            thc.Add(new TypeHandlerConfiguration("th" + typeHandlerID.ToString(), 
                                                 th.GetType(), 
                                                 (if typeHandlerID = 0 then [||] else [| "th" + (typeHandlerID - 1).ToString() |]),
                                                 [||]))
            typeHandlerID <- typeHandlerID + 1

        // Create assembly based configuration
        new PipelineConfiguration(defStrides, Array.ofSeq(seq {
                                                                    for item in sourceDic do
                                                                        let thc, sr, st, sp = item.Value                                                                        
                                                                        yield new SourceConfiguration(AssemblySource(item.Key), thc.ToArray(), sr.ToArray(), st.ToArray(), sp.ToArray())     
                                                               }))
        
    ///
    ///<summary>
    ///Whether or not this configuration is a default configuration
    ///</summary>
    ///
    member this.IsDefault 
        with get() =
            let isDef = 
                if this.Sources.Length = 0 then
                    false
                else
                    this.Sources |> Array.map(fun (el:SourceConfiguration) -> el.IsDefault) |> Array.reduce(fun (el1:bool) (el2:bool) -> el1 && el2)
            isDef
    ///
    ///<summary>
    ///Whether or not the native components must be merged with this configuration
    ///</summary>
    ///
    member this.LoadDefaultSteps 
        with get() = 
            defStrides && (not this.IsDefault)

    member internal this.ToXml() =
        let el = new XElement(XName.Get(this.GetType().Name),
                    new XAttribute(XName.Get("LoadDefaultComponents"), this.LoadDefaultSteps),
                    new XElement(XName.Get("Sources"),
                        Array.ofSeq(seq {
                            for item in this.Sources do
                                yield item.ToXml()
                        })))
        let doc = new XDocument(el)
        doc
        
    static member internal FromXml(doc: XDocument, srcRoot: string) =
        let sources = new List<SourceConfiguration>()
        let loadDefault = if(doc.Root.Attribute(XName.Get("LoadDefaultComponents")) <> null) then
                            bool.Parse(doc.Root.Attribute(XName.Get("LoadDefaultComponents")).Value)
                          else
                             false
        for s in doc.Root.Elements(XName.Get("Sources")) do
            for source in s.Elements() do
                sources.Add(SourceConfiguration.FromXml(source, srcRoot))
        
        PipelineConfiguration(loadDefault, Array.ofSeq sources)
        
    member internal this.MakeExplicit() =
        let sources = new List<SourceConfiguration>()
        for item in this.Sources do
            // Filter out assembly with no components even if made explicit
            let exp = item.MakeExplicit()
            if exp.IsExplicit then
                sources.Add(exp)        
        PipelineConfiguration(this.LoadDefaultSteps, Array.ofSeq sources)
        
    member internal this.MergeDefault(def: PipelineConfiguration) =
        if this.LoadDefaultSteps then
            let sources = new List<SourceConfiguration>()
            for s in this.MakeExplicit().Sources do
                sources.Add(s)
            // Merge with default sources
            for s in def.MakeExplicit().Sources do
                sources.Add(s)
            PipelineConfiguration(false, Array.ofSeq sources)
        else
            this.MakeExplicit()

                                                          
        

                            
       
            

