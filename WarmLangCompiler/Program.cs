using WarmLangCompiler;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.Exceptions;
using WarmLangLexerParser.Read;

var program = "SyntaxTest/Minus.test";
var lexerDebug = true;
var longExceptions = false;
foreach (var arg in args)
{
    switch(arg)
    {
        case "--lex-mute":
        {
            lexerDebug = false;
        } break;
        case "--trace":
        {
            longExceptions = true;
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

var lexer = new Lexer(new FileWindow(program));
var tokens = lexer.Lex();
if(lexerDebug)
{
    foreach(var token in tokens)
    {
        Console.WriteLine(token);
    }
}

try 
{
    var parser = new Parser(tokens);
    ASTNode root = parser.Parse();
    Console.WriteLine($"Parsed:\n\t{root}");

    var res = WarmLangInterpreter.Run(root);
    Console.WriteLine($"Evaluated '{program}' -> {res}");
} catch(ParserException e)
{
    Console.WriteLine(longExceptions ? e : e.Message);
    return -1;
}
catch (NotImplementedException e )
{
    Console.WriteLine(longExceptions ? e : e.Message);
    return 2;
}



return 0;