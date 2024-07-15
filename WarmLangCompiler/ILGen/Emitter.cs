namespace WarmLangCompiler.ILGen;
using WarmLangCompiler.Binding;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.ErrorReporting;
using static WarmLangCompiler.Binding.BoundBinaryOperatorKind;

using Mono.Cecil;
using Mono.Cecil.Cil;
using WarmLangCompiler.Binding.Lower;
using Mono.Cecil.Rocks;
using System.Collections.Immutable;

public sealed class Emitter{

    private readonly ErrorWarrningBag _diag;
    private readonly Dictionary<TypeSymbol, TypeReference> _cilTypes;
    private readonly ImmutableDictionary<FunctionSymbol, MethodReference> _builtInFunctions;
    private readonly MethodReference _toStringConvert, _stringConcat, _stringEqual, _stringSubscript;
    
    private readonly Dictionary<FunctionSymbol, MethodDefinition> _funcs;
    private readonly Dictionary<BoundLabel, Instruction> _labels;
    private readonly Dictionary<VariableSymbol, VariableDefinition> _locals;
    private readonly Dictionary<int, BoundLabel> _awaitingLabels;

    //mono.cecil stuff?
    private AssemblyDefinition _assemblyDef;
    private TypeDefinition _program;

    private static readonly TypeSymbol _cilBaseType = new("unkown");
    private static readonly (TypeSymbol type, string cilName)[] _builtInTypes = new (TypeSymbol, string)[]
    {
        (TypeSymbol.Int, "System.Int32"),       (TypeSymbol.Bool, "System.Boolean"), 
        (TypeSymbol.String, "System.String"),   (TypeSymbol.Void, "System.Void"), 
        (_cilBaseType, "System.Object"),
    };

    private Emitter(ErrorWarrningBag diag)
    {
        _diag = diag;
        _funcs = new();
        _cilTypes = new();
        _labels = new();
        _locals = new();
        _awaitingLabels = new();

        // IL -> ".assembly 'App' {}"
        var assemblyName = new AssemblyNameDefinition("App", new Version(1,0));
        _assemblyDef = AssemblyDefinition.CreateAssembly(assemblyName, "warmlang", ModuleKind.Console);
        
        //TODO: A hardcoded necessary file path is not very sexy
        // IL -> ".assembly extern mscorlib {}"
        var mscorlib = AssemblyDefinition.ReadAssembly("/usr/lib/mono/4.8-api/mscorlib.dll");

        foreach(var (type, cilName) in _builtInTypes)
        {
            foreach(var module in mscorlib.Modules)
            {
                var t = module.GetType(cilName);
                if(t is not null)
                {
                    _cilTypes[type] = _assemblyDef.MainModule.ImportReference(t);
                    break;
                }
            }
        }
        // IL -> ".class private auto ansi beforefieldinit abstract sealed Program extends [mscorlib]System.Object {"
        var systemObject = _assemblyDef.MainModule.ImportReference(_cilTypes[_cilBaseType]);
        _program = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, systemObject);
        _assemblyDef.MainModule.Types.Add(_program);
        _builtInFunctions = ResolveBuiltInMethods(mscorlib);

        //TODO: temp to string
        var dotnetConvert = mscorlib.MainModule.GetType("System.Convert");
        _toStringConvert = GetMethodFromTypeDefinition(dotnetConvert, "ToString", new[]{"System.Object"});
        
