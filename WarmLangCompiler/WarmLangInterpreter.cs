namespace WarmLangCompiler;
using System.Collections.Immutable;
using WarmLangLexerParser.AST;

public static class WarmLangInterpreter
{
    public static int Run(ASTNode root)
    {
        var env = ImmutableDictionary.Create<string,int>();
        try {
            var (returned, _) = Evaluate(root, env);
            return returned;
        } catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return -1;
        }
    }

    public static (int, ImmutableDictionary<string,int>) Evaluate(ASTNode node, ImmutableDictionary<string,int> env)
    {
        switch(node)
        {
            case ConstExpression c: {
                return (c.Value, env);
            }
            case VarExpression var: {
                var hasFailed = !env.TryGetValue(var.Name, out var value);
                if(hasFailed)
                {
                    throw new Exception($"Failed: Variable {var.Name} has not been declared.");
                }
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
            case VarDeclarationExpression binding: {
                var name = binding.Name;
                var (value, eEnv) = Evaluate(binding.RightHandSide, env);
                var nextEnv = eEnv.Add(name, value);
                return (value, nextEnv);
            }
            case VarAssignmentExpression assignment: {
                var name = assignment.Name;
                var (value, eEnv) = Evaluate(assignment.RightHandSide, env);
                return (value, eEnv.SetItem(name, value)); 
                //Use eEnv because in the future we may want to allow something like
                // var x = 10; var y = 5; x = y++;
                // which would update both x and y.
            }
            case ExprStatement expr: {
                return Evaluate(expr.Expression, env);
            }
            case BlockStatement block: {
                var expressions = block.Children;
                var nenv = env;
                for (int i = 0; i < expressions.Count-1; i++)
                {
                    var expr = expressions[i];
                    var (_, nenv2) = Evaluate(expr, nenv);
                    nenv = nenv2;
                }
                var last = expressions[^1];
                
                var (lastValue, fEnv) = Evaluate(last, nenv);
                return (lastValue, fEnv); //Return an unaltered environment - to throw away any variables declared in block.
            }
            case IfStatement ifstmnt: {
                var (condValue, cEnv) = Evaluate(ifstmnt.Condition, env);
                var (value, nEnv) = Evaluate(
                    condValue != 0 ? ifstmnt.Then : ifstmnt.Else,
                    cEnv 
                );
                return (value, nEnv);
            }
            default: {
                throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
            }
        }
    }
}