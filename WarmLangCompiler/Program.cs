using WarmLangCompiler;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.Exceptions;

var DEFAULT_PROGRAM = "SyntaxTest/test.test";

var parsedArgs = ArgsParser.ParseArgs(args, DEFAULT_PROGRAM);
if(parsedArgs is null)
{
    return -1;
}
// To change the "default" value of the values below goto ArgsParser.ParseArgs :)
var (program, parserDebug, lexerDebug, longExceptions) = (ParsedArgs) parsedArgs; //cast is okay-ish, we just did a null check

try 
{
    var lexer = Lexer.FromFile(program);
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
    if(parserDebug)
    {
        Console.WriteLine($"Parsed:\n\t{root}");
    }

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