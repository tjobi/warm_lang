namespace WarmLangCompiler.ILGen;
using WarmLangCompiler.Binding;
using WarmLangCompiler.Symbols;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Binding.BoundAccessing;
using WarmLangLexerParser.ErrorReporting;
using static WarmLangCompiler.Binding.BoundBinaryOperatorKind;
using static WarmLangCompiler.ILGen.EmitterTypeSymbolHelpers;

using System.Collections.Immutable;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;

public sealed class Emitter{

    private readonly ErrorWarrningBag _diag;
    private readonly bool _debug;
    
    private static int _localFuncId = 0;

    private readonly ImmutableDictionary<FunctionSymbol, MethodReference> _builtInFunctions;
    private readonly MethodReference _stringConcat, _stringEqual, _stringSubscript;
    private readonly MethodReference _listEmpty, _listAdd, _listRemove, _listSubscript, _listSet, _listAddMany, _listLength;
    private readonly MethodReference _objectEquals, _objectCtor, _wlEquals, _wlToString;
    
    private readonly Dictionary<FunctionSymbol, MethodDefinition> _funcs;
    private readonly Dictionary<FunctionSymbol, ClosureState> _funcClosure;
    private readonly Stack<FunctionBodyState> _bodyStateStack;

    private static readonly string WARM_LANG_NAMESPACE = "";
    private readonly TypeDefinition _globalsType;
    private readonly Dictionary<VariableSymbol, FieldDefinition> _globals;

    private readonly Dictionary<TypeSymbol, WLTypeInformation> _typeInfoOf; 

    //mono.cecil stuff?
    private AssemblyDefinition _assemblyDef;
    private TypeDefinition _program;
    private AssemblyDefinition _mscorlib;

    private readonly CilTypeManager _cilTypeManager;
    private readonly ListMethodHelper _listMethods;
    private TypeReference CilTypeOf(TypeSymbol type) => _cilTypeManager.GetType(type);

    public FunctionBodyState BodyState => _bodyStateStack.Peek();
    public FunctionBodyState ParentState()
    {
        var cur = _bodyStateStack.Pop();
        var toRet = _bodyStateStack.Peek();
        _bodyStateStack.Push(cur);
        return toRet;
    }

