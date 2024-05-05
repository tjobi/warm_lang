using WarmLangLexerParser;
using WarmLangLexerParser.AST;

//Console.WriteLine("Something something compiler: ");

var program = "test1.test";
var lexer = new Lexer();
var tokens = lexer.Lex(program);

// foreach(var token in tokens)
// {
//     Console.WriteLine(token);
// }

var parser = new Parser(tokens);
ASTNode root = parser.Parse();
Console.WriteLine($"Parsed:\n\t{root}");

var env = new Dictionary<string,int>();
Console.WriteLine($"Evaluating {program} -> {Evaluate(root, env)}");


static int Evaluate(ASTNode node, Dictionary<string,int> env)
{
    switch(node)
    {
        case ConstExpression c: {
            return c.Value;
        }
        case BinaryExpressionNode cur: {
            var left = Evaluate(cur.Left, env);
            var right = Evaluate(cur.Right, env);
            switch(cur.Operation)
            {
                case "+": {
                    return left + right;
                }
                case "*": {
                    return left * right;
                }
                default: {
                    throw new NotImplementedException($"Operation {cur.Operation} is not yet defined");
                }
            }
        
        }
        case VarBindingExpression binding: {
            var name = binding.Name;
            var value = Evaluate(binding.RightHandSide, env);
            env[name] = value;
            return value;
        }
        case VarExpression var: {
            return env[var.Name];
        }
        case BlockStatement block: {
            var expressions = block.Children;
            for (int i = 0; i < expressions.Count-1; i++)
            {
                var expr = expressions[i];
                Evaluate(expr, env);
            }
            var last = expressions[expressions.Count-1];
            return Evaluate(last, env);
        }
        default: {
            throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
        }
    }
}