namespace WarmLangCompiler;
using System.Collections.Immutable;
using WarmLangLexerParser.AST;


using VarEnv = List<System.Collections.Immutable.ImmutableDictionary<string,int>>;

public static class WarmLangInterpreter
{
    public static int Run(ASTNode root)
    {
        var env = new VarEnv()
        {
            ImmutableDictionary.Create<string,int>()
        };
        try {
            var (returned, _) = Evaluate(root, env);
            return returned;
        } catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return -1;
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

    public static (int, VarEnv) Evaluate(ASTNode node, VarEnv env)
    {
        switch(node)
        {
            case ConstExpression c: {
                return (c.Value, env);
            }
            case VarExpression var: {
                var value = Lookup(env, var.Name);
                return (value, env);
            }
            case BinaryExpressionNode cur: {
                var (left, leftEnv) = Evaluate(cur.Left, env);
                var (right, resEnv) = Evaluate(cur.Right, leftEnv);

                var res = cur.Operation switch {
                    "+" => left + right,
                    "*" => left * right,
                    _ => throw new NotImplementedException($"Failed: Operation {cur.Operation} is not yet defined")
                };

                return (res, resEnv);
            }
            case VarDeclarationExpression decl: {
                var name = decl.Name;
                var (value, eEnv) = Evaluate(decl.RightHandSide, env);
                var (_, nextEnv) = DeclareVar(eEnv, name, value);
                return (value, nextEnv);
            }
            case VarAssignmentExpression assignment: {
                var name = assignment.Name;
                var (value, eEnv) = Evaluate(assignment.RightHandSide, env);
                return AssignVar(eEnv, name, value);
                //Use eEnv because in the future we may want to allow something like
                // var x = 10; var y = 5; x = y++;
                // which would update both x and y.
            }
            case ExprStatement expr: {
                return Evaluate(expr.Expression, env);
            }
            case BlockStatement block: {
                var expressions = block.Children;
                var nenv = PushScope(env);
                for (int i = 0; i < expressions.Count-1; i++)
                {
                    var expr = expressions[i];
                    var (_, nenv2) = Evaluate(expr, nenv);
                    nenv = nenv2;
                }
                var last = expressions[^1];
                
                var (lastValue, fEnv) = Evaluate(last, nenv);
                return (lastValue, PopScope(fEnv)); //Return an unaltered environment - to throw away any variables declared in block.
            }
            case IfStatement ifstmnt: {
                var ifScope = PushScope(env);
                var (condValue, cEnv) = Evaluate(ifstmnt.Condition, ifScope);
                var doThenBranch = condValue != 0;
                if(!doThenBranch && ifstmnt.Else is null)
                {
                    //What to return in this case?
                    return (0, cEnv);
                }
                var (value, nEnv) = Evaluate(doThenBranch ? ifstmnt.Then : ifstmnt.Else!, cEnv);
                return (value, PopScope(nEnv));
            }
            default: {
                throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
            }
        }
    }
}