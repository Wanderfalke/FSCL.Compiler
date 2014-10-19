namespace FSCL.Compiler
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Quotations
open System
open System.Collections.Generic
open System.Collections.ObjectModel

type FunctionParameter(name:string, 
                       originalPlaceholder: Quotations.Var, 
                       parameterType: FunctionParameterType,
                       meta: IParamMetaCollection option) =
    let sp = new List<IFunctionParameter>()

    interface IFunctionParameter with

        // Override starts
        member this.Name
            with get() = 
                this.Name
        
        member this.DataType 
            with get() =
                this.Placeholder.Type
            
        member this.ParameterType 
            with get() =
                this.ParameterType           
            
        member this.Placeholder 
            with get() =
                this.Placeholder
                
        member this.OriginalPlaceholder 
            with get() =
                this.OriginalPlaceholder

        member this.AccessAnalysis
            with get() =
                this.AccessAnalysis

        member this.ReturnExpr 
            with get() =
                this.ReturnExpr

        member this.IsReturned 
            with get() =
                this.IsReturned

        member this.SizeParameters 
            with get() =
                sp.AsReadOnly()
            
        member this.Meta 
            with get() =
                this.Meta

        member this.IsSizeParameter
            with get() = 
                this.IsSizeParameter 
                       
        member this.IsNormalParameter 
            with get() = 
                this.IsNormalParameter     

        member this.IsDynamicParameter 
            with get() = 
                this.IsDynamicParameter
                 
        member this.IsImplicitParameter 
            with get() = 
                this.IsImplicitParameter

        member this.DynamicAllocationArguments
            with get() =
                this.DynamicAllocationArguments
    // Override ends

    // Get-set properties
    member val Name = name
        with get
       
    member val ParameterType = parameterType
        with get

    member val Placeholder = originalPlaceholder 
        with get, set
            
    member val OriginalPlaceholder = originalPlaceholder
        with get
            
    member this.DataType
        with get() =
            this.Placeholder.Type
            
    member this.SizeParameters
        with get() =
            sp

    member val AccessAnalysis = AccessAnalysisResult.NoAccess
        with get, set
            
    member val ReturnExpr = None
        with get, set

    member val IsReturned = false 
        with get, set
            
    member val Meta =
        if meta.IsNone then
            new ParamMetaCollection() :> IParamMetaCollection
        else
            meta.Value
        with get
        
    member val IsSizeParameter =
        match parameterType with
        | SizeParameter ->
            true
        | _ ->
            false           
        with get 

    member val IsNormalParameter =
        match parameterType with
        | NormalParameter ->
            true
        | _ ->
            false     
        with get
           
    member val IsDynamicParameter =
        match parameterType with
        | DynamicParameter(_) ->
            true
        | _ ->
            false     
        with get
         
    member val IsImplicitParameter =
        match parameterType with
        | ImplicitParameter ->
            true
        | _ ->
            false  
        with get
              
    member val DynamicAllocationArguments =
        match parameterType with
        | DynamicParameter(allocArgs) ->
            Some(allocArgs)
        | _ ->
            None
        with get
         
type OriginalFunctionParameter(p: ParameterInfo, placeholder: Quotations.Var, meta: IParamMetaCollection option) =
    inherit FunctionParameter(p.Name, placeholder, NormalParameter, meta)
    interface IOriginalFunctionParameter with
        member this.OriginalParamterInfo 
            with get() =
                p