        var dotnetString = mscorlib.MainModule.GetType("System.String");
        _stringConcat = GetMethodFromTypeDefinition(dotnetString, "Concat", new[]{"System.String", "System.String"});
        _stringEqual = GetMethodFromTypeDefinition(dotnetString, "op_Equality", new[]{"System.String", "System.String"});
        _stringSubscript = GetMethodFromTypeDefinition(dotnetString, "get_Chars", new[]{"System.Int32"});
    }

    private ImmutableDictionary<FunctionSymbol, MethodReference> ResolveBuiltInMethods(AssemblyDefinition mscorlib)
    {
        var builder = ImmutableDictionary.CreateBuilder<FunctionSymbol,MethodReference>();
        var dotnetConsole = mscorlib.MainModule.GetType("System.Console");
        builder[BuiltInFunctions.StdWriteLine]  = GetMethodFromTypeDefinition(dotnetConsole, "WriteLine", new string[]{"System.String"});
        builder[BuiltInFunctions.StdWrite]      = GetMethodFromTypeDefinition(dotnetConsole, "Write", new[]{"System.String"});
        builder[BuiltInFunctions.StdWriteC]     = GetMethodFromTypeDefinition(dotnetConsole, "Write", new[]{"System.Char"});
        builder[BuiltInFunctions.StdRead]       = GetMethodFromTypeDefinition(dotnetConsole, "ReadLine", Array.Empty<string>());
        builder[BuiltInFunctions.StdClear]      = GetMethodFromTypeDefinition(dotnetConsole, "Clear", Array.Empty<string>());
        var dotnetString = mscorlib.MainModule.GetType("System.String");
        builder[BuiltInFunctions.StrLen]        = GetMethodFromTypeDefinition(dotnetString, "get_Length", Array.Empty<string>());
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

    public static void EmitProgram(BoundProgram program, ErrorWarrningBag diag)
    {
        var emitter = new Emitter(diag);
        if(emitter._diag.Any())
            return;
        var outfile = Path.Combine(Directory.GetCurrentDirectory(), "out.dll");
        if(!Path.Exists(outfile))
            File.Create(outfile);
        else
            File.WriteAllText(outfile, string.Empty);
        emitter.EmitProgram(outfile, program);
    }

    private void EmitProgram(string outfile, BoundProgram program)
    {
        if(_diag.Any())
            return; //something went wrong in constructor
        
        foreach(var (func,_) in program.Functions)
        {
            EmitFunctionDeclaration(func);
        }

        foreach(var (func, body) in program.Functions)
        {
            EmitFunctionBody(func, body);
        }

        //TODO: What to do for entrypoint?
        // _writer.WriteLine(".method private hidebysig static void Main ( string[] args) cil managed {");
        // _writer.WriteLine(".entrypoint");
        var main = program.Functions
                   .Where(f => f.Key.Name == "main")
                   .Select(f => _funcs[f.Key])
                   .FirstOrDefault() ?? throw new Exception("UH OH - no main?");
        _assemblyDef.EntryPoint = main;
        _assemblyDef.Write(outfile);
    }

    private void EmitFunctionDeclaration(FunctionSymbol func)
    {
        if(func is LocalFunctionSymbol)
            throw new NotImplementedException("Emitter doesn't support local functions just yet");
        var funcDefintion = new MethodDefinition(func.Name, MethodAttributes.Static | MethodAttributes.Private, _cilTypes[func.Type]);
        
        foreach(var @param in func.Parameters)
        {
            var paramDef = new ParameterDefinition(@param.Name, ParameterAttributes.None, _cilTypes[@param.Type]);
            funcDefintion.Parameters.Add(paramDef);
        }
        _funcs[func] = funcDefintion;
        _program.Methods.Add(funcDefintion);
    }

    private void EmitFunctionBody(FunctionSymbol func, BoundBlockStatement body)
    {
        _labels.Clear();
        _locals.Clear();
        _awaitingLabels.Clear();

        var funcDefintion = _funcs[func];

        var ilProcessor = funcDefintion.Body.GetILProcessor();
        EmitBlockStatement(ilProcessor, body);


        foreach(var (instrIdx, label) in _awaitingLabels)
        {
            var instr = ilProcessor.Body.Instructions[instrIdx];
            var targetInstr = _labels[label];
            instr.Operand = targetInstr;
        }
        ilProcessor.Body.OptimizeMacros();
        foreach(var instr in ilProcessor.Body.Instructions)
        {
            Console.WriteLine(instr);
        }
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
            case BoundExprStatement expr:
                EmitExprStatement(processor, expr);
                break;
            case BoundFunctionDeclaration func:
                throw new NotImplementedException($"{nameof(Emitter)} doesn't allow local functions yet!");
            default: 
                throw new NotImplementedException($"{nameof(Emitter)} doesn't know '{statement} yet'");
        }
    }

    private void EmitVariableDeclaration(ILProcessor processor, BoundVarDeclaration varDecl)
    {
        var variable = new VariableDefinition(_cilTypes[varDecl.Symbol.Type]);
        _locals[varDecl.Symbol] = variable;
        processor.Body.Variables.Add(variable);
        
        EmitExpression(processor, varDecl.RightHandSide);
        EmitStoreLocation(processor, varDecl.Symbol);
    }

    private void EmitConditionalGotoStatement(ILProcessor processor, BoundConditionalGotoStatement condGoto)
    {
        EmitExpression(processor, condGoto.Condition);
        var instruction = processor.Body.Instructions.Count;
        if(condGoto.FallsThroughTrueBranch)
        {
            _awaitingLabels[instruction] = condGoto.LabelFalse;
            processor.Emit(OpCodes.Brfalse, processor.Create(OpCodes.Nop));
        }
        else {
            _awaitingLabels[instruction] = condGoto.LabelTrue;
            processor.Emit(OpCodes.Brtrue, processor.Create(OpCodes.Nop));
        }
        
    }

    private void EmitGotoStatement(ILProcessor processor, BoundGotoStatement gotoo)
    {
        _awaitingLabels[processor.Body.Instructions.Count] = gotoo.Label;
        processor.Emit(OpCodes.Br, processor.Create(OpCodes.Nop));
    }

    private void EmitLabelStatement(ILProcessor processor, BoundLabelStatement label)
    {
        //dump a nop we can jump to?
        var nop = processor.Create(OpCodes.Nop);
        processor.Append(nop);
        _labels[label.Label] = nop;
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
            var convExprType = conv.Expression.Type;
            var needsBoxing = convExprType == TypeSymbol.Bool || convExprType == TypeSymbol.Int;
            if(needsBoxing)
                processor.Emit(OpCodes.Box, _cilTypes[convExprType]);
            processor.Emit(OpCodes.Call, _toStringConvert);
            return;
        }

        if(conv.Type is ListTypeSymbol)
            return;
        
        throw new NotImplementedException($"{nameof(EmitTypeConvesionExpression)} didn't know how to handle conversion '{conv.Type}' to '{conv.Expression.Type}'");
    }

    private void EmitListExpression(ILProcessor processor, BoundListExpression listInit)
    {
        throw new NotImplementedException();
    }

    private void EmitAssignmentExpression(ILProcessor processor, BoundAssignmentExpression assignment)
    {
        switch(assignment.Access)
        {
            case BoundNameAccess name:
                EmitExpression(processor,assignment.RightHandSide);
                //Very important to dup because the assignment will consume the value, but we want to leave a value on the stack.
                processor.Emit(OpCodes.Dup); 
                EmitStoreLocation(processor, name.Symbol);
                break;
            case BoundSubscriptAccess sa:
                throw new NotImplementedException($"{nameof(EmitAssignmentExpression)} doesn't allow assigning when subscripting");
        }
    }

    private void EmitAccessExpression(ILProcessor processor, BoundAccessExpression access)
    {
        EmitAccess(access.Access);
        void EmitAccess(BoundAccess acc)
        {
            switch(acc)
            {
                case BoundNameAccess name:
                    if(name.Symbol is ParameterSymbol ps)
                    {
                        processor.Emit(OpCodes.Ldarg, ps.Placement);
                    } else {
                        var variable = _locals[name.Symbol];
                        processor.Emit(OpCodes.Ldloc, variable);
                    }
                    break;
                case BoundSubscriptAccess sa:
                    if(sa.Target.Type == TypeSymbol.String)
                    {
                        EmitAccess(sa.Target);
                        EmitExpression(processor, sa.Index);
                        processor.Emit(OpCodes.Callvirt, _stringSubscript);
                        return;
                    }

                    throw new NotImplementedException($"{nameof(EmitAccessExpression)} doesn't do subscripting for '{sa.Target.Type.Name}' yet");
                case BoundExprAccess ae:
                    throw new NotImplementedException($"{nameof(EmitAccessExpression)} doesn't allow access to expressions yet");
            }
        }
    }

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
            processor.Emit(OpCodes.Nop);
            return;
        }
        throw new NotImplementedException($"{nameof(Emitter)} doesn't allow builtin calls with more than 1 argument");
    }

    private void EmitUnaryExpression(ILProcessor processor, BoundUnaryExpression unary)
    {
        throw new NotImplementedException();
    }

    private void EmitBinaryExpression(ILProcessor processor, BoundBinaryExpression binary)
    {
        var leftType = binary.Left.Type;
        var rightType = binary.Right.Type;
        if(leftType is ListTypeSymbol || rightType is ListTypeSymbol)
        {
            throw new NotImplementedException($"Not implemented for {binary.Left.Type.Name}");
        }
        if(binary.Operator.Kind == LogicAND || binary.Operator.Kind == LogicaOR)
        {
            EmitLogicalAndOR(processor, binary);
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

    private void EmitStoreLocation(ILProcessor processor, VariableSymbol variable)
    {
        if(variable is ParameterSymbol ps)
        {
            processor.Emit(OpCodes.Starg, ps.Placement);
            return;
        }
        var variabledef = _locals[variable];
        processor.Emit(OpCodes.Stloc, variabledef);
    }

}