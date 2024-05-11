namespace WarmLangCompiler;

using WarmLangCompiler.Interpreter;
using WarmLangLexerParser.AST;
public static class WarmLangInterpreter
{
    public static int Run(ASTNode root)
    {
        var venv = new VarEnv();
        var fenv = new FuncEnv();
        try {
            var (returned, _,_) = Evaluate(root, venv, fenv);
            return returned;
        } catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return -1;
        }
    }

    public static (int, IAssignableEnv<int>, IEnv<Funct>) Evaluate(ASTNode node, IAssignableEnv<int> env, IEnv<Funct> fenv)
    {
        switch(node)
        {
            case ConstExpression c: {
                return (c.Value, env, fenv);
            }
            case VarExpression var: {
                var value = env.Lookup(var.Name);
                return (value, env, fenv);
            }
            case BinaryExpressionNode cur: {
                var (left, leftEnv, _) = Evaluate(cur.Left, env, fenv);
                var (right, resEnv, _) = Evaluate(cur.Right, leftEnv, fenv);

                var res = cur.Operation switch {
                    "+" => left + right,
                    "*" => left * right,
                    _ => throw new NotImplementedException($"Failed: Operation {cur.Operation} is not yet defined")
                };

                return (res, resEnv, fenv);
            }
            case VarDeclarationExpression decl: {
                var name = decl.Name;
                var (value, eEnv,_) = Evaluate(decl.RightHandSide, env, fenv);
                var (_, nextEnv) = eEnv.Declare( name, value);
                return (value, (VarEnv)nextEnv, fenv);
            }
            case VarAssignmentExpression assignment: {
                var name = assignment.Name;
                var (value, eEnv,_) = Evaluate(assignment.RightHandSide, env, fenv);
                var (res, nEnv) = eEnv.Assign(name, value);
                return (res, (VarEnv)nEnv, fenv);
                //Use eEnv because in the future we may want to allow something like
                // var x = 10; var y = 5; x = y++;
                // which would update both x and y.
            }
            case CallExpression call: {
                var name = call.Name;
                var callArgs = call.Arguments;
                var (paramNames, funcBody) = fenv.Lookup(name);
                var callVarScope = env.Push();
                var callFunScope = fenv.Push();
                foreach(var (paramName, expr) in paramNames.Zip(callArgs))
                {
                    var (value, nEnv, nFEnv) = Evaluate(expr, (VarEnv)callVarScope, callFunScope);
                    var (_, nVarEnv) = nEnv.Declare(paramName, value);
                    callVarScope = nVarEnv;
                    callFunScope = nFEnv; //not too sure about this one :)
                }
                var (returnedValue, retVarEnv, retFuncEnv) = Evaluate(funcBody,(VarEnv)callVarScope, callFunScope);
                return (returnedValue, (VarEnv)retVarEnv.Pop(), retFuncEnv.Pop());
            }
            case FuncDeclaration funDecl: {
                var funcName = funDecl.Name;
                var paramNames = funDecl.Params;
                var body = funDecl.Body;
                var (function, newFEnv) = fenv.Declare(funcName, new Funct(paramNames, body));
                return (0, env, newFEnv);
            }
            case ExprStatement expr: {
                return Evaluate(expr.Expression, env, fenv);
            }
            case BlockStatement block: {
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
            case IfStatement ifstmnt: {
                var ifScope = env.Push();
                var ifFuncScope = fenv.Push();
                var (condValue, cEnv, cFuncEnv) = Evaluate(ifstmnt.Condition, (VarEnv) ifScope, ifFuncScope);
                var doThenBranch = condValue != 0;
                if(!doThenBranch && ifstmnt.Else is null)
                {
                    //What to return in this case?
                    return (0, (VarEnv)cEnv.Pop(), cFuncEnv.Pop());
                }
                var (value, nEnv, nFuncEnv) = Evaluate(doThenBranch ? ifstmnt.Then : ifstmnt.Else!, cEnv, fenv);
                return (value, (VarEnv)nEnv.Pop(), nFuncEnv.Pop());
            }
            default: {
                throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
            }
        }
    }
}