using WarmLangCompiler.Binding;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Interpreter.Values;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser;

namespace WarmLangCompiler.Interpreter;

public sealed class BoundInterpreter
{
    private readonly BoundProgram program;
    private VariableEnv _variableEnvironment;
    private FunctionEnv _functionEnvironment;

    public BoundInterpreter(BoundProgram program)
    {
        this.program = program;
        _variableEnvironment = new();
        _functionEnvironment = new(program.Functions);
    }

    public static Value Run(BoundProgram program)
    {
        var runner = new BoundInterpreter(program);
        return runner.Run();
    }

    public Value Run() => EvaluateStatement(program.Statement);

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
        _variableEnvironment.Declare(varDecl.Symbol.Name, initializer);
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
            ("<-", ListValue a) => a.RemoveLast(),  //TODO: Should it return the array or the value removed?
            ("!", BoolValue b) => b.Negate(),
            _ => throw new NotImplementedException($"Unary {operatorAsString} is not defined on {exprValue.GetType()}")
        };
    }

    private Value EvaluateBinaryExpression(BoundBinaryExpression binOp)
    {
        var left = EvaluateExpression(binOp.Left);
        var right = EvaluateExpression(binOp.Right);

        var op = binOp.Operator;
        Value res = (op.Kind.AsString(),left,right) switch 
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
            _ => throw new NotImplementedException($"{nameof(BoundInterpreter)} - Operator: '{op.Kind.AsString()}' on {left.GetType().Name} and {right.GetType().Name} is not defined")
        };

        return res;
    }

    private Value EvaluateCallExpression(BoundCallExpression call)
    {
        var function = call.Function;
        if(function.IsBuiltInFunction())
            return EvaluateCallBuiltinExpression(call);
        var functionBody = _functionEnvironment.Lookup(function);
        var callArgs = call.Arguments;
        var funcParams = function.Parameters;
        PushEnvironments();
        for (int i = 0; i < call.Arguments.Length; i++)
        {
            //var paramType = funcParams[i].Type; //We have checked this type in the binder, should be all good!
            var paramName = funcParams[i].Name;
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
                _variableEnvironment.Assign(nameAccess.Symbol.Name, res);
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
                return _variableEnvironment.Lookup(nameAccess.Symbol.Name);
            }
            case BoundExprAccess ae: 
            {
                return EvaluateExpression(ae.Expression);
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
        return accessTarget switch
        {
            BoundNameAccess na => _variableEnvironment.Lookup(na.Symbol.Name),
            BoundExprAccess ea => EvaluateExpression(ea.Expression),
            _ => throw new Exception($"{nameof(BoundInterpreter)} access expression - your code didn't work bro"),
        };
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