    private Emitter(ErrorWarrningBag diag, bool debug = false)
    {
        _debug = debug;
        _diag = diag;
        _funcs = new();
        _bodyStateStack = new();
        _globals = new();
        _funcClosure = new();
        _typeInfoOf = new();

        // IL -> ".assembly 'App' {}"
        var assemblyName = new AssemblyNameDefinition("App", new Version(1,0));
        _assemblyDef = AssemblyDefinition.CreateAssembly(assemblyName, "warmlang", ModuleKind.Console);
        
        // IL -> ".assembly extern mscorlib {}"
        _mscorlib = ReadMscorlib();
        _cilTypeManager = new(_mscorlib, _assemblyDef);
        _listMethods = _cilTypeManager.ListHelper;
        
        foreach(var type in BuiltInTypes())
        {
            foreach(var module in _mscorlib.Modules)
            {
                var t = module.GetType(type.ToCilName());
                if(t is not null)
                {
                    var typeRef = _assemblyDef.MainModule.ImportReference(t);
                    _cilTypeManager.Add(type, typeRef);
                    break;
                }
            }
        }

        // IL -> ".class private auto ansi beforefieldinit abstract sealed Program extends [mscorlib]System.Object {"
        var systemObject = _assemblyDef.MainModule.ImportReference(CilTypeOf(CILBaseTypeSymbol));
        _program = new TypeDefinition(WARM_LANG_NAMESPACE, "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, systemObject);
        _assemblyDef.MainModule.Types.Add(_program);

        _globalsType = CreateGlobalsType(systemObject);
        _assemblyDef.MainModule.Types.Add(_globalsType);

        _builtInFunctions = ResolveBuiltInMethods(_mscorlib);

        // Needed for the implemenatation of __wl_tostring
        var dotnetConvert = _mscorlib.MainModule.GetType("System.Convert");
        var toStringConvert = GetMethodFromTypeDefinition(dotnetConvert, "ToString", GetCilParamNames(CILBaseTypeSymbol));
        
        var dotnetString = _mscorlib.MainModule.GetType("System.String");
        _stringConcat    = GetMethodFromTypeDefinition(dotnetString, "Concat", GetCilParamNames(TypeSymbol.String, TypeSymbol.String));
        _stringEqual     = GetMethodFromTypeDefinition(dotnetString, "op_Equality", GetCilParamNames(TypeSymbol.String, TypeSymbol.String));
        _stringSubscript = GetMethodFromTypeDefinition(dotnetString, "get_Chars", GetCilParamNames(TypeSymbol.Int));

        var dotnetArrayList = _mscorlib.MainModule.GetType("System.Collections.ArrayList");
        _listEmpty      = GetMethodFromTypeDefinition(dotnetArrayList, ".ctor", GetCilParamNames());
        _listAdd        = GetMethodFromTypeDefinition(dotnetArrayList, "Add", GetCilParamNames(CILBaseTypeSymbol));
        _listRemove     = GetMethodFromTypeDefinition(dotnetArrayList, "RemoveAt", GetCilParamNames(TypeSymbol.Int));
        _listSubscript  = GetMethodFromTypeDefinition(dotnetArrayList, "get_Item", GetCilParamNames(TypeSymbol.Int));
        _listSet        = GetMethodFromTypeDefinition(dotnetArrayList, "set_Item", GetCilParamNames(TypeSymbol.Int, CILBaseTypeSymbol));
        _listAddMany    = GetMethodFromTypeDefinition(dotnetArrayList, "AddRange", new[]{"System.Collections.ICollection"});
        _listLength     = GetMethodFromTypeDefinition(dotnetArrayList, "get_Count", GetCilParamNames());

        var dotnetObject = _mscorlib.MainModule.GetType("System.Object");
        _objectEquals = GetMethodFromTypeDefinition(dotnetObject, "Equals", GetCilParamNames(CILBaseTypeSymbol, CILBaseTypeSymbol));
        _objectCtor   = GetMethodFromTypeDefinition(dotnetObject, ".ctor", GetCilParamNames());

        var functionHelper = new WLRuntimeFunctionHelper(_program, CilTypeOf, _listLength, _listSubscript, _stringEqual, toStringConvert, _objectEquals, _stringConcat);
        //functionHelper.EnableDebugging(_builtInFunctions[BuiltInFunctions.StdWriteLine]);
        _wlEquals = functionHelper.CreateWLEquals();
        _wlToString = functionHelper.CreateWLToString();
    }

    private ImmutableDictionary<FunctionSymbol, MethodReference> ResolveBuiltInMethods(AssemblyDefinition mscorlib)
    {
        var builder = ImmutableDictionary.CreateBuilder<FunctionSymbol,MethodReference>();
        var dotnetConsole = mscorlib.MainModule.GetType("System.Console");
        builder[BuiltInFunctions.StdWriteLine]  = GetMethodFromTypeDefinition(dotnetConsole, "WriteLine", GetCilParamNames(TypeSymbol.String));
        builder[BuiltInFunctions.StdWrite]      = GetMethodFromTypeDefinition(dotnetConsole, "Write", GetCilParamNames(TypeSymbol.String));
        builder[BuiltInFunctions.StdWriteC]     = GetMethodFromTypeDefinition(dotnetConsole, "Write", new[]{"System.Char"});
        builder[BuiltInFunctions.StdRead]       = GetMethodFromTypeDefinition(dotnetConsole, "ReadLine", GetCilParamNames());
        builder[BuiltInFunctions.StdClear]      = GetMethodFromTypeDefinition(dotnetConsole, "Clear", GetCilParamNames());
        var dotnetString = mscorlib.MainModule.GetType("System.String");
        builder[BuiltInFunctions.StrLen]        = GetMethodFromTypeDefinition(dotnetString, "get_Length", GetCilParamNames());
        return builder.ToImmutable();
    }
    private MethodReference GetMethodFromTypeDefinition(TypeDefinition type, string methodName, string[] parameterTypes)
    {
        var methods = type.Methods
                      .Where(m => m.Name == methodName && m.Parameters.Count == parameterTypes.Length)
                      .ToArray();
        if(methods.Length == 0)
        {
            _diag.ReportRequiredMethodProblems(type.Name, methodName, parameterTypes);
            return null!;
        }

        foreach(var method in methods)
        {
            var allParamsMatch = true;
            for (int i = 0; i < parameterTypes.Length && allParamsMatch; i++)
            {
                var expectedParam = parameterTypes[i];
                var foundParam = method.Parameters[i].ParameterType.FullName;
                if(expectedParam != foundParam)
                    allParamsMatch = false;
            }
            if(!allParamsMatch)
                continue;
            return _assemblyDef.MainModule.ImportReference(method);
        }
        _diag.ReportRequiredMethodProblems(type.Name, methodName, parameterTypes);
        return null!;
    }

    private static AssemblyDefinition ReadMscorlib()
    {
        //TODO: Add a way for the user of compiler to specify path to dll, AND look through other versions of folder structure
        #if OS_LINUX
            string LINUX_PATH = "/usr/lib/mono/4.8-api/mscorlib.dll";
            if(File.Exists(LINUX_PATH))
                return AssemblyDefinition.ReadAssembly(LINUX_PATH);
        #elif OS_WINDOWS
            string WINDOWS_PATH = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll";
            if(File.Exists(WINDOWS_PATH))
                return AssemblyDefinition.ReadAssembly(WINDOWS_PATH);
        #endif
        throw new NotImplementedException($"-- Compiler couldn't find 'mscorlib' allow user to specify mscorlib.dll location ---");
    }
   
    private TypeDefinition CreateGlobalsType(TypeReference systemObject)
    {
        var globalsType = new TypeDefinition(WARM_LANG_NAMESPACE, "GLOBALS",
                                                            TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit 
                                                            | TypeAttributes.Public  | TypeAttributes.Abstract 
                                                            | TypeAttributes.Sealed, systemObject);
        var globalConstructor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.HideBySig
                                                               | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName 
                                                               | MethodAttributes.Static , CilTypeOf(TypeSymbol.Void));
        globalsType.Methods.Add(globalConstructor);
        return globalsType;
    } 
   
    public static void EmitProgram(string outfile, BoundProgram program, ErrorWarrningBag diag, bool debug = false)
    {
        var emitter = new Emitter(diag, debug);
        if(emitter._diag.Any())
            return;
        emitter.EmitProgram(outfile, program);
    }

    private void EmitProgram(string outfile, BoundProgram program)
    {
        if(_diag.Any())
            return; //something went wrong in constructor
        
        foreach(var (type, members) in program.GetDeclaredTypes())
        {
            EmitTypeDeclaration(type, members);
        }

        foreach(var func in program.GetFunctionSymbols())
        {
            EmitFunctionDeclaration(func);
        }

        var globalsProcessor = _globalsType.Methods[0].Body.GetILProcessor();
        foreach(var globalVar in program.GlobalVariables)
        {
            EmitGlobalVariableDeclaration(globalsProcessor, globalVar);
        }
        globalsProcessor.Emit(OpCodes.Ret);
        globalsProcessor.Body.OptimizeMacros();

        //program.TypeMemberInformation still holds all the type information
        //GetFunctionSymbols just skips the need for multiple loops (for now at least)
        foreach(var (func, body) in program.GetFunctionSymbolsAndBodies())
        {
            EmitFunctionBody(func, body);
        }
        
        var main = _funcs[program.MainFunc is not null ? program.MainFunc : program.ScriptMain!];

        _assemblyDef.EntryPoint = main;
        _assemblyDef.Write(outfile);
    }