[<AllowNullLiteral>]
type KernelUtilityFunctionInfo(parsedSignature: MethodInfo, 
                               paramInfos: ParameterInfo list,
                               paramVars: Quotations.Var list,
                               body: Expr, 
                               isLambda: bool) =   

    let parameters =
        paramInfos |> 
        List.mapi(fun i (p:ParameterInfo) ->
                    OriginalFunctionParameter(p, paramVars.[i], None) :> FunctionParameter)

    interface IKernelUtilityFunctionInfo with
        member this.ID 
            with get() = 
                this.ID
        member this.ParsedSignature 
            with get() =
                this.ParsedSignature
        member this.OriginalParameters
            with get() =
                let roList = new List<IOriginalFunctionParameter>()
                for item in this.OriginalParameters do
                    roList.Add(item :?> OriginalFunctionParameter)
                roList.AsReadOnly()
        member this.GeneratedParameters 
            with get() =
                let roList = new List<IFunctionParameter>()
                for item in this.GeneratedParameters do
                    roList.Add(item)
                roList.AsReadOnly()
        member this.Parameters 
            with get() =
                let roList = new List<IFunctionParameter>()
                for item in this.OriginalParameters do
                    roList.Add(item)
                for item in this.GeneratedParameters do
                    roList.Add(item)
                roList.AsReadOnly()
        member this.ReturnType
            with get() =
                this.ReturnType
        member this.SignatureCode
            with get() =
                this.SignatureCode
        member this.Body
            with get() =
                this.Body
        member this.OriginalBody
            with get() =
                this.OriginalBody
        member this.Code
            with get() =
                this.Code
        member this.IsLambda 
            with get() =
                this.IsLambda
        member this.CustomInfo
            with get() =
                this.CustomInfo :> IReadOnlyDictionary<string, obj>
        member this.GetParameter(name) =
            match this.GetParameter(name) with
            | Some(p) ->
                Some(p :> IFunctionParameter)
            | _ ->
                None

    // Get-Set properties
    ///
    ///<summary>
    /// The ID of the function
    ///</summary>
    ///
    abstract member ID: FunctionInfoID
    default this.ID 
        with get() =     
            // A lambda kernels/function is identified by its AST    
            if isLambda then
                LambdaID(body.ToString())
            else
                MethodID(parsedSignature)

    member val ParsedSignature = parsedSignature with get
    ///
    ///<summary>
    /// The set of information about original (signature-extracted) function parameters
    ///</summary>
    ///
    abstract member OriginalParameters: FunctionParameter list
    default this.OriginalParameters 
        with get() =
            parameters
            
    ///
    ///<summary>
    /// The set of information about generated function parameters
    ///</summary>
    ///
    member val GeneratedParameters = new List<FunctionParameter>() with get
    ///
    ///<summary>
    /// The set of information about all the function parameters
    ///</summary>
    ///
    member this.Parameters 
        with get() =
            this.OriginalParameters @ List.ofSeq(this.GeneratedParameters)
    ///
    ///<summary>
    /// The function return type
    ///</summary>
    ///
    member val ReturnType = parsedSignature.ReturnType with get, set
    ///
    ///<summary>
    /// The body of the function
    ///</summary>
    ///
    member val OriginalBody = body with get
    ///
    ///<summary>
    /// The body of the function
    ///</summary>
    ///
    member val Body = body with get, set
    ///
    ///<summary>
    /// The generated target code
    ///</summary>
    ///
    member val Code = "" with get, set   
    ///
    ///<summary>
    /// The generated target code for the signature
    ///</summary>
    ///
    member val SignatureCode = "" with get, set   
    ///
    ///<summary>
    /// Whether this function has been generated from a lambda
    ///</summary>
    ///
    member val IsLambda = isLambda with get
    ///
    ///<summary>
    /// A set of custom additional information to be stored in the function
    ///</summary>
    ///<remarks>
    /// This set can be useful to collect and share additional information between custom steps/processors (compiler extensions)
    ///</remarks>
    ///
    member val CustomInfo = new Dictionary<String, Object>() with get
    
    member this.GetParameter(name) =
        let r =  Seq.tryFind(fun (p: FunctionParameter) -> p.Name = name) (this.OriginalParameters)
        match r with
        | Some(p) ->
            Some(p)
        | _ ->
            Seq.tryFind(fun (p: FunctionParameter) -> p.Name = name) (this.GeneratedParameters)

