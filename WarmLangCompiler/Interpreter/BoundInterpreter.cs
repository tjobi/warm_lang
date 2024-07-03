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

    public Value Run()
    {
        return EvaluateStatement(program.Statement);
    }

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
        for (int i = 0; i < block.Statements.Length; i++)
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
                    var condtion = EvaluateExpression(jmp.Condition);
                    i = labelIndex[IsValueTrue(condtion) ? jmp.LabelTrue : jmp.LabelFalse];
                } break;
                case BoundReturnStatement ret:
                {
                    if(ret.Expression is null)
                        return Value.Void;
                    return EvaluateExpression(ret.Expression);
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
            _ => throw new NotImplementedException($"Interpreter doesn't know '{expr.GetType()} yet!'"),
        };
    }

    private Value EvaluateTypeConversionExpression(BoundTypeConversionExpression conv)
    {
        //TODO: Right now we only support [] to list<xxx> so, we do nothing here
        return EvaluateExpression(conv.Expression);
    }

    private Value EvaluateUnaryExpression(BoundUnaryExpression unary)
    {
        var exprValue = EvaluateExpression(unary.Left);
        var operatorAsString = unary.Operator.Operator.AsString();
        return (operatorAsString, exprValue) switch 
        {
            ("+", IntValue i) => i,  //do nothing for the (+1) cases
            ("-", IntValue i) => new IntValue(-i.Value), //flip it for the (-1) cases
            ("<-", ListValue a) => a.RemoveLast(),  //TODO: Should it return the array or the value removed?
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
            ("*", IntValue i1, IntValue i2) => i1 * i2,
            ("-", IntValue i1, IntValue i2) => i1 - i2,
            ("==", ListValue a, ListValue b) => BoolValue(a.IsEqualTo(b)),
            ("==", _,_) => BoolValue(left == right),
            ("<", IntValue i1, IntValue i2) => BoolValue(i1 < i2), 
            ("<=", IntValue i1, IntValue i2) =>  BoolValue(i1 <= i2),
            ("::", ListValue arr,_) => arr.Add(right),
            ("+", ListValue a1, ListValue a2) => a1 + a2,
            _ => throw new NotImplementedException($"Operator: \"{op}\" on {left.GetType().Name} and {right.GetType().Name} is not defined")
        };

        return res;
    }

    private Value EvaluateCallExpression(BoundCallExpression call)
    {
        var function = call.Function;
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
                throw new NotImplementedException($"Subscripting not implemented for {target.GetType().Name}");
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
                    Value index = EvaluateExpression(sa.Index);
                    if (index is IntValue idx && target is ListValue lst)
                    {
                        if (idx >= lst.Length || idx < 0)
                            throw new Exception($"Index was out of range. Must be non-negative and less than size of collection");

                        if (idx < lst.Length && idx >= 0)
                            return lst[idx];
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
            _ => throw new Exception($"BoundInterpreter access expression - your code didn't work bro"),
        };
    }

    private Value EvaluateConstantExpression(BoundConstantExpression konst)
    {
        return new IntValue(konst.Constant.Value);
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

     private static bool IsValueTrue(Value v)
    {
        if (v is IntValue i)
            return i != 0;
        throw new NotImplementedException($"value of {v.GetType()} cannot be used as boolean");
    }

    private static Value BoolValue(bool cond) => new IntValue(cond ? 1 : 0);

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