    private void EmitTypeDeclaration(TypeSymbol type, IList<MemberSymbol> members)
    {
        var TYPE_ATTRIBUTES = TypeAttributes.Public | TypeAttributes.AnsiClass 
                              | TypeAttributes.Sealed | TypeAttributes.AutoLayout
                              | TypeAttributes.BeforeFieldInit;
        var typeDef = new TypeDefinition(WARM_LANG_NAMESPACE, type.Name, TYPE_ATTRIBUTES, CilTypeOf(CILBaseTypeSymbol));
        _assemblyDef.MainModule.Types.Add(typeDef);

        //TODO: Must constructor call Dotnet.Object.ctor()?
        var CONSTRUCTOR_ATTRIBUTES = MethodAttributes.Public | MethodAttributes.HideBySig
                                     | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        var ctor = new MethodDefinition(".ctor", CONSTRUCTOR_ATTRIBUTES, CilTypeOf(TypeSymbol.Void));
        typeDef.Methods.Add(ctor);
        var bp = ctor.Body.GetILProcessor();
        bp.Emit(OpCodes.Ldarg_0);
        bp.Emit(OpCodes.Call, _objectCtor);
        bp.Emit(OpCodes.Ret);
        ctor.Body.OptimizeMacros();

        _cilTypeManager.Add(type, typeDef);
        var memberFields = new Dictionary<MemberSymbol, FieldReference>();
        foreach(var member in members)
        {
            //TODO: Revisit when member functions can be declared on the type declaration
            if(member is not MemberFieldSymbol f) continue;
            var FIELD_ATTRIBUTES = FieldAttributes.Public;
            var fieldDef = new FieldDefinition(f.Name, FIELD_ATTRIBUTES, CilTypeOf(member.Type));
            memberFields[member] = fieldDef;
            typeDef.Fields.Add(fieldDef);
        }
        _typeInfoOf[type] = new WLTypeInformation(type, typeDef, ctor, memberFields);
    }

    private void EmitFunctionDeclaration(FunctionSymbol func)
    {
        var funcDefintion = new MethodDefinition(func.Name, MethodAttributes.Static | MethodAttributes.Public, CilTypeOf(func.Type));
        
        foreach(var @param in func.Parameters)
        {
            var paramDef = new ParameterDefinition(@param.Name, ParameterAttributes.None, CilTypeOf(@param.Type));
            funcDefintion.Parameters.Add(paramDef);
        }
        _funcs[func] = funcDefintion;
        _program.Methods.Add(funcDefintion);
    }

    private void EmitFunctionBody(FunctionSymbol func, BoundBlockStatement body)
    {
        var sharedLocals = new HashSet<ScopedVariableSymbol>(func.SharedLocals);


        var thisState = new FunctionBodyState(func, sharedLocals);
        _bodyStateStack.Push(thisState);
        var funcDefintion = _funcs[func];
        var ilProcessor = funcDefintion.Body.GetILProcessor();

        SetupClosureState(thisState, func, ilProcessor);

        EmitBlockStatement(ilProcessor, body);


        foreach (var (instrIdx, label) in thisState.AwaitingLabels)
        {
            var instr = ilProcessor.Body.Instructions[instrIdx];
            var targetInstr = thisState.Labels[label];
            instr.Operand = targetInstr;
        }
        ilProcessor.Body.OptimizeMacros();

        _bodyStateStack.Pop();
        if(_debug) PrintFuncBody(func, ilProcessor);
    }

    private void EmitGlobalVariableDeclaration(ILProcessor processor, BoundVarDeclaration globalVar)
    {
        var fieldDef = new FieldDefinition(globalVar.Symbol.Name, FieldAttributes.Public | FieldAttributes.Static, CilTypeOf(globalVar.Type));
        _globalsType.Fields.Add(fieldDef);
        EmitExpression(processor, globalVar.RightHandSide);
        processor.Emit(OpCodes.Stsfld, fieldDef);
        _globals[globalVar.Symbol] = fieldDef;
    }

    private void EmitStatement(ILProcessor processor, BoundStatement statement)
    {
        switch(statement)
        {
            case BoundVarDeclaration varDecl:
                EmitVariableDeclaration(processor, varDecl);
                break;
            case BoundConditionalGotoStatement condGoto:
                EmitConditionalGotoStatement(processor, condGoto);
                break;
            case BoundGotoStatement gotoo:
                EmitGotoStatement(processor, gotoo);
                break;
            case BoundLabelStatement label:
                EmitLabelStatement(processor,label);
                break;
            case BoundBlockStatement block:
                EmitBlockStatement(processor, block);
                break;
            case BoundReturnStatement ret:
                EmitReturnStatement(processor, ret);
                break;
            case BoundFunctionDeclaration func when func.Symbol is LocalFunctionSymbol symbol:
                if(symbol.Body is not null) {
                    EmitLocalFunctionDeclaration(symbol);
                    EmitFunctionBody(symbol, symbol.Body);
                    break;
                }
                throw new Exception($"{nameof(Emitter)} - has reached exception state in {nameof(EmitStatement)} - LocalFunction '{symbol}' doesn't have a body!");
            case BoundExprStatement expr:
                EmitExprStatement(processor, expr);
                break;
            default: 
                throw new NotImplementedException($"{nameof(Emitter)} doesn't know '{statement} yet'");
        }
    }

