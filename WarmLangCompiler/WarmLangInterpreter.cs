namespace WarmLangCompiler;

using WarmLangCompiler.Interpreter;
using WarmLangCompiler.Interpreter.Values;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.Typs;

public static class WarmLangInterpreter
{
    public static Value Run(ASTNode root)
    {
        var venv = new VarEnv();
        var fenv = new FuncEnv();
        try {
            var (returned, _,_) = Evaluate(root, venv, fenv);
            return returned;
        } catch (Exception e)
        {
            return new ErrValue(e.Message);
        }
    }

    public static (Value, IAssignableEnv<Value>, IEnv<Funct>) Evaluate(ASTNode node, IAssignableEnv<Value> env, IEnv<Funct> fenv)
    {
        switch(node)
        {
            case ConstExpression c: 
            {
                return (new IntValue(c.Value), env, fenv);
            }
            case AccessExpression var: 
            {
                var (value, newVarEnv) = Access(var.Access, env, fenv);
                return (value, newVarEnv, fenv);
            }
            case BinaryExpression cur: 
            {
                var (left, leftEnv, _) = Evaluate(cur.Left, env, fenv);
                var (right, resEnv, _) = Evaluate(cur.Right, leftEnv, fenv);

                var op = cur.Operation;
                Value res = (op,left,right) switch 
                {
                    ("+", IntValue i1, IntValue i2) => new IntValue(i1.Value + i2.Value),
                    ("*", IntValue i1, IntValue i2) => new IntValue(i1.Value * i2.Value),
                    ("-", IntValue i1, IntValue i2) => new IntValue(i1.Value - i2.Value),
                    //TODO: When we introduce booleans, change ones below
                    ("==", IntValue i1, IntValue i2) => new IntValue(i1.Value == i2.Value ? 1 : 0), 
                    ("<", IntValue i1, IntValue i2) => new IntValue(i1.Value < i2.Value ? 1 : 0), 
                    ("<=", IntValue i1, IntValue i2) =>  new IntValue(i1.Value <= i2.Value ? 1 : 0),
                    ("::", ListValue arr, IntValue i) => arr.Add(i),
                    ("+", ListValue a1, ListValue a2) => new ListValue(a1.Elements.Concat(a2.Elements).ToList()),
                    _ => throw new NotImplementedException($"Operator: \"{op}\" on {left.GetType().Name} and {right.GetType().Name} is not defined")
                };

                return (res, resEnv, fenv);
            }
            case UnaryExpression unary: 
            {
                var (exprValue, newVarEnv, newFuncEnv) = Evaluate(unary.Expression, env, fenv);
                var value = (unary.Operation, exprValue) switch 
                {
                    ("+", IntValue i) => i,  //do nothing for the (+1) cases
                    ("-", IntValue i) => new IntValue(-i.Value), //flip it for the (-1) cases
                    (":!", ListValue a) => a.RemoveLast(),  //TODO: Should it return the array or the value removed?
                    _ => throw new NotImplementedException($"Unary {unary.Operation} is not defined on {exprValue.GetType()}")
                };
                return (value, newVarEnv, newFuncEnv);
            }
            case VarDeclarationExpression decl: 
            {
                var name = decl.Name;
                var (value, eEnv,_) = Evaluate(decl.RightHandSide, env, fenv);
                var (_, nextEnv) = eEnv.Declare( name, value);
                return (value, (VarEnv)nextEnv, fenv);
            }
            case AssignmentExpression assignment: 
            {
                switch(assignment.Access)
                {
                    case NameAccess name:
                    {
                        var (value, eEnv,_) = Evaluate(assignment.RightHandSide, env, fenv);
                        var (_,newVarEnv) = eEnv.Assign(name.Name, value);
                        return (value, (VarEnv) newVarEnv, fenv);
                    }
                    case SubscriptAccess sa: 
                    {
                        var (target, _) = Access(sa.Target, env, fenv);
                        var (index, newVarEnv, _) = Evaluate(sa.Index, env, fenv);
                        if(target is ListValue arr && index is IntValue iv)
                        {
                            var idx = iv.Value;
                            if(idx < arr.Elements.Count && idx > 0)
                            {
                                var (value, newVarEnv2, _) = Evaluate(assignment.RightHandSide, newVarEnv, fenv);
                                arr[idx] = value;
                                return (value, newVarEnv2, fenv);
                            } else 
                            {
                                throw new Exception("Index was out of range. Must be non-negative and less than size of collection");
                            }
                        } else 
                        {
                            throw new NotImplementedException($"Subscripting not implemented for {target.GetType().Name}");
                        }
                    }
                    default:
                        throw new NotImplementedException($"No interpreter support for access {assignment.Access.GetType().Name}");
                }
            }
            case ListInitExpression arrInitter: 
            {
                var values = new List<Value>();
                foreach(var expr in arrInitter.Elements)
                {
                    var evaluatedResult = Evaluate(expr, env, fenv);
                    values.Add(evaluatedResult.Item1);
                    env  = evaluatedResult.Item2;
                    fenv = evaluatedResult.Item3;
                }
                var res = new ListValue(values);
                return (res, env, fenv);
            }
            case CallExpression call: 
            {
                var toCall = call.Called;
                var callArgs = call.Arguments;
                if(toCall is not AccessExpression && ((AccessExpression) toCall).Access is not NameAccess)
                {
                    throw new NotImplementedException("Interpreter doesn't allow arbitrary function calls");
                }
                var (functionParameters, funcBody) = fenv.Lookup(((NameAccess) ((AccessExpression)toCall).Access).Name);
                
                var callVarScope = env.Push();
                var callFunScope = fenv.Push();

                foreach(var ((paramType, paramName), expr) in functionParameters.Zip(callArgs)) 
                {
                    var (value, nEnv, nFEnv) = Evaluate(expr, (VarEnv)callVarScope, callFunScope);

                    //This is an attempt at prevention a function being called with arguments of wrong types.
                    //something like : "function func(int x) { x + 2; } 
                    //                  func(true)"  <-- true is not an int?!?!
                    var (_, nVarEnv) = (value, paramType) switch 
                    {
                        (IntValue _, TokenKind.TInt) => nEnv.Declare(paramName, value),
                        _ => throw new Exception($"Value of {value.GetType().Name} does not match function paramter type {paramType}")
                    };
                    
                    callVarScope = nVarEnv;
                    callFunScope = nFEnv; //not too sure about this one :)
                }
                var (returnedValue, retVarEnv, retFuncEnv) = Evaluate(funcBody,(VarEnv)callVarScope, callFunScope);
                return (returnedValue, (VarEnv)retVarEnv.Pop(), retFuncEnv.Pop());
            }
            case FuncDeclaration funDecl: 
            {
                var funcName = funDecl.Name;
                var paramNames = funDecl.Params
                                        .Select(p => (p.Item1.ToTokenKind(), p.Item2))
                                        .ToList();
                var body = funDecl.Body;
                var (function, newFEnv) = fenv.Declare(funcName, new Funct(paramNames, body));
                return (new IntValue(0), env, newFEnv);
            }
            case ExprStatement expr: 
            {
                return Evaluate(expr.Expression, env, fenv);
            }
            case BlockStatement block: 
            {
                var expressions = block.Children;
                var nenv = env.Push();
                var nFuncEnv = fenv.Push();
                for (int i = 0; i < expressions.Count-1; i++)
                {
                    var expr = expressions[i];
                    var (_, nenv2, fenv2) = Evaluate(expr, (VarEnv)nenv, fenv);
                    nenv = nenv2;
                    nFuncEnv = fenv2;
                }
                var last = expressions[^1];
                
                var (lastValue, lastEnv, lastFuncEnv) = Evaluate(last, (VarEnv)nenv, nFuncEnv);
                return (lastValue, (VarEnv)lastEnv.Pop(), lastFuncEnv.Pop()); //Return an unaltered environment - to throw away any variables declared in block.
            }
            case IfStatement ifstmnt: 
            {
                var ifScope = env.Push();
                var ifFuncScope = fenv.Push();
                var (condValue, cEnv, cFuncEnv) = Evaluate(ifstmnt.Condition, (VarEnv) ifScope, ifFuncScope);
                
                bool doThenBranch = condValue switch //TODO: When we introduce booleans, look at this again :)
                {
                    IntValue i => i.Value != 0,
                    _ => throw new NotImplementedException($"value of {condValue.GetType()} cannot be used as boolean")
                };

                if(!doThenBranch && ifstmnt.Else is null)
                {
                    //What to return in this case?
                    return (new IntValue(0), (VarEnv)cEnv.Pop(), cFuncEnv.Pop());
                }
                var (value, nEnv, nFuncEnv) = Evaluate(doThenBranch ? ifstmnt.Then : ifstmnt.Else!, cEnv, fenv);
                return (value, (VarEnv)nEnv.Pop(), nFuncEnv.Pop());
            }
            default: 
            {
                throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
            }
        }
    }