///
///<summary>
/// The set of information about kernels collected and maintained by the compiler
///</summary>
///<remarks>
/// This type inherits from FunctionInfo without exposing any additional property/member. The set
/// of information contained in FunctionInfo is in fact enough expressive to represent a kernel. 
/// From another point of view, a function can be considered a special case of a kernel, where the address-space is fixed, some
/// OpenCL functions cannot be called (e.g. get_global_id) and with some other restrictions.
/// KernelInfo is kept an independent, different class from FunctionInfo with the purpose to trigger different compiler processing on the basis of the
/// actual type.
///</remarks>
///     
[<AllowNullLiteral>]
type KernelInfo(originalSignature: MethodInfo, 
                paramInfos: ParameterInfo list,
                paramVars: Quotations.Var list,
                body: Expr, 
                meta: ReadOnlyMetaCollection, 
                isLambda: bool) =
    inherit KernelUtilityFunctionInfo(originalSignature, paramInfos, paramVars, body, isLambda)
    
    let parameters =
        paramInfos |>
        List.mapi(fun i (p:ParameterInfo) ->                        
                        OriginalFunctionParameter(p, paramVars.[i], Some(meta.ParamMeta.[i])) :> FunctionParameter)

    let localVars = new Dictionary<Quotations.Var, Type * (Expr list option)>()

    override this.OriginalParameters
        with get() =
            parameters

    member this.LocalVars
        with get() =
            localVars
    
    member this.IsLocalVar(v: Quotations.Var) =
        localVars.ContainsKey(v)
        
    member val Meta = meta 
        with get
        
    member this.CloneTo(kInfo: KernelInfo) =
        // Copy kernel info fields
        kInfo.Body <- this.Body
        kInfo.Code <- this.Code
        for item in this.CustomInfo do
            if not (kInfo.CustomInfo.ContainsKey(item.Key)) then
                kInfo.CustomInfo.Add(item.Key, item.Value)
        for item in this.GeneratedParameters do
            if item.IsReturned && item.IsDynamicParameter then
                // Must associate new Return Meta
                let oldParameter = item
                let newParameter = new FunctionParameter(item.Name, item.OriginalPlaceholder, item.ParameterType, Some(kInfo.Meta.ReturnMeta :> IParamMetaCollection)) 
                newParameter.AccessAnalysis <- oldParameter.AccessAnalysis
                newParameter.IsReturned <- oldParameter.IsReturned
                newParameter.ReturnExpr <- oldParameter.ReturnExpr
                newParameter.Placeholder <- oldParameter.Placeholder
                for i = 0 to oldParameter.SizeParameters.Count - 1 do
                    newParameter.SizeParameters.Add(oldParameter.SizeParameters.[i])
                kInfo.GeneratedParameters.Add(newParameter)
            else                    
                kInfo.GeneratedParameters.Add(item)
        for i = 0 to this.OriginalParameters.Length - 1 do
            let oldParameter = this.OriginalParameters.[i]
            let newParameter = kInfo.OriginalParameters.[i]
            newParameter.AccessAnalysis <- oldParameter.AccessAnalysis
            newParameter.IsReturned <- oldParameter.IsReturned
            newParameter.ReturnExpr <- oldParameter.ReturnExpr
            newParameter.Placeholder <- oldParameter.Placeholder
            for i = 0 to oldParameter.SizeParameters.Count - 1 do
                newParameter.SizeParameters.Add(oldParameter.SizeParameters.[i])
        for item in this.LocalVars do
            kInfo.LocalVars.Add(item.Key, item.Value)
        kInfo.ReturnType <- this.ReturnType   

    interface IKernelInfo with                
        member this.LocalVars 
            with get() =
                this.LocalVars :> IReadOnlyDictionary<Quotations.Var, Type * (Expr list option)>                 
        member this.Meta  
            with get() =
                this.Meta            

[<AllowNullLiteral>]
type RegularFunctionInfo(signature: MethodInfo, meta: ReadOnlyMetaCollection) =
    interface IRegularFunctionInfo with
        member this.ID 
            with get() =
                this.ID
        member this.Signature 
            with get() =
                signature
        member this.Meta 
            with get() =
                meta
                
    member val ID = MethodID(signature) with get
    member val Signature = signature with get
    member val Meta = meta with get
    
[<AllowNullLiteral>]
type KernelModule(k: KernelInfo) =  
    interface IKernelModule with
        member this.Kernel
            with get() =
                this.Kernel :> IKernelInfo
        member this.Functions
            with get() =
                this.Functions :> IReadOnlyDictionary<FunctionInfoID, IKernelUtilityFunctionInfo>
        member this.GlobalTypes
            with get() =
                this.GlobalTypes |> List.ofSeq
        member this.Directives
            with get() =
                this.Directives |> List.ofSeq
        member this.Code
            with get() =
                this.Code
        member this.CustomInfo
            with get() =
                this.CustomInfo :> IReadOnlyDictionary<string, obj>
        member this.ConstantDefines 
            with get() =
                this.ConstantDefines :> IReadOnlyDictionary<String, Expr * bool>
        member this.Meta 
            with get() =
                this.Meta

    // Get-Set properties
    member val Kernel = k with get
    member val Functions = new Dictionary<FunctionInfoID, IKernelUtilityFunctionInfo>() with get
    member val GlobalTypes = new HashSet<Type>() with get
    member val Directives = new HashSet<String>() with get

    member val ConstantDefines = new Dictionary<string, Expr * bool>() with get
    member val StaticConstantDefinesCode = new Dictionary<string, string>() with get
 
    member val Code:string option = None with get, set
    member val CustomInfo = new Dictionary<String, Object>() with get
    member this.Meta
        with get() =
            this.Kernel.Meta
    
    member this.CloneTo(kModule: KernelModule) =
        // Copy kernel info fields
        this.Kernel.CloneTo(kModule.Kernel)
        for item in this.Functions do
            kModule.Functions.Add(item.Key, item.Value) |> ignore
        for item in this.GlobalTypes do
            kModule.GlobalTypes.Add(item) |> ignore
        for item in this.Directives do
            kModule.Directives.Add(item) |> ignore
        for item in this.ConstantDefines do
            kModule.ConstantDefines.Add(item.Key, item.Value) |> ignore
        for item in this.StaticConstantDefinesCode do
            kModule.StaticConstantDefinesCode.Add(item.Key, item.Value) |> ignore
        for item in this.CustomInfo do
            kModule.CustomInfo.Add(item.Key, item.Value) |> ignore
        kModule.Code <- this.Code