    private void EmitVariableDeclaration(ILProcessor processor, BoundVarDeclaration varDecl)
    {
        var symbol = varDecl.Symbol;
        if(symbol is LocalVariableSymbol local && BodyState.IsSharedVariable(local))
        {  
            var fieldDef = _funcClosure[local.BelongsToOrThrow].GetFieldOrThrow(symbol);
            //BodyState.SharedLocals[symbol] = fieldDef;
        }
        else 
        {
            var variable = new VariableDefinition(CilTypeOf(varDecl.Type));
            BodyState.Locals[symbol] = variable;
            processor.Body.Variables.Add(variable);
        }
        var intstrBefore = GetLastInstruction(processor);
        EmitExpression(processor, varDecl.RightHandSide);
        EmitStoreLocation(processor, symbol, intstrBefore);
    }

    private void EmitConditionalGotoStatement(ILProcessor processor, BoundConditionalGotoStatement condGoto)
    {
        EmitExpression(processor, condGoto.Condition);
        var instruction = processor.Body.Instructions.Count;
        if(condGoto.FallsThroughTrueBranch)
        {
            BodyState.AwaitingLabels[instruction] = condGoto.LabelFalse;
            processor.Emit(OpCodes.Brfalse, processor.Create(OpCodes.Nop));
        }
        else {
            BodyState.AwaitingLabels[instruction] = condGoto.LabelTrue;
            processor.Emit(OpCodes.Brtrue, processor.Create(OpCodes.Nop));
        }
        
    }

    private void EmitGotoStatement(ILProcessor processor, BoundGotoStatement gotoo)
    {
        var instrCount = processor.Body.Instructions.Count;
        BodyState.AwaitingLabels[instrCount] = gotoo.Label;
        processor.Emit(OpCodes.Br, processor.Create(OpCodes.Nop));
    }

    private void EmitLabelStatement(ILProcessor processor, BoundLabelStatement label)
    {
        //dump a nop we can jump to?
        var nop = processor.Create(OpCodes.Nop);
        processor.Append(nop);
        BodyState.Labels[label.Label] = nop;
    }

    private void EmitBlockStatement(ILProcessor processor, BoundBlockStatement block)
    {
        foreach(var statement in block.Statements)
            EmitStatement(processor, statement);
    }

    private void EmitReturnStatement(ILProcessor processor, BoundReturnStatement ret)
    {
        if(ret.Expression is not null)
        {
            EmitExpression(processor, ret.Expression);
        }
        processor.Emit(OpCodes.Ret);
    }

    private void EmitLocalFunctionDeclaration(LocalFunctionSymbol func)
    {
        var funcName = $"_local_{_localFuncId++}_{func.Name}";
        var funcDefintion = new MethodDefinition(funcName, MethodAttributes.Static | MethodAttributes.Assembly, CilTypeOf(func.Type));
        _funcs[func] = funcDefintion;
        _program.Methods.Add(funcDefintion);

        foreach(var @param in func.Parameters)
        {
            var paramDef = new ParameterDefinition(@param.Name, ParameterAttributes.None, CilTypeOf(@param.Type));
            funcDefintion.Parameters.Add(paramDef);
        }
    }

    private void EmitExprStatement(ILProcessor processor, BoundExprStatement stmnt)
    {
        EmitExpression(processor, stmnt.Expression);
        if(stmnt.Expression.Type != TypeSymbol.Void)
            processor.Emit(OpCodes.Pop);
    }

    private void EmitExpression(ILProcessor processor, BoundExpression expr)
    {
        switch(expr)
        {
            case BoundTypeConversionExpression conv:
                EmitTypeConvesionExpression(processor, conv);
                break;
            case BoundListExpression listInit:
                EmitListExpression(processor, listInit);
                break;
            case BoundObjectInitExpression structInit:
                EmitObjectExpression(processor, structInit);
                break;
            case BoundAssignmentExpression assignment:
                EmitAssignmentExpression(processor, assignment);
                break;
            case BoundAccessExpression access:
                EmitAccessExpression(processor, access);
                break;
            case BoundCallExpression call:
                EmitCallExpression(processor, call);
                break;
            case BoundUnaryExpression unary:
                EmitUnaryExpression(processor, unary);
                break;
            case BoundBinaryExpression binary:
                EmitBinaryExpression(processor, binary);
                break;
            case BoundConstantExpression bound:
                EmitConstantExpression(processor,bound);
                break;
            case BoundNullExpression:
                processor.Emit(OpCodes.Ldnull);
                break;
            default: 
                throw new NotImplementedException($"{nameof(Emitter)} doesn't know '{expr} yet'");
        }
    }

    private void EmitTypeConvesionExpression(ILProcessor processor, BoundTypeConversionExpression conv)
    {
        EmitExpression(processor, conv.Expression);
        if(conv.Type == TypeSymbol.Int && conv.Expression.Type == TypeSymbol.Bool)
        {
            return;
        }
        if(conv.Type == TypeSymbol.Bool && conv.Expression.Type == TypeSymbol.Int)
        {
            //image 250 at top of stack
            processor.Emit(OpCodes.Ldc_I4_0);            
            processor.Emit(OpCodes.Ceq);                 //250 == 0 -> 0
            processor.Emit(OpCodes.Ldc_I4_0);            
            processor.Emit(OpCodes.Ceq);                 // 0 == 0 -> 1
            return;
        }
        if(conv.Type == TypeSymbol.String)
        {
            // TODO: FIX printing, most important for lists, but others would also be cool. So that they match interpreter. 
            var convExprType = conv.Expression.Type;
            EmitBoxIfNeeded(processor, convExprType);
            processor.Emit(OpCodes.Call, _wlToString);
            return;
        }

        if(conv.Type is ListTypeSymbol)
            return;
        
        if(conv.Expression.Type == TypeSymbol.Null) return; //null is implicit
        
        throw new NotImplementedException($"{nameof(EmitTypeConvesionExpression)} didn't know how to handle conversion '{conv.Type}' to '{conv.Expression.Type}'");
    }

