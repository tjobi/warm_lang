using WarmLangCompiler.Binding;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Binding.BoundAccessing;
using WarmLangCompiler.Interpreter.Values;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser;

namespace WarmLangCompiler.Interpreter;

public sealed class BoundInterpreter
{
    private readonly BoundProgram program;
    private readonly FunctionSymbol _entryPoint;
    private VariableEnv _variableEnvironment;
    private FunctionEnv _functionEnvironment;

    public BoundInterpreter(BoundProgram program)
    {
        this.program = program;
        if(program.MainFunc is not null)
            _entryPoint = program.MainFunc;
        else if(program.ScriptMain is not null)
            _entryPoint = program.ScriptMain;
        else 
            throw new Exception($"Received a program where both '{nameof(program.MainFunc)}' and '{nameof(program.ScriptMain)}' are missing");
        
        _functionEnvironment = new(program.Functions);
        _variableEnvironment = new();
        foreach(var globalVar in program.GlobalVariables)
        {
            EvaluateVarDeclaration(globalVar);
        }
    }

    public static Value Run(BoundProgram program)
    {
        var runner = new BoundInterpreter(program);
        return runner.Run();
    }

    public Value Run() => EvaluateStatement(_functionEnvironment.Lookup(_entryPoint)!);

    private Value EvaluateStatement(BoundStatement statement)
    {
        return statement switch 
        {
            BoundExprStatement expr => EvaluateExpression(expr.Expression),
            BoundBlockStatement block => EvaluateBlock(block),
            BoundVarDeclaration varDecl => EvaluateVarDeclaration(varDecl),
            BoundFunctionDeclaration decl when decl.Symbol is LocalFunctionSymbol func => EvaluateFunctionDeclaration(func),
            BoundFunctionDeclaration => Value.Void, //Global function, is already declared from binder
            _ => throw new NotImplementedException($"Interpreter doesn't know {statement.GetType().Name} yet!"),
        };
    }

    private Value EvaluateBlock(BoundBlockStatement block)
    {
        Dictionary<BoundLabel, int> labelIndex = new();
        for(int i = 0; i < block.Statements.Length; i++)
        {
            var stmnt = block.Statements[i];
            if(stmnt is BoundLabelStatement labelStmnt)
            {
                labelIndex.Add(labelStmnt.Label, i);
            }
        }

        PushEnvironments();
        Value res = Value.Void;
        bool returned = false;
        for (int i = 0; i < block.Statements.Length && !returned; i++)
        {
            var stmnt = block.Statements[i];
            switch (stmnt)
            {
                case BoundLabelStatement: continue;
                case BoundGotoStatement gotoo:
                {
                    var index = labelIndex[gotoo.Label];
                    i = index;
                } break;
                case BoundConditionalGotoStatement jmp:
                {
                    var condition = EvaluateExpression(jmp.Condition) as BoolValue;
                    i = labelIndex[condition! ? jmp.LabelTrue : jmp.LabelFalse];
                } break;
                case BoundReturnStatement ret:
                {
                    if(ret.Expression is null)
                        res = Value.Void;
                    else
                        res = EvaluateExpression(ret.Expression);
                    returned = true;
                    break;
                }
                default:
                {
                    res = EvaluateStatement(stmnt);
                } break;
            }
        }
        PopEnvironments();
        return res;
    }

    private Value EvaluateVarDeclaration(BoundVarDeclaration varDecl)
    {
        var initializer = EvaluateExpression(varDecl.RightHandSide);
        _variableEnvironment.Declare(varDecl.Symbol, initializer);
        return initializer;
    }

    private Value EvaluateFunctionDeclaration(LocalFunctionSymbol local)
    {
        _functionEnvironment.Declare(local, local.Body!);
        return Value.Void;
    }


    private Value EvaluateExpression(BoundExpression expr)
    {
        return expr switch
        {
            BoundTypeConversionExpression conv => EvaluateTypeConversionExpression(conv),
            BoundUnaryExpression unaOp => EvaluateUnaryExpression(unaOp),
            BoundBinaryExpression binOp => EvaluateBinaryExpression(binOp),
            BoundCallExpression call => EvaluateCallExpression(call),
            BoundAssignmentExpression assign => EvaluateAssignmentExpression(assign),
            BoundAccessExpression acc => EvaluateAccessExpression(acc),
            BoundConstantExpression konst => EvaluateConstantExpression(konst),
            BoundListExpression lst => EvaluateListExpression(lst),
            BoundStructInitExpression bse => EvaluateStructInitExpression(bse),
            _ => throw new NotImplementedException($"{nameof(BoundInterpreter)} doesn't know '{expr.GetType().Name}' yet"),
        };
    }

