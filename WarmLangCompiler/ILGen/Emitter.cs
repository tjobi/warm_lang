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

public sealed class Emitter{

    private readonly ErrorWarrningBag _diag;
    private readonly bool _debug;
    
    private static int _localFuncId = 0;

    private readonly ImmutableDictionary<FunctionSymbol, MethodReference> _builtInFunctions;
    private readonly MethodReference _stringConcat, _stringEqual, _stringSubscript;
    private readonly MethodReference _listEmpty, _listAdd, _listRemove, _listSubscript, _listSet, _listAddMany, _listLength;
    private readonly MethodReference _objectEquals, _wlEquals, _wlToString;
    
    private readonly Dictionary<FunctionSymbol, MethodDefinition> _funcs;

    private readonly Stack<FunctionBodyState> _bodyStateStack;

    private readonly TypeDefinition _globalsType;
    private readonly Dictionary<VariableSymbol, FieldDefinition> _globals;

    //mono.cecil stuff?
    private AssemblyDefinition _assemblyDef;
    private TypeDefinition _program;
    private AssemblyDefinition _mscorlib;

    private readonly Dictionary<TypeSymbol, TypeReference> _cilTypes;
    private TypeReference CilTypeOf(TypeSymbol type) => _cilTypes[type.AsRecognisedType()];

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
        _cilTypes = new();
        _bodyStateStack = new();
        _globals = new();

        // IL -> ".assembly 'App' {}"
        var assemblyName = new AssemblyNameDefinition("App", new Version(1,0));
        _assemblyDef = AssemblyDefinition.CreateAssembly(assemblyName, "warmlang", ModuleKind.Console);
        
        // IL -> ".assembly extern mscorlib {}"
        _mscorlib = ReadMscorlib();
        
        foreach(var type in BuiltInTypes())
        {
            foreach(var module in _mscorlib.Modules)
            {
                var t = module.GetType(type.ToCilName());
                if(t is not null)
                {
                    _cilTypes[type] = _assemblyDef.MainModule.ImportReference(t);
                    break;
                }
            }
        }