    private void EmitListExpression(ILProcessor processor, BoundListExpression listInit)
    {
        var ctor = _listMethods.Empty(listInit.Type);
        var add = _listMethods.Add(listInit.Type);
        processor.Emit(OpCodes.Newobj, ctor);
        foreach(var expr in listInit.Expressions)
        {
            processor.Emit(OpCodes.Dup);
            EmitExpression(processor, expr);
            processor.Emit(OpCodes.Callvirt, add);
        }
    }

    private void EmitObjectExpression(ILProcessor processor, BoundObjectInitExpression init)
    {
        if (!_typeInfoOf.TryGetValue(init.Type, out var typeInfo))
            throw new Exception($"{nameof(Emitter)} - Something went wrong, there was no type information for '{init.Type}'");
        
        processor.Emit(OpCodes.Newobj, typeInfo.Constructor);
        foreach(var (mSymbol, expr) in init.InitializedMembers)
        {
            processor.Emit(OpCodes.Dup);
            EmitExpression(processor, expr);
            processor.Emit(OpCodes.Stfld, typeInfo.SymbolToField[mSymbol]);
        }

    }

    private void EmitAssignmentExpression(ILProcessor processor, BoundAssignmentExpression assignment)
    {
        VariableDefinition tmpVar;
        switch(assignment.Access)
        {
            case BoundNameAccess name:
                var instrBefore = GetLastInstruction(processor);
                var isVariableInClosure = BodyState.IsSharedVariable(name.Symbol);
                EmitExpression(processor,assignment.RightHandSide);
                
                //Very important to dup because the assignment will consume the value, but we want to leave a value on the stack.
                if(!isVariableInClosure) processor.Emit(OpCodes.Dup); 
                
                EmitStoreLocation(processor, name.Symbol, instrBefore);
                
                if(isVariableInClosure) EmitLoadAccess(processor, name); //must leave something to pop.
                break;
            case BoundSubscriptAccess sa:
                if(sa.Target.Type is ListTypeSymbol lts)
                {
                    var rhsType = CilTypeOf(assignment.RightHandSide.Type);
                    //introduces a local variable, because 'System.Void ArrayList::set_Item(int32,object)' eats the right-hand-side
                    //And we cannot emit the right hand side again, since it may mutate other state...
                    tmpVar = new VariableDefinition(rhsType);
                    processor.Body.Variables.Add(tmpVar);
                    EmitLoadAccess(processor, sa.Target);
                    EmitExpression(processor, sa.Index);
                    EmitExpression(processor, assignment.RightHandSide);
                    processor.Emit(OpCodes.Stloc, tmpVar);
                    processor.Emit(OpCodes.Ldloc, tmpVar);
                    processor.Emit(OpCodes.Callvirt, _listMethods.Update(lts));
                    processor.Emit(OpCodes.Ldloc, tmpVar);
                    return;
                }
                throw new NotImplementedException($"{nameof(EmitAssignmentExpression)} doesn't allow assignments into type '{assignment.Access.Type}'");
            case BoundMemberAccess bma and {Member: MemberFieldSymbol fSymbol}:
                var typeInfo = _typeInfoOf[bma.Target.Type];
                var fieldRef = typeInfo.SymbolToField[fSymbol];
                //TODO: can we avoid the temporary variable, or is it possible to reuse them?
                tmpVar = new VariableDefinition(CilTypeOf(assignment.RightHandSide.Type));
                processor.Body.Variables.Add(tmpVar);

                EmitLoadAccess(processor, bma.Target);
                EmitExpression(processor, assignment.RightHandSide);
                processor.Emit(OpCodes.Dup);
                processor.Emit(OpCodes.Stloc, tmpVar);
                processor.Emit(OpCodes.Stfld, fieldRef);
                processor.Emit(OpCodes.Ldloc, tmpVar);
                break;
            default:
                throw new NotImplementedException($"{nameof(EmitAssignmentExpression)} - doesn't know how to do {assignment.Access}");
        }
    }

    private void EmitAccessExpression(ILProcessor processor, BoundAccessExpression access) => EmitLoadAccess(processor, access.Access);

    private void EmitCallExpression(ILProcessor processor, BoundCallExpression call)
    {
        if(call.Function.IsBuiltInFunction())
        {
            EmitCallBuiltinExpression(processor, call);
            return;
        }
        foreach(var arg in call.Arguments)
        {
            EmitExpression(processor, arg);
        }

        if(call.Function is LocalFunctionSymbol f && f.RequiresClosure)
        {
            var funcDef = _funcs[f];
            for(int i = f.Parameters.Length; i < funcDef.Parameters.Count; i++)
            {

                var closureType = funcDef.Parameters[i].ParameterType;
                if(!BodyState.TryGetAvailableClosureLoadInstruction(closureType, processor, out var loadClosureInstr))
                {
                    throw new Exception($"Couldn't find '{closureType}' when calling '{f}'");            
                }
                processor.Append(loadClosureInstr);
            }
        }

        processor.Emit(OpCodes.Call, _funcs[call.Function]);
    }

    private void PrintBodyClosures()
    {
        if(BodyState.AvailableClosures is not null)
        {
            Console.WriteLine("Looking at " + BodyState.Func);
            foreach(var closure in BodyState.AvailableClosures)
            {
                Console.WriteLine("   " + closure.Key + "  - is variable? " + (closure.Value.variable is not null).ToString());
            }
        }   
    }

    private void EmitCallBuiltinExpression(ILProcessor processor, BoundCallExpression call)
    {
        var numParams = call.Function.Parameters.Length;
        if(numParams == 0)
        {
            processor.Emit(OpCodes.Call, _builtInFunctions[call.Function]);
            return;
        }
        else if(numParams == 1)
        {
            EmitExpression(processor, call.Arguments[0]);
            processor.Emit(OpCodes.Call, _builtInFunctions[call.Function]);
            return;
        }
        throw new NotImplementedException($"{nameof(Emitter)} doesn't allow builtin calls with more than 1 argument");
    }

