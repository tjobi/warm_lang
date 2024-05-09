using System.Collections.Immutable;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;

var program = "test.test";
if (args.Length > 0)
{
    program = args[0];
}

var lexer = new Lexer();
var tokens = lexer.Lex(program);

var parser = new Parser(tokens);
ASTNode root = parser.Parse();

Console.WriteLine($"Parsed:\n\t{root}");

var env = ImmutableDictionary.Create<string,int>();
var (returned, _) = Evaluate(root, env);
Console.WriteLine($"Evaluating {program} -> {returned}");


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
            for (int i = 0; i < expressions.Count-1; i++)
            {
                var expr = expressions[i];
                var (val, nenv) = Evaluate(expr, env);
                env = nenv;
            }
            var last = expressions[^1];
            return Evaluate(last, env);
        }
        default: {
            throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
        }
    }
}