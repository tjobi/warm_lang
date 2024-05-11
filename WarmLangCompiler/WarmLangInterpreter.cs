namespace WarmLangCompiler;
using System.Collections.Immutable;
using WarmLangLexerParser.AST;


using VarEnv = List<System.Collections.Immutable.ImmutableDictionary<string,int>>;
using Funct = Tuple<System.Collections.Immutable.ImmutableList<string>, WarmLangLexerParser.AST.StatementNode>;
using FuncEnv = List<System.Collections.Immutable.ImmutableDictionary<string, Tuple<System.Collections.Immutable.ImmutableList<string>, WarmLangLexerParser.AST.StatementNode>>>;

public static class WarmLangInterpreter
{
    public static int Run(ASTNode root)
    {
        var env = new VarEnv()
        {
            ImmutableDictionary.Create<string,int>()
        };
        var fenv = new FuncEnv()
        {
            ImmutableDictionary<string, Funct>.Empty
        };
        try {
            var (returned, _,_) = Evaluate(root, env, fenv);
            return returned;
        } catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return -1;
        }
    }

    private static FuncEnv PushFuncScope(FuncEnv env)
    {
        env.Add(ImmutableDictionary<string, Funct>.Empty);
        return env;
    }

    private static FuncEnv PopFuncScope(FuncEnv env)
    {
        env.RemoveAt(env.Count - 1);
        return env;
    }

    private static Funct LookupFunc(FuncEnv env, string funcName)
    {
        for (int i = env.Count - 1; i >= 0 ; i--)
        {
            var scope = env[i];
            if(scope.TryGetValue(funcName, out var res))
            {
                return res;
            }
        }
        throw new Exception($"Failed: Variable {funcName} has not been declared.");
    }

    private static (Funct, FuncEnv) DeclareFunc(FuncEnv env, string funcName, IList<string> paramNames, StatementNode body)
    {
        var latestFuncScope = env[^1];
        try
        {
            var funcTuple = (paramNames.ToImmutableList(), body).ToTuple();
            var newEnv = latestFuncScope.Add(funcName, funcTuple);
            env[^1] = newEnv;
            return (funcTuple, env);
        } catch
        {
            throw new Exception("Failed: Function is already defined");
        }

    }

    private static VarEnv PushScope(VarEnv env)
    {
        env.Add(ImmutableDictionary<string, int>.Empty);
        return env;
    }

    private static VarEnv PopScope(VarEnv env )
    {
        env.RemoveAt(env.Count-1);
        return env;
    } 

    private static int Lookup(VarEnv env, string name)
    {
        for (int i = env.Count - 1; i >= 0 ; i--)
        {
            var scope = env[i];
            if(scope.TryGetValue(name, out var res))
            {
                return res;
            }
        }
        throw new Exception($"Failed: Variable {name} has not been declared.");
    }

    private static (int, VarEnv) AssignVar(VarEnv env, string name, int value)
    {
        for (int i = env.Count - 1; i >= 0 ; i--)
        {
            var scope = env[i];
            if(scope.ContainsKey(name))
            {
                env[i] = scope.SetItem(name, value);
                return (value, env);
            }
        }
        throw new Exception($"Failed: {name} does not exist");
    }

    private static (int, VarEnv) DeclareVar(VarEnv env, string name, int value)
    {

        var mostRecentScope = env.Last();
        try
        {
            var newEnv = mostRecentScope.Add(name, value);
            env[^1] = newEnv;
            return (value, env);
        } catch
        {
            throw new Exception("Failed: Variable is already defined");
        }
    }

    public static (int, VarEnv, FuncEnv) Evaluate(ASTNode node, VarEnv env, FuncEnv fenv)
    {
        switch(node)
        {
            case ConstExpression c: {
                return (c.Value, env, fenv);
            }
            case VarExpression var: {
                var value = Lookup(env, var.Name);
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
                var (_, nextEnv) = DeclareVar(eEnv, name, value);
                return (value, nextEnv, fenv);
            }
            case VarAssignmentExpression assignment: {
                var name = assignment.Name;
                var (value, eEnv,_) = Evaluate(assignment.RightHandSide, env, fenv);
                var (res, nEnv) = AssignVar(eEnv, name, value);
                return (res, nEnv, fenv);
                //Use eEnv because in the future we may want to allow something like
                // var x = 10; var y = 5; x = y++;
                // which would update both x and y.
            }
            case CallExpression call: {
                var name = call.Name;
                var callArgs = call.Arguments;
                var (paramNames, funcBody) = LookupFunc(fenv, name);
                var callVarScope = PushScope(env); 
                var callFunScope = PushFuncScope(fenv);
                foreach(var (paramName, expr) in paramNames.Zip(callArgs))
                {
                    var (value, nEnv, nFEnv) = Evaluate(expr, callVarScope, callFunScope);
                    var (_, nVarEnv) = DeclareVar(nEnv, paramName, value);
                    callVarScope = nVarEnv;
                    callFunScope = nFEnv; //not too sure about this one :)
                }
                var (returnedValue, retVarEnv, retFuncEnv) = Evaluate(funcBody,callVarScope, callFunScope);
                return (returnedValue, PopScope(retVarEnv), PopFuncScope(retFuncEnv));
            }
            case FuncDeclaration funDecl: {
                var funcName = funDecl.Name;
                var paramNames = funDecl.Params;
                var body = funDecl.Body;
                var (fun, newFEnv) = DeclareFunc(fenv, funcName, paramNames, body);
                return (0, env, newFEnv);
            }
            case ExprStatement expr: {
                return Evaluate(expr.Expression, env, fenv);
            }
            case BlockStatement block: {
                var expressions = block.Children;
                var nenv = PushScope(env);
                var nFuncEnv = PushFuncScope(fenv);
                for (int i = 0; i < expressions.Count-1; i++)
                {
                    var expr = expressions[i];
                    var (_, nenv2, fenv2) = Evaluate(expr, nenv, fenv);
                    nenv = nenv2;
                    nFuncEnv = fenv2;
                }
                var last = expressions[^1];
                
                var (lastValue, lastEnv, lastFuncEnv) = Evaluate(last, nenv, nFuncEnv);
                return (lastValue, PopScope(lastEnv), PopFuncScope(lastFuncEnv)); //Return an unaltered environment - to throw away any variables declared in block.
            }
            case IfStatement ifstmnt: {
                var ifScope = PushScope(env);
                var ifFuncScope = PushFuncScope(fenv);
                var (condValue, cEnv, cFuncEnv) = Evaluate(ifstmnt.Condition, ifScope, ifFuncScope);
                var doThenBranch = condValue != 0;
                if(!doThenBranch && ifstmnt.Else is null)
                {
                    //What to return in this case?
                    return (0, PopScope(cEnv), PopFuncScope(cFuncEnv));
                }
                var (value, nEnv, nFuncEnv) = Evaluate(doThenBranch ? ifstmnt.Then : ifstmnt.Else!, cEnv, fenv);
                return (value, PopScope(nEnv), PopFuncScope(nFuncEnv));
            }
            default: {
                throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
            }
        }
    }
}