    private Value EvaluateTypeConversionExpression(BoundTypeConversionExpression conv)
    {
        var value = EvaluateExpression(conv.Expression);
        if(conv.Type == TypeSymbol.Bool)
            if(value is IntValue i)
                return BoolValue.FromBool(i != 0);
        if(conv.Type == TypeSymbol.Int)
            if(value is BoolValue bv)
                return new IntValue(bv ? 1 : 0);
            if(value is StrValue str)
                return int.TryParse(str, out int res) ? new IntValue(res) : new ErrValue($"Couldn't convert {str} to an int");
        if(conv.Type == TypeSymbol.String)
        {
            return new StrValue(value.StdWriteString());
        }
        //Convert [] when used in variable declaration
        if(conv.Type is ListTypeSymbol)
            return value;
        throw new Exception($"{nameof(BoundInterpreter)} doesn't know conversion from '{value}' to '{conv.Type}'");
    }

    private Value EvaluateUnaryExpression(BoundUnaryExpression unary)
    {
        var exprValue = EvaluateExpression(unary.Left);
        var operatorAsString = unary.Operator.Operator.AsString();
        return (operatorAsString, exprValue) switch 
        {
            ("+", IntValue i) => i,  //do nothing for the (+1) cases
            ("-", IntValue i) => new IntValue(-i), //flip it for the (-1) cases
            ("<-", ListValue a) => a.RemoveLast(),
            ("!", BoolValue b) => b.Negate(),
            _ => throw new NotImplementedException($"Unary {operatorAsString} is not defined on {exprValue.GetType()}")
        };
    }

    private Value EvaluateBinaryExpression(BoundBinaryExpression binOp)
    {
        var op = binOp.Operator;    
        var left = EvaluateExpression(binOp.Left);
        
        //Early returns of '&&' '||', right should could be an a assignment :O
        if(op.Kind == BoundBinaryOperatorKind.LogicAND && left is BoolValue bv && bv == false)
            return BoolValue.False;
        if(op.Kind == BoundBinaryOperatorKind.LogicaOR && left is BoolValue bvv && bvv == true)
            return BoolValue.True;
        
        var right = EvaluateExpression(binOp.Right);

        Value res = (op.OpTokenKind.AsString(),left,right) switch 
        {
            ("+", IntValue i1, IntValue i2) => i1 + i2,
            ("-", IntValue i1, IntValue i2) => i1 - i2,
            ("*", IntValue i1, IntValue i2) => i1 * i2,
            ("/", IntValue i1, IntValue i2) => i1 / i2,
            ("**", IntValue i1, IntValue i2) => new IntValue((int)Math.Pow(i1,i2)),
            ("==", ListValue a, ListValue b) => BoolValue.FromBool(a.IsEqualTo(b)),
            ("!=", ListValue a, ListValue b) => BoolValue.FromBool(!a.IsEqualTo(b)),
            ("==", _,_) => BoolValue.FromBool(left == right),
            ("!=", _,_) => BoolValue.FromBool(left != right),
            ("<", IntValue i1, IntValue i2) => BoolValue.FromBool(i1 < i2), 
            ("<=", IntValue i1, IntValue i2) =>  BoolValue.FromBool(i1 <= i2),
            (">", IntValue i1, IntValue i2) => BoolValue.FromBool(i1 > i2), 
            (">=", IntValue i1, IntValue i2) =>  BoolValue.FromBool(i1 >= i2),
            ("::", ListValue arr,_) => arr.Add(right),
            ("+", ListValue a1, ListValue a2) => a1 + a2,
            ("+", StrValue str1, StrValue str2) => str1 + str2,
            ("&&", BoolValue b1, BoolValue b2) => BoolValue.FromBool(b1 && b2),
            ("||", BoolValue b1, BoolValue b2) => BoolValue.FromBool(b1 || b2),
            _ => throw new NotImplementedException($"{nameof(BoundInterpreter)} - Operator: '{op.OpTokenKind.AsString()}' on {left.GetType().Name} and {right.GetType().Name} is not defined")
        };

        return res;
    }

    private Value EvaluateCallExpression(BoundCallExpression call)
    {
        var function = call.Function;
        if(function.IsBuiltInFunction())
            return EvaluateCallBuiltinExpression(call);
        var functionBody = (function.IsMemberFunc 
                                ? program.TypeMemberInformation.FunctionBodies[function.OwnerType!][function]
                                : _functionEnvironment.Lookup(function))
                                ?? throw new Exception($"{nameof(BoundInterpreter)} couldn't find function to call '{function}'");
        
        var callArgs = call.Arguments;
        var funcParams = function.Parameters;
        PushEnvironments();
        for (int i = 0; i < call.Arguments.Length; i++)
        {
            //var paramType = funcParams[i].Type; //We have checked this type in the binder, should be all good!
            var paramName = funcParams[i];
            var paramValue = EvaluateExpression(callArgs[i]);
            _variableEnvironment.Declare(paramName, paramValue);
        }
        Value res = EvaluateStatement(functionBody);
        PopEnvironments();
        return res;
    }

