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
ExpressionNode root = parser.Parse();
Console.WriteLine($"Parsed:\n\t{root}");
Console.WriteLine($"Evaluating {program} -> {Evaluate(root)}");

static int Evaluate(ExpressionNode node)
{
    switch(node)
    {
        case ConstExpression c: {
            return c.Value;
        }
        case BinaryExpressionNode cur: {
            var left = Evaluate(cur.Left);
            var right = Evaluate(cur.Right);
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
        default: {
            throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
        }
    }
}