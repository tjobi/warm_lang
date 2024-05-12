using WarmLangCompiler;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;

var program = "SyntaxTest/Minus.test";
var lexerDebug = true;
foreach (var arg in args)
{
    switch(arg)
    {
        case "--lex-mute":
        {
            lexerDebug = false;
        } break;
        default: {
            if(!File.Exists(arg))
            {
                Console.WriteLine("Failed: Given an non-existing filepath");
                return 2;
            }
            program = arg;
        }break;
    }
}

var lexer = new Lexer(program);
var tokens = lexer.Lex();
if(lexerDebug)
{
    foreach(var token in tokens)
    {
        Console.WriteLine(token);
    }
}

var parser = new Parser(tokens);
ASTNode root = parser.Parse();

Console.WriteLine($"Parsed:\n\t{root}");

var res = WarmLangInterpreter.Run(root);
Console.WriteLine($"Evaluated '{program}' -> {res}");


return 0;