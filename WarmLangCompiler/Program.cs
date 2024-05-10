using System.Collections.Immutable;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;

var program = "test1.test";
if (args.Length > 0)
{
    program = args[0];
    if (!File.Exists(program))
    {
        Console.WriteLine("Failed: Given an non-existing filepath");
        return 2;
    }
}

var lexer = new Lexer(program);
var tokens = lexer.Lex();
foreach(var token in tokens)
{
    Console.WriteLine(token);
}

var parser = new Parser(tokens);
ASTNode root = parser.Parse();

Console.WriteLine($"Parsed:\n\t{root}");

var env = ImmutableDictionary.Create<string,int>();
try {
    var (returned, _) = Evaluate(root, env);
    Console.WriteLine($"Evaluating {program} -> {returned}");
} catch (Exception e)
{
    Console.WriteLine(e.Message);
    return 1;
}


static (int, ImmutableDictionary<string,int>) Evaluate(ASTNode node, ImmutableDictionary<string,int> env)
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
                throw new Exception($"Failed: Variable {var.Name} is out of scope.");
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
            var (lastValue, _) = Evaluate(last, nenv);
            return (lastValue, env); //Return an unaltered environment - to throw away any variables declared in block.
        }
        default: {
            throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
        }
    }
}

return 0;