        // IL -> ".class private auto ansi beforefieldinit abstract sealed Program extends [mscorlib]System.Object {"
        var systemObject = _assemblyDef.MainModule.ImportReference(_cilTypes[CILBaseTypeSymbol]);
        _program = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, systemObject);
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
        var globalsType = new TypeDefinition("", "GLOBALS", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit 
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

        // TODO: program.TypeMemberInformation still holds all the type information
        //GetFunctionSymbols just skips the need for multiple loops (for now at least)
        foreach(var (func, body) in program.GetFunctionSymbolsAndBodies())
        {
            EmitFunctionBody(func, body);
        }
        
        var main = _funcs[program.MainFunc is not null ? program.MainFunc : program.ScriptMain!];

        _assemblyDef.EntryPoint = main;
        _assemblyDef.Write(outfile);
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
        var thisState = new FunctionBodyState(func);
        _bodyStateStack.Push(thisState);
        var funcDefintion = _funcs[func];
        var ilProcessor = funcDefintion.Body.GetILProcessor();

        if(func is not LocalFunctionSymbol)
        {
            foreach(var stmnt in body.Statements)
            {
                if(stmnt is BoundFunctionDeclaration decl && decl.Symbol is LocalFunctionSymbol lf && lf.RequiresClosure)
                {
                    if(thisState.ClosureType is null)
                    {
                        var closureType = new TypeDefinition("", $"closure_{func.Name}", TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass, 
                                                             CilTypeOf(CILClosureType));

                        _program.NestedTypes.Add(closureType);
                        
                        var closureVariable = new VariableDefinition(closureType);
                        ilProcessor.Body.Variables.Add(closureVariable);
                        
                        thisState.AddClosure(closureType, closureVariable);
                    }
                    foreach(var c in lf.Closure)
                    {
                        var @field = new FieldDefinition(c.Name, FieldAttributes.Public, CilTypeOf(c.Type));
                        thisState.AddClosureField(@field);
                    }
                }
            }
        }

        EmitBlockStatement(ilProcessor, body);


        foreach(var (instrIdx, label) in thisState.AwaitingLabels)
        {
            var instr = ilProcessor.Body.Instructions[instrIdx];
            var targetInstr = thisState.Labels[label];
            instr.Operand = targetInstr;
        }
        ilProcessor.Body.OptimizeMacros();

        _bodyStateStack.Pop();

        if(_debug)
        {
            Console.WriteLine($"-- FUNCTION '{func}'");
            foreach(var parameter in _funcs[func].Parameters)
            {
                Console.Write("    ");
                Console.WriteLine($"arg: {parameter.Index} - {parameter.ParameterType}");
            }
            foreach(var variable in ilProcessor.Body.Variables)
            {
                Console.Write("    ");
                Console.WriteLine($"loc: {variable.Index} - {variable.VariableType}");
            }
            foreach(var instr in ilProcessor.Body.Instructions)
            {
                Console.WriteLine(instr);
            }
            Console.WriteLine($"-- END      '{func.Name}'");
        }
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
        if(symbol.IsFree && BodyState.TryGetClosureField(symbol, out var fieldDef))
        {  
            BodyState.SharedLocals[symbol] = fieldDef;
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

        if(func.RequiresClosure)
        {   
            var closureType = BodyState.ClosureType.MakeByReferenceType();
            var closureParam = new ParameterDefinition("closure", ParameterAttributes.None, closureType);
            funcDefintion.Parameters.Add(closureParam);
        }
        
        foreach(var @param in func.Parameters)
        {
            var paramDef = new ParameterDefinition(@param.Name, ParameterAttributes.None, CilTypeOf(@param.Type));
            funcDefintion.Parameters.Add(paramDef);
        }
        _funcs[func] = funcDefintion;
        _program.Methods.Add(funcDefintion);
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
        
        throw new NotImplementedException($"{nameof(EmitTypeConvesionExpression)} didn't know how to handle conversion '{conv.Type}' to '{conv.Expression.Type}'");
    }

    private void EmitListExpression(ILProcessor processor, BoundListExpression listInit)
    {
        processor.Emit(OpCodes.Newobj, _listEmpty);
        foreach(var expr in listInit.Expressions)
        {
            processor.Emit(OpCodes.Dup);
            EmitExpression(processor, expr);
            EmitBoxIfNeeded(processor, expr.Type);
            processor.Emit(OpCodes.Callvirt, _listAdd);
            processor.Emit(OpCodes.Pop);  // System.Int32 ArrayList::Add(System.Object)  
        }
    }

    private void EmitAssignmentExpression(ILProcessor processor, BoundAssignmentExpression assignment)
    {
        switch(assignment.Access)
        {
            case BoundNameAccess name:
                var instrBefore = GetLastInstruction(processor);
                var isVariableInClosure = name.Symbol.IsFree;
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
                    var tmpVar = new VariableDefinition(rhsType);
                    processor.Body.Variables.Add(tmpVar);
                    EmitLoadAccess(processor, sa.Target);
                    EmitExpression(processor, sa.Index);
                    EmitExpression(processor, assignment.RightHandSide);
                    processor.Emit(OpCodes.Stloc, tmpVar);
                    processor.Emit(OpCodes.Ldloc, tmpVar);
                    EmitBoxIfNeeded(processor, assignment.RightHandSide.Type);
                    processor.Emit(OpCodes.Callvirt, _listSet);
                    processor.Emit(OpCodes.Ldloc, tmpVar);
                    return;
                }
                throw new NotImplementedException($"{nameof(EmitAssignmentExpression)} doesn't allow assignments into type '{assignment.Access.Type}'");
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

        if(call.Function is LocalFunctionSymbol f && f.RequiresClosure)
        {
            processor.Emit(OpCodes.Ldloca, BodyState.ClosureVariable);
        }

        foreach(var arg in call.Arguments)
        {
            EmitExpression(processor, arg);
        }
        processor.Emit(OpCodes.Call, _funcs[call.Function]);
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

                processor.Emit(OpCodes.Dup);                            //Duplicate to be consumed by _listsubcript
                processor.Emit(OpCodes.Dup);                            //Duplicate, consumed by _listLength
                processor.Emit(OpCodes.Callvirt, _listLength);          
                processor.Emit(OpCodes.Ldc_I4_1);
                processor.Emit(OpCodes.Sub);
                processor.Emit(OpCodes.Callvirt, _listSubscript);
                processor.Emit(OpCodes.Unbox_Any, type);                //Prepare return value from unary expression
                processor.Emit(OpCodes.Stloc, tmpVar);                  //store away value, so we can remove the end
                
                processor.Emit(OpCodes.Dup);                            //Duplicate to be consumed by _listLength
                processor.Emit(OpCodes.Callvirt, _listLength);
                processor.Emit(OpCodes.Ldc_I4_1);
                processor.Emit(OpCodes.Sub);          
                processor.Emit(OpCodes.Callvirt, _listRemove);          //consumed the original list pointer and returns void

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
        if(binary.Operator.Kind == LogicAND || binary.Operator.Kind == LogicaOR)
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
                    processor.Emit(OpCodes.Newobj, _listEmpty);
                    processor.Emit(OpCodes.Dup);
                    processor.Emit(OpCodes.Dup);
                    EmitExpression(processor, binary.Left);
                    processor.Emit(OpCodes.Callvirt, _listAddMany);
                    EmitExpression(processor, binary.Right);
                    processor.Emit(OpCodes.Callvirt, _listAddMany);
                    break;
                case ListAdd:
                    processor.Emit(OpCodes.Dup);
                    EmitExpression(processor, binary.Right);
                    EmitBoxIfNeeded(processor, binary.Right.Type);
                    processor.Emit(OpCodes.Callvirt, _listAdd);
                    processor.Emit(OpCodes.Pop);
                    break;
                //TODO: Both NotEquals and Equals uses reference equality! we want the element equality.
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
        if(variable is ParameterSymbol ps)
        {
            processor.Emit(OpCodes.Starg, ps.Placement);
            return;
        }
        if(variable is GlobalVariableSymbol gs)
        {
            processor.Emit(OpCodes.Stsfld, _globals[gs]);
            return;
        }
        if(variable.IsFree)
        {
            if(BodyState.Func is LocalFunctionSymbol f && f.RequiresClosure)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Stfld, ParentState().GetClosureField(variable));
            } else 
            {
                var loadAddress = processor.Create(OpCodes.Ldloca, BodyState.ClosureVariable);
                if(before is not null)
                {
                    processor.InsertAfter(before, loadAddress);
                } 
                else throw new Exception($"{nameof(EmitStoreLocation)} must be supplied with the instruction before to store into a closure from its origin scope \\0/");
                processor.Emit(OpCodes.Stfld, BodyState.GetClosureField(variable));
            }
            
            return;
        }
        var variableDef = BodyState.Locals[variable];
        processor.Emit(OpCodes.Stloc, variableDef);
    }

    private void EmitLoadAccess(ILProcessor processor, BoundAccess acc)
    {
        switch(acc)
        {
            case BoundNameAccess name:
                var nameSymbol = name.Symbol;
                if(nameSymbol is ParameterSymbol ps)
                {
                    processor.Emit(OpCodes.Ldarg, ps.Placement);
                } 
                else if(nameSymbol is GlobalVariableSymbol gs) {
                    var variable = _globals[gs];
                    processor.Emit(OpCodes.Ldsfld, variable);
                } 
                else {
                    var locals = BodyState.Locals;
                    if(locals.TryGetValue(nameSymbol, out var variable))
                        processor.Emit(OpCodes.Ldloc, variable);
                    else if(nameSymbol.IsFree)
                    {
                        if(BodyState.Func is LocalFunctionSymbol f && f.RequiresClosure)
                        {
                            processor.Emit(OpCodes.Ldarg_0);
                            processor.Emit(OpCodes.Ldfld, ParentState().GetClosureField(nameSymbol));
                        } else 
                        {
                            processor.Emit(OpCodes.Ldloca, BodyState.ClosureVariable);
                            processor.Emit(OpCodes.Ldfld, BodyState.GetClosureField(nameSymbol));
                        }
                    }
                    else throw new Exception($"{nameof(Emitter)}.{nameof(EmitLoadAccess)} couldn't find '{nameSymbol.Name}'");
                }
                break;
            case BoundMemberAccess mba:
                var access = mba.Target;
                EmitLoadAccess(processor, access);
                if(mba.Member.IsBuiltin)
                {
                    EmitBuiltinTypeMember(processor, access.Type, mba.Member);
                    return;
                }
                throw new NotImplementedException($"{nameof(Emitter)} doesn't know how to emit '{access.Type}.{mba.Member}' yet");
            case BoundSubscriptAccess sa:
                if(sa.Target.Type == TypeSymbol.String || sa.Target.Type is ListTypeSymbol)
                {
                    EmitLoadAccess(processor, sa.Target);
                    EmitExpression(processor, sa.Index);
                    if(sa.Target.Type == TypeSymbol.String)
                        processor.Emit(OpCodes.Callvirt, _stringSubscript);
                    else if(sa.Target.Type is ListTypeSymbol lts)
                    {
                        processor.Emit(OpCodes.Callvirt, _listSubscript);
                        processor.Emit(OpCodes.Unbox_Any, CilTypeOf(lts.InnerType));
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
                processor.Emit(OpCodes.Callvirt, _listLength);
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

}