    private void EmitUnaryExpression(ILProcessor processor, BoundUnaryExpression unary)
    {
        EmitExpression(processor, unary.Left);
        switch(unary.Operator.Kind)
        {
            case BoundUnaryOperatorKind.UnaryPlus:
                break;
            case BoundUnaryOperatorKind.UnaryMinus:
                processor.Emit(OpCodes.Neg);
                break;
            case BoundUnaryOperatorKind.LogicalNOT:
                processor.Emit(OpCodes.Ldc_I4_0);
                processor.Emit(OpCodes.Ceq);
                break;
            case BoundUnaryOperatorKind.ListRemoveLast:
                var type = CilTypeOf(unary.Type);
                var tmpVar = new VariableDefinition(type);
                processor.Body.Variables.Add(tmpVar);

                var length = _listMethods.Length(unary.Left.Type);
                var remove = _listMethods.Remove(unary.Left.Type);
                var subscr = _listMethods.Subscript(unary.Left.Type);

                processor.Emit(OpCodes.Dup);                            //Duplicate to be consumed by _listsubcript
                processor.Emit(OpCodes.Dup);                            //Duplicate, consumed by _listLength
                processor.Emit(OpCodes.Callvirt, length);          
                processor.Emit(OpCodes.Ldc_I4_1);
                processor.Emit(OpCodes.Sub);
                processor.Emit(OpCodes.Callvirt, subscr);
                processor.Emit(OpCodes.Stloc, tmpVar);                  //store away value, so we can remove the end
                
                processor.Emit(OpCodes.Dup);                            //Duplicate to be consumed by _listLength
                processor.Emit(OpCodes.Callvirt, length);
                processor.Emit(OpCodes.Ldc_I4_1);
                processor.Emit(OpCodes.Sub);          
                processor.Emit(OpCodes.Callvirt, remove);          //consumed the original list pointer and returns void

                processor.Emit(OpCodes.Ldloc, tmpVar);                  //leave value of removed on top of stack. 
                break;
            default:
                throw new NotImplementedException($"{nameof(EmitUnaryExpression)} doesn't know '{unary.Operator.Kind}' for type '{unary.Left.Type}'");
        }
    }

    private void EmitBinaryExpression(ILProcessor processor, BoundBinaryExpression binary)
    {
        var leftType = binary.Left.Type;
        var rightType = binary.Right.Type;
        if(binary.Operator.Kind == LogicAND || binary.Operator.Kind == LogicOR)
        {
            EmitLogicalAndOR(processor, binary);
            return;
        }

        if(leftType is ListTypeSymbol || rightType is ListTypeSymbol)
        {
            if(binary.Operator.Kind != ListConcat)
                EmitExpression(processor, binary.Left);

            switch(binary.Operator.Kind)
            {
                case ListConcat: 
                    var ctor = _listMethods.Empty(leftType);
                    var addMany = _listMethods.AddMany(leftType);
                    processor.Emit(OpCodes.Newobj, ctor);
                    processor.Emit(OpCodes.Dup);
                    processor.Emit(OpCodes.Dup);
                    EmitExpression(processor, binary.Left);
                    processor.Emit(OpCodes.Callvirt, addMany);
                    EmitExpression(processor, binary.Right);
                    processor.Emit(OpCodes.Callvirt, addMany);
                    break;
                case ListAdd:
                    processor.Emit(OpCodes.Dup);
                    EmitExpression(processor, binary.Right);
                    processor.Emit(OpCodes.Callvirt, _listMethods.Add(leftType));
                    break;
                case BoundBinaryOperatorKind.Equals:
                    EmitExpression(processor, binary.Right);
                    processor.Emit(OpCodes.Call, _wlEquals);
                    break;
                case NotEquals:
                    EmitExpression(processor, binary.Right);
                    processor.Emit(OpCodes.Call, _wlEquals);
                    processor.Emit(OpCodes.Ldc_I4_0);
                    processor.Emit(OpCodes.Ceq);
                    break;
                default:
                    throw new NotImplementedException($"{nameof(EmitBinaryExpression)} doesn't implement '{binary.Operator.Kind}' for '{leftType}' and '{rightType}'");
            }
            return;
        }

        EmitExpression(processor, binary.Left);
        EmitExpression(processor, binary.Right);

        if(leftType == TypeSymbol.String || rightType == TypeSymbol.String)
        {
            switch(binary.Operator.Kind)
            {
                case StringConcat:
                    processor.Emit(OpCodes.Call, _stringConcat);
                    break;
                case BoundBinaryOperatorKind.Equals:
                    processor.Emit(OpCodes.Call, _stringEqual);
                    break;
                case NotEquals:
                    processor.Emit(OpCodes.Call, _stringEqual);
                    processor.Emit(OpCodes.Ldc_I4_0);
                    processor.Emit(OpCodes.Ceq);
                    break;
            }
            return;
        }
        
        switch(binary.Operator.Kind)
        {
            case Addition:
                processor.Emit(OpCodes.Add);
                break;
            case Multiplication:
                processor.Emit(OpCodes.Mul);
                break;
            case Division:
                processor.Emit(OpCodes.Div);
                break;
            case Subtraction:
                processor.Emit(OpCodes.Sub);
                break;
            case BoundBinaryOperatorKind.Equals:
                processor.Emit(OpCodes.Ceq);
                break;
            case NotEquals:
                // x != y   is the same as  (x = y) == false
                processor.Emit(OpCodes.Ceq);
                processor.Emit(OpCodes.Ldc_I4_0);
                processor.Emit(OpCodes.Ceq);
                break;
            case LessThan:
                processor.Emit(OpCodes.Clt);
                break;
            case LessThanEqual:
                // x <= y  is the same as ((y < x) == false) 
                processor.Emit(OpCodes.Cgt);
                processor.Emit(OpCodes.Ldc_I4_0);
                processor.Emit(OpCodes.Ceq);
                break;
            case GreaterThan:
                processor.Emit(OpCodes.Cgt);
                break;
            case GreaterThanEqual:
                // x >= y  is the same as ((x < y) == false) 
                processor.Emit(OpCodes.Clt);
                processor.Emit(OpCodes.Ldc_I4_0);
                processor.Emit(OpCodes.Ceq);
                break;
        }
        
    }