    private Value EvaluateCallBuiltinExpression(BoundCallExpression call)
    {
        var function = call.Function;
        if(function == BuiltInFunctions.StdWriteLine || function == BuiltInFunctions.StdWrite)
        {
            var toPrint = EvaluateExpression(call.Arguments[0]).StdWriteString();
            if(function == BuiltInFunctions.StdWriteLine)
                Console.WriteLine(toPrint);
            else
                Console.Write(toPrint);
            return Value.Void;
        }
        if(function == BuiltInFunctions.StdWriteC)
        {
            var evalRes = (IntValue) EvaluateExpression(call.Arguments[0]);
            Console.WriteLine((char)evalRes.Value);
            return Value.Void;
        }
        if(function == BuiltInFunctions.StdClear)
        {
            Console.Clear();
            return Value.Void;
        }
        if(function == BuiltInFunctions.StdRead)
        {
            return new StrValue(Console.ReadLine() ?? "");
        }
        if(function == BuiltInFunctions.StrLen)
        {
            var evalRes = (StrValue) EvaluateExpression(call.Arguments[0]);
            return new IntValue(evalRes.Value.Length);
        }
        throw new NotImplementedException($"{nameof(BoundInterpreter)} doesn't know builtin {function}");
    }

    private Value EvaluateAssignmentExpression(BoundAssignmentExpression assign)
    {
        Value res;
        switch(assign.Access)
        {
            case BoundNameAccess nameAccess:
            {
                res = EvaluateExpression(assign.RightHandSide);
                _variableEnvironment.Assign(nameAccess.Symbol, res);
            } break;
            case BoundSubscriptAccess sa:
            {
                BoundAccess accessTarget = sa.Target;
                for(; accessTarget is BoundSubscriptAccess saa; accessTarget = saa.Target);
                var target = GetValueFromAccess(accessTarget);
                var index = EvaluateExpression(sa.Index);
                res = EvaluateExpression(assign.RightHandSide);

                if(target is ListValue lst && index is IntValue idx)
                {
                    if(idx < lst.Elements.Count && idx >= 0)
                    {
                        lst[idx] = res;
                        break;
                    } else 
                        throw new Exception("Index was out of range. Must be non-negative and less than size of collection");
                }
                throw new Exception($"Cannot subscript into '{sa.Target.Type}' using value of type '{sa.Index.Type}'");
            } 
            case BoundMemberAccess bma:
            {
                var target = GetValueFromAccess(bma.Target);
                
                if(bma.Member.IsReadOnly) 
                    throw new Exception($"{nameof(EvaluateAssignmentExpression)} tried to assign into a readonly member '{bma.Target.Type}.{bma.Member}'");
                if(target is not StructValue structVal) 
                    throw new NotImplementedException($"{nameof(BoundInterpreter)}.{nameof(EvaluateAssignmentExpression)} doesn't know assignment of '{bma.Target.Type}.{bma.Member}'");
                
                res = structVal[bma.Member.Name] = EvaluateExpression(assign.RightHandSide);
            } break;
            default:
                throw new NotImplementedException($"Assignment into access of type '{assign.Access.GetType().Name}' is not known");
        }
        return res;
    }

    private Value EvaluateAccessExpression(BoundAccessExpression acc)
    {
        switch(acc.Access)
        {
            case BoundNameAccess nameAccess:
            {
                return _variableEnvironment.Lookup(nameAccess.Symbol);
            }
            case BoundExprAccess ae: 
            {
                return EvaluateExpression(ae.Expression);
            }
            case BoundMemberAccess bma:
            {
                return GetValueFromAccess(acc.Access);
            }
            case BoundSubscriptAccess sa:
                {
                    BoundAccess accessTarget = sa.Target;
                    for (; accessTarget is BoundSubscriptAccess saa; accessTarget = saa.Target) ;
                    Value target = GetValueFromAccess(accessTarget);
                    IntValue idx = EvaluateExpression(sa.Index) as IntValue ?? throw new Exception($"{nameof(BoundInterpreter)} cannot use '{sa.Index.Type}' as subscript");
                    if (target is ListValue lst)
                    {
                        if (idx >= lst.Length || idx < 0)
                            throw new Exception($"Index was out of range. Must be non-negative and less than size of collection");

                        if (idx < lst.Length && idx >= 0)
                            return lst[idx];
                    }
                    else if (target is StrValue str)
                    {
                        if (idx >= str.Value.Length || idx < 0)
                            throw new Exception($"Index was out of range. Must be non-negative and less than size of collection");

                        if (idx < str.Value.Length && idx >= 0)
                            return new IntValue(str.Value[idx]);
                    }
                    //nothing good came out of this!
                    throw new Exception($"Cannot subscript into '{sa.Target.Type}' using value of type '{sa.Index.Type}'");
                }
        }
        throw new NotImplementedException($"Access of type '{acc.Access.GetType().Name}' is not known");
    }