    public static (Value, IAssignableEnv<Value>) Access(Access acc, IAssignableEnv<Value> varEnv, IEnv<Funct> fenv)
    {
        switch(acc)
        {
            case NameAccess name:
            {
                return (varEnv.Lookup(name.Name), varEnv);
            }
            case SubscriptAccess sa:
            {
                var (target, _) = Access(sa.Target, varEnv, fenv);
                var (index, newVarEnv, _) = Evaluate(sa.Index, varEnv, fenv);
                switch(index)
                {
                    case IntValue iv:
                    {
                        var idx = iv.Value;
                        var res = target switch 
                        {
                            ListValue a when idx < a.Length && idx >= 0 => a.Elements[idx],
                            ListValue a when idx >= a.Length || idx < 0 
                                => throw new Exception($"Index was out of range. Must be non-negative and less than size of collection"),
                            _ => throw new Exception($"Cannot subscript into type {target.GetType().Name}")
                        };
                        return (res, newVarEnv);
                    }
                    default: 
                        throw new Exception($"Cannot subscript into '{sa.Target}' using {index.GetType().Name}");
                }
            }
            case ExprAccess ae: 
            {
                var (value, _,_) = Evaluate(ae.Expression, varEnv, fenv);
                return (value, varEnv);
            }
            default: 
                throw new NotImplementedException($"Access: {acc.GetType().Name} is not implemented");
        }
    }
}