    private void EmitLogicalAndOR(ILProcessor processor, BoundBinaryExpression binary)
    {
        EmitExpression(processor, binary.Left);
        var kind = binary.Operator.Kind;
        if(kind == LogicAND)
        {
            // x && y
            //   Emit(x)
            //   brfalse push0
            //   Emit(y)
            //   br end&&   
            // push0:
            //   ldc.i4.0
            // end&&:
            var brEarlyFalse = processor.Create(OpCodes.Brfalse, processor.Create(OpCodes.Nop));
            processor.Append(brEarlyFalse);
            EmitExpression(processor, binary.Right);
            var brdone = processor.Create(OpCodes.Br, processor.Create(OpCodes.Nop));
            processor.Append(brdone);
            var push0 = processor.Create(OpCodes.Ldc_I4_0);
            processor.Append(push0);
            var end = processor.Create(OpCodes.Nop);
            processor.Append(end);
            brEarlyFalse.Operand = push0;
            brdone.Operand = end;
        }
        else 
        {
            // x || y:
            //   Emit(x)
            //   brtrue push1   <-- Consumes the x value
            //   Emit(y)
            //   br end||       <-- Doesn't consume y
            // push1:
            //   ldc.i4.1
            // end||:
            var brEarlyTrue = processor.Create(OpCodes.Brtrue, processor.Create(OpCodes.Nop));
            processor.Append(brEarlyTrue);
            EmitExpression(processor, binary.Right);
            var brdone = processor.Create(OpCodes.Br, processor.Create(OpCodes.Nop));
            processor.Append(brdone);
            var push1 = processor.Create(OpCodes.Ldc_I4_1);
            processor.Append(push1);
            var end = processor.Create(OpCodes.Nop); //TODO: Could this be an "awaitinglabels"?
            processor.Append(end);
            brEarlyTrue.Operand = push1;
            brdone.Operand = end;
        }
    }

    private static void EmitConstantExpression(ILProcessor processor, BoundConstantExpression bound)
    {

        if(bound.Type == TypeSymbol.String)
        {
            processor.Emit(OpCodes.Ldstr, bound.Constant.GetCastValue<string>());
        }
        if(bound.Type == TypeSymbol.Bool)
        {
            if(bound.Constant.GetCastValue<bool>())
                processor.Emit(OpCodes.Ldc_I4_1);
            else 
                processor.Emit(OpCodes.Ldc_I4_0);
        }
        if(bound.Type == TypeSymbol.Int)
        {
            var val = bound.Constant.GetCastValue<int>();
            processor.Emit(OpCodes.Ldc_I4, val);
        }   
    }

    private void EmitStoreLocation(ILProcessor processor, VariableSymbol variable, Instruction? before = null)
    {
        if(variable is ScopedVariableSymbol sv && BodyState.IsSharedVariable(sv))
        {
            var (ldClosure, stfld) = GetClosureVariableAccess(processor, sv, OpCodes.Stfld);
            if(before is not null)
                processor.InsertAfter(before, ldClosure);
            else throw new Exception($"{nameof(EmitStoreLocation)} must be supplied with the instruction before to store into a closure from its origin scope \\0/");
            processor.Append(stfld);
            return;
        }

        switch(variable)
        {
            case ParameterSymbol ps:
                processor.Emit(OpCodes.Starg, ps.Placement);
                return;
            case GlobalVariableSymbol gs:
                processor.Emit(OpCodes.Stsfld, _globals[gs]);
                return;
            case LocalVariableSymbol ls:
                var variableDef = BodyState.Locals[variable];
                processor.Emit(OpCodes.Stloc, variableDef);
                return;
        }
    }

    private void EmitLoadAccess(ILProcessor processor, BoundAccess acc)
    {
        switch(acc)
        {
            case BoundNameAccess name:
                var nameSymbol = name.Symbol;
                if(nameSymbol is ScopedVariableSymbol sv && BodyState.IsSharedVariable(sv))
                {
                    var (closureLoad, fieldLoad) = GetClosureVariableAccess(processor, sv, OpCodes.Ldfld);
                    processor.Append(closureLoad);
                    processor.Append(fieldLoad);
                    return;
                }
                switch(nameSymbol)
                {
                    case ParameterSymbol ps:
                        processor.Emit(OpCodes.Ldarg, ps.Placement);
                        return;
                    case GlobalVariableSymbol gs:
                        var variable = _globals[gs];
                        processor.Emit(OpCodes.Ldsfld, variable);
                        return;
                    case LocalVariableSymbol ls when BodyState.Locals.TryGetValue(nameSymbol, out var local):
                        processor.Emit(OpCodes.Ldloc, local);
                        return;
                    default:
                        throw new Exception($"{nameof(Emitter)}.{nameof(EmitLoadAccess)} couldn't find '{nameSymbol.Name}'");
                }
            case BoundMemberAccess mba:
                var access = mba.Target;
                EmitLoadAccess(processor, access);
                if(mba.Member.IsBuiltin)
                {
                    EmitBuiltinTypeMember(processor, access.Type, mba.Member);
                    return;
                }
                if(!_typeInfoOf[access.Type].SymbolToField.TryGetValue(mba.Member, out var @field))
                    throw new Exception($"{nameof(Emitter)} - Something went wrong, could not find field definition for '{access.Type}.{mba.Member}'");
                processor.Emit(OpCodes.Ldfld, @field);
                return;
                //throw new NotImplementedException($"{nameof(Emitter)} doesn't know how to emit '{access.Type}.{mba.Member}' yet");
            case BoundSubscriptAccess sa:
                if(sa.Target.Type == TypeSymbol.String || sa.Target.Type is ListTypeSymbol)
                {
                    EmitLoadAccess(processor, sa.Target);
                    EmitExpression(processor, sa.Index);
                    if(sa.Target.Type == TypeSymbol.String)
                        processor.Emit(OpCodes.Callvirt, _stringSubscript);
                    else if(sa.Target.Type is ListTypeSymbol lts)
                    {
                        processor.Emit(OpCodes.Callvirt, _listMethods.Subscript(lts));
                    }
                    return;
                }
                throw new NotImplementedException($"{nameof(EmitLoadAccess)} doesn't do subscripting for '{sa.Target.Type.Name}' yet");
            case BoundExprAccess ae:
                EmitExpression(processor, ae.Expression);
                break;

        }
    }