    private Value GetValueFromAccess(BoundAccess accessTarget)
    {
        switch(accessTarget)
        {
            case BoundNameAccess name:
                return _variableEnvironment.Lookup(name.Symbol);
            case BoundExprAccess ea: 
                return EvaluateExpression(ea.Expression);
            case BoundMemberAccess bma when bma is {Member: MemberFieldSymbol symbol}:
                var valTarget = GetValueFromAccess(bma.Target);
                if(symbol.IsBuiltin)
                {
                    //TODO: This should look for the field we are accessing not just assume it's the length!
                    return valTarget switch
                    {
                        StrValue str => new IntValue(str.Value.Length),
                        ListValue lst => new IntValue(lst.Length),
                        _ => throw new NotImplementedException($"{nameof(BoundInterpreter)}.{nameof(GetValueFromAccess)} doesn't know '{bma.Target.Type}.{bma.Member}'")
                    };
                }
                if(valTarget is not StructValue sTarget) throw new Exception($"{nameof(BoundInterpreter)}.{nameof(GetValueFromAccess)} Assumption that 'symbol.IsBuiltin' is adequate is wrong - time to fix TODO");
                if(!sTarget.TryGetField(bma.Member.Name, out var fieldValue))
                    throw new Exception($"{nameof(BoundInterpreter)} couldn't find '{bma.Member}' on {bma.Target.Type}");
                return fieldValue;
            default:
                throw new Exception($"{nameof(BoundInterpreter)}.{nameof(GetValueFromAccess)} doesn't know how to handle {accessTarget}");
        }
    }

    private Value EvaluateConstantExpression(BoundConstantExpression konst)
    {
        return konst.Constant.Value switch {
            int i    => new IntValue(i),
            bool boo => BoolValue.FromBool(boo),
            string s => new StrValue(s),
            _ => throw new NotImplementedException($"{nameof(BoundInterpreter)} doesn't know value of type '{konst.Constant.Value.GetType().Name}' yet"),
        };
    }

    private Value EvaluateListExpression(BoundListExpression initializer)
    {
        var values = new ListValue(initializer.Expressions.Length);
        foreach(var expr in initializer.Expressions)
        {
            var evaluatedResult = EvaluateExpression(expr);
            values.Add(evaluatedResult);
        }
        return values;
    }

    private Value EvaluateStructInitExpression(BoundStructInitExpression bse)
    {
        var membersOfStruct = program.TypeMemberInformation.Members[bse.Type];
        var strct = new StructValue(bse.Type);
        foreach(var initedMember in bse.InitializedMembers)
        {
            var fieldName = initedMember.MemberSymbol.Name; 
            strct[fieldName] = EvaluateExpression(initedMember.Rhs);
        }
        foreach(var m in membersOfStruct)
        {
            if(m is MemberFuncSymbol) continue;
            if(strct.IsFieldInitialized(m.Name)) continue;
            strct[m.Name] = GetDefault(m.Type);
        }
        return strct;
    }

    private Value GetDefault(TypeSymbol symbol)
    {
        if(symbol == TypeSymbol.Bool) return BoolValue.DEFAULT;
        if(symbol == TypeSymbol.Int) return IntValue.DEFAULT;
        if(symbol == TypeSymbol.Void) return Value.Void;
        if(symbol is ListTypeSymbol) return ListValue.GET_DEFAULT();

        //TODO: Some better way of telling - right now we assume we're doing a struct type
        var strct = new StructValue(symbol);
        foreach(var member in program.TypeMemberInformation.Members[symbol])
        {
            strct.AddField(member.Name, GetDefault(member.Type));
        }
        return strct;
    }

    private void PushEnvironments()
    {
        _variableEnvironment.Push();
        _functionEnvironment.Push();
    }

    private void PopEnvironments()
    {
        _variableEnvironment.Pop();
        _functionEnvironment.Pop();
    }
}