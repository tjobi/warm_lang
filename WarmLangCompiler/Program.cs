using WarmLangCompiler;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.ErrorReporting;
using WarmLangLexerParser.Exceptions;

var DEFAULT_PROGRAM = "SyntaxTest/test.test";

var parsedArgs = ArgsParser.ParseArgs(args, DEFAULT_PROGRAM);
if(parsedArgs is null)
{
    return -1;
}
// To change the "default" value of the values below goto ArgsParser.ParseArgs :)
var (program, parserDebug, lexerDebug, longExceptions, interactive) = (ParsedArgs) parsedArgs; //cast is okay-ish, we just did a null check

if(interactive)
{
    for(string? input = Console.ReadLine(); input is not null; input = Console.ReadLine())
    {
        if(input == "q;;")
        {
            break;
        }
        var diagnostics = new ErrorWarrningBag();
        var lexer = Lexer.FromString(input, diagnostics);
        var tokens = lexer.Lex();
        if(diagnostics.Any())
        {
            diagnostics.ToList().ForEach(Console.WriteLine);
            diagnostics.Clear();
        }
        var parser = new Parser(tokens, diagnostics);
        var parsed = parser.Parse();
        if(diagnostics.Any())
        {
            diagnostics.ToList().ForEach(Console.WriteLine);
        }

        Console.WriteLine(parsed);
        var run = WarmLangInterpreter.Run(parsed);
        Console.WriteLine($"Evaluated to: {run}");
    }
    return 0;
}

try 
{
    var diagnostics = new ErrorWarrningBag();
    var lexer = Lexer.FromFile(program, diagnostics);
    var tokens = lexer.Lex();
    if(lexerDebug)
    {
        foreach(var token in tokens)
        {
            Console.WriteLine(token);
        }
    }
    if(diagnostics.Any())
    {
        Console.WriteLine("--Lexer problems--");
        foreach(var err in diagnostics)
        {
            Console.WriteLine(err);
        }
    }

    diagnostics.Clear();
    var parser = new Parser(tokens, diagnostics);
    ASTNode root = parser.Parse();
    if(parserDebug)
    {
        Console.WriteLine($"Parsed:\n\t{root}");
    }
    if(diagnostics.Any())
    {
        Console.WriteLine("--Parser problems --");
        foreach(var err in diagnostics)
        {
            Console.WriteLine(err);
        }
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