    private void EmitBuiltinTypeMember(ILProcessor processor, TypeSymbol type, MemberSymbol member)
    {
        if(member.Name == "len")
        {
            if(type == TypeSymbol.String)
            {
                processor.Emit(OpCodes.Callvirt, _builtInFunctions[BuiltInFunctions.StrLen]);
                return;
            }
            else if(type is ListTypeSymbol)
            {
                processor.Emit(OpCodes.Callvirt, _listMethods.Length(type));
                return;
            }
        }
        throw new NotImplementedException($"{nameof(Emitter)}-{nameof(EmitBuiltinTypeMember)} doesn't know '{type}.{member}'");
    }

    private void EmitBoxIfNeeded(ILProcessor processor, TypeSymbol previousExprType)
    {
        if(previousExprType.NeedsBoxing())
            processor.Emit(OpCodes.Box, CilTypeOf(previousExprType));
    }

    private Instruction GetLastInstruction(ILProcessor processor)
    {
        var instructions = processor.Body.Instructions;
        //Insert an NOP if no other instructions exists
        if(instructions.Count <= 0) processor.Emit(OpCodes.Nop);
        return instructions[^1];
    }

    private (Instruction LoadClosureInstr, Instruction fieldInstr) GetClosureVariableAccess(ILProcessor processor, ScopedVariableSymbol scoped, OpCode action)
    {
        var varClosure = _funcClosure[scoped.BelongsTo!];
        if(!BodyState.TryGetAvailableClosureLoadInstruction(varClosure, processor, out var loadinstr))
            throw new Exception($"{nameof(GetClosureVariableAccess)} couldn't find '{varClosure}' for '{BodyState.Func}'");
        
        var loadField = processor.Create(action, varClosure.GetFieldOrThrow(scoped));
        return (loadinstr, loadField);
    }

    private void SetupClosureState(FunctionBodyState stateOfFuncBody, FunctionSymbol func, ILProcessor ilProcessor)
    {
        if (stateOfFuncBody.SharedLocals.Count > 0) //Does the function share locals? Then add a closure
        {
            var closureType = new TypeDefinition("", $"closure_{func.Name}", TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass, CilTypeOf(CILClosureType));
            _program.NestedTypes.Add(closureType);
            var closureVariable = new VariableDefinition(closureType);
            ilProcessor.Body.Variables.Add(closureVariable);

            var closure = new ClosureState(closureType, closureVariable, stateOfFuncBody);
            stateOfFuncBody.AddClosureVariable(_funcClosure[func] = closure);

            foreach (var shared in stateOfFuncBody.SharedLocals)
            {
                var @field = new FieldDefinition(shared.Name, FieldAttributes.Public, CilTypeOf(shared.Type));
                stateOfFuncBody.AddClosureField(@field);
                if (shared is ParameterSymbol p)
                {
                    ilProcessor.Emit(OpCodes.Ldloca, closureVariable);
                    ilProcessor.Emit(OpCodes.Ldarg, p.Placement);
                    ilProcessor.Emit(OpCodes.Stfld, @field);
                }
            }
        }
        if (func is LocalFunctionSymbol l && l.Closure is { Count: > 0 })
        {
            stateOfFuncBody.SharedLocals.UnionWith(l.Closure);
            foreach (var cVar in l.Closure)
            {
                var cVarClosure = _funcClosure[cVar.BelongsToOrThrow];
                if (!EmitterClosureHelper.TryFindMatchingClosureParameter(_funcs, func, cVarClosure, out var paramDef))
                {
                    paramDef = EmitterClosureHelper.CreateMatchingClosureParameter(_funcs, func, cVarClosure);
                }
                stateOfFuncBody.AddAvailableClosure(cVarClosure, paramDef);
            }
        }
    }

    private void PrintFuncBody(FunctionSymbol func, ILProcessor ilProcessor)
    {
        Console.WriteLine($"-- FUNCTION '{func}'");
        foreach (var parameter in _funcs[func].Parameters)
        {
            Console.Write("    ");
            Console.WriteLine($"arg: {parameter.Index} - {parameter.ParameterType}");
        }
        foreach (var variable in ilProcessor.Body.Variables)
        {
            Console.Write("    ");
            Console.WriteLine($"loc: {variable.Index} - {variable.VariableType}");
        }
        foreach (var instr in ilProcessor.Body.Instructions)
        {
            Console.WriteLine(instr);
        }
        Console.WriteLine($"-- END      '{func.Name}'");
    }
}