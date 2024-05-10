using WarmLangCompiler;
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

var res = WarmLangInterpreter.Run(root);
Console.WriteLine($"Evaluated '{program}' -> {res}");


return 0;