[<AllowNullLiteral>]
type RegularFunctionModule(f: RegularFunctionInfo) =  
    interface IRegularFunctionModule with
        member this.Function
            with get() =
                this.Function :> IRegularFunctionInfo
        member this.CustomInfo
            with get() =
                this.CustomInfo :> IReadOnlyDictionary<string, obj>
        member this.Meta 
            with get() =
                this.Meta

    member val Function = f with get
    member val CustomInfo = new Dictionary<String, Object>() with get
    member this.Meta
        with get() =
            this.Function.Meta 
    
[<AllowNullLiteral>]
type KernelFlowGraphNode(content: KernelModule, workItemInfo: Expr option, callArgs: Expr list option, isRoot: bool) =
    interface IKernelFlowGraphNode with
        member this.Content
            with get() =
                this.Content :> ICompilationModule
        member this.CallArgs
            with get() =
                this.CallArgs
        member this.WorkItemInfo
            with get() =
                this.WorkItemInfo
        member this.Input 
            with get() =
                this.Input :> IReadOnlyDictionary<string, ICompilationFlowGraphNode>
        member this.IsRoot
            with get() =
                this.IsRoot
        member this.IsEntryPoint 
            with get() =
                this.IsEntryPoint
    member val Content = content with get
    member val CallArgs = callArgs with get
    member val WorkItemInfo = workItemInfo with get
    member val Input = new Dictionary<string, ICompilationFlowGraphNode>() with get
    member val IsRoot = isRoot with get
    member this.IsEntryPoint 
        with get() =
            this.Input.Count = 0
            
[<AllowNullLiteral>]
type RegularFunctionFlowGraphNode(content: RegularFunctionModule, objInstance: Expr option, callArgs: Expr list option, isRoot: bool) =
    interface IRegularFunctionFlowGraphNode with
        member this.Content
            with get() =
                this.Content :> ICompilationModule
        member this.CallArgs
            with get() =
                this.CallArgs
        member this.Input 
            with get() =
                this.Input :> IReadOnlyDictionary<string, ICompilationFlowGraphNode>
        member this.IsRoot
            with get() =
                this.IsRoot
        member this.IsEntryPoint 
            with get() =
                this.IsEntryPoint
        member this.ObjectInstance 
            with get() =
                this.ObjectInstance
    member val Content = content with get
    member val ObjectInstance = objInstance with get
    member val CallArgs = callArgs with get
    member val Input = new Dictionary<string, ICompilationFlowGraphNode>() with get
    member val IsRoot = isRoot with get
    member this.IsEntryPoint 
        with get() =
            this.Input.Count = 0
        
[<AllowNullLiteral>]
type CompilationUnit(root: ICompilationFlowGraphNode) =  
    let kMods = new List<IKernelModule>()  
    let rfMods = new List<IRegularFunctionModule>() 
    let rec getKeyMods(n: ICompilationFlowGraphNode) =
        for c in n.Input do 
            getKeyMods(c.Value) 
        if n :? KernelFlowGraphNode then
            kMods.Add((n :?> KernelFlowGraphNode).Content)
    let rec getRegFunMods(n: ICompilationFlowGraphNode) =
        for c in n.Input do 
            getRegFunMods(c.Value) 
        if n :? RegularFunctionFlowGraphNode then
            rfMods.Add((n :?> RegularFunctionFlowGraphNode).Content)
    do 
        getKeyMods(root)
        getRegFunMods(root)

    interface ICompilationUnit with
        member this.Root
            with get() =
                this.Root
        member this.KernelModules 
            with get() =
                this.KernelModules |> List.ofSeq
        member this.RegularFunctionModules 
            with get() =
                this.RegularFunctionModules |> List.ofSeq
    // Get-Set properties
    member val Root = root with get
    member val KernelModules = kMods with get
    member val RegularFunctionModules = rfMods with get
    
[<AllowNullLiteral>]
type CompilerCache(openCLMetadataVerifier: ReadOnlyMetaCollection * ReadOnlyMetaCollection -> bool,
                   regularFunctionMetadataVerifier: ReadOnlyMetaCollection * ReadOnlyMetaCollection -> bool) =
    member val Kernels = Dictionary<FunctionInfoID, List<KernelModule>>() 
        with get
    
    member this.TryFindCompatibleOpenCLCachedKernel(id: FunctionInfoID, 
                                                    meta: ReadOnlyMetaCollection) =
        if this.Kernels.ContainsKey(id) then
            let potentialKernels = this.Kernels.[id]
            // Check if compatible kernel meta in cached kernels
            let item = Seq.tryFind(fun (cachedKernel: KernelModule) ->
                                        openCLMetadataVerifier(cachedKernel.Meta, meta)) potentialKernels
            item
        else
            None  
                             
