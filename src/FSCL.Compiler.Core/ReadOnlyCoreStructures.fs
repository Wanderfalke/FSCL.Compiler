namespace FSCL.Compiler
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Quotations
open System
open System.Collections.Generic
open System.Collections.ObjectModel

///
///<summary>
/// Enumeration describing the access mode to a kernel parameter (R, W, RW or not used)
///</summary>
///
[<Flags>]
type AccessAnalysisResult =
| NoAccess = 0
| ReadAccess = 1
| WriteAccess = 2

type FunctionParameterType =
| NormalParameter
| SizeParameter
| DynamicParameter of Expr array
| ImplicitParameter

type FunctionInfoID =
| LambdaID of string
| MethodID of MethodInfo
    
type ConstantDefineValue =
| StaticValue of Expr
| DynamicValue of (Expr -> string)
///
///<summary>
/// The set of information about a kernel parameter collected and maintained by the compiler
///</summary>
///
type IFunctionParameter =
    abstract Name: string with get
    abstract DataType: Type with get
    abstract ParameterType: FunctionParameterType with get
    abstract OriginalPlaceholder: Quotations.Var with get
    abstract Placeholder: Quotations.Var with get
    abstract AccessAnalysis: AccessAnalysisResult with get
    abstract ReturnExpr: Expr option with get
    abstract IsReturned: bool with get
    abstract SizeParameters: ReadOnlyCollection<IFunctionParameter> with get
    abstract Meta: IParamMetaCollection with get
            
    abstract IsSizeParameter: bool with get
    abstract IsNormalParameter: bool with get
    abstract IsDynamicParameter: bool with get
    abstract IsImplicitParameter: bool with get
    abstract DynamicAllocationArguments: Expr array option with get

type IOriginalFunctionParameter =
    inherit IFunctionParameter
    abstract OriginalParamterInfo: ParameterInfo with get
    
///
///<summary>
/// The set of information about utility functions collected and maintained by the compiler
///</summary>
///

[<AllowNullLiteral>]
type IKernelUtilityFunctionInfo =
    abstract ID: FunctionInfoID with get
    abstract ParsedSignature: MethodInfo with get

    abstract OriginalParameters: ReadOnlyCollection<IOriginalFunctionParameter> with get
    abstract GeneratedParameters: ReadOnlyCollection<IFunctionParameter> with get
    abstract Parameters: ReadOnlyCollection<IFunctionParameter> with get
    abstract ReturnType: Type with get
    abstract Body: Expr with get
    abstract OriginalBody: Expr with get

    abstract member Code: string with get
    abstract member SignatureCode: string with get
    
    abstract IsLambda: bool with get
    abstract CustomInfo: IReadOnlyDictionary<string, obj> with get

    abstract GetParameter: string -> IFunctionParameter option

[<AllowNullLiteral>]
type IKernelInfo =
    inherit IKernelUtilityFunctionInfo
    abstract LocalVars: IReadOnlyDictionary<Quotations.Var, Type * (Expr list option)>
    abstract Meta: ReadOnlyMetaCollection with get
    
[<AllowNullLiteral>]
type IRegularFunctionInfo =
    abstract ID: FunctionInfoID with get
    abstract member Signature: MethodInfo with get
    abstract member Meta: ReadOnlyMetaCollection with get
    
[<AllowNullLiteral>]
type ICompilationModule = 
    abstract CustomInfo: IReadOnlyDictionary<string, obj> with get
    abstract Meta: ReadOnlyMetaCollection with get    

[<AllowNullLiteral>]
type IKernelModule =
    inherit ICompilationModule

    abstract Kernel: IKernelInfo with get
    abstract Functions: IReadOnlyDictionary<FunctionInfoID, IKernelUtilityFunctionInfo> with get
    abstract GlobalTypes: Type list with get
    abstract Directives: String list with get
    abstract ConstantDefines: IReadOnlyDictionary<String, Expr * bool> with get
    abstract Code:string option with get
    
[<AllowNullLiteral>]
type IRegularFunctionModule = 
    inherit ICompilationModule
     
    abstract Function: IRegularFunctionInfo with get
                
[<AllowNullLiteral>]
type ICompilationFlowGraphNode =
    abstract Content: ICompilationModule with get
    abstract CallArgs: Expr list option with get

    abstract Input: IReadOnlyDictionary<string, ICompilationFlowGraphNode> with get
    abstract IsRoot: bool with get
    abstract IsEntryPoint: bool with get
    
[<AllowNullLiteral>]
type IKernelFlowGraphNode =
    inherit ICompilationFlowGraphNode
    abstract WorkItemInfo: Expr option with get
        
[<AllowNullLiteral>]
type IRegularFunctionFlowGraphNode =
    inherit ICompilationFlowGraphNode
    abstract ObjectInstance: Expr option with get 

[<AllowNullLiteral>]
type ICompilationUnit =
    abstract Root: ICompilationFlowGraphNode with get
    abstract KernelModules: IKernelModule list
    abstract RegularFunctionModules: IRegularFunctionModule list
