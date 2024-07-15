using WarmLangCompiler;
using WarmLangCompiler.Binding;
using WarmLangCompiler.ILGen;
using WarmLangCompiler.Interpreter;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.ErrorReporting;
using WarmLangLexerParser.Exceptions;

var DEFAULT_PROGRAM = "SyntaxTest/test.test";

var parsedArgs = ArgsParser.ParseArgs(args, DEFAULT_PROGRAM);
if(parsedArgs is null)
{
    return 16;
}
var (program, parserDebug, 
     lexerDebug, binderDebug,
     longExceptions, interactive,
     isEvaluateMode) = (ParsedArgs) parsedArgs!;

if(interactive) 
{
    return Interactive.Loop();
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
        if(diagnostics.Any())
        {
            Console.WriteLine("--Lexer problems--");
            foreach(var err in diagnostics)
            {
                Console.WriteLine(err);
            }
        }
    }

    var parser = new Parser(tokens, diagnostics);
    ASTNode root = parser.Parse();
    if(parserDebug)
    {
        Console.WriteLine($"Parsed:\n\t{root}");
        if(diagnostics.Any())
        {
            Console.WriteLine("--Parser problems--");
            foreach(var err in diagnostics)
            {
                Console.WriteLine(err);
            }

        }
    }

    var binder = new Binder(diagnostics);
    var boundProgram = binder.BindProgram(root);
    if(binderDebug)
    {
        Console.WriteLine($"Bound: {boundProgram}");
    }
    if(diagnostics.Any())
    {
        Console.WriteLine("--Compilation failed on: --");
        foreach(var err in diagnostics)
        {
            Console.WriteLine(err);
        }
        Console.WriteLine("Exitting... no evaluation");
        return 1;
    }

    if(isEvaluateMode)
    {
        var res = BoundInterpreter.Run(boundProgram);
        Console.WriteLine($"Evaluated '{program}' -> {res}");
        return 0;
    }

    Emitter.EmitProgram(boundProgram, diagnostics);
    foreach(var err in diagnostics)
    {
        Console.WriteLine(err);
    }
    Console.WriteLine($"Compiled '{program}' to 'out.dll' with {diagnostics.Count()} errors");

} catch(ParserException e)
{
    Console.WriteLine(longExceptions ? e : e.Message);
    return 20;
}
catch (NotImplementedException e )
{
    Console.WriteLine(longExceptions ? e : e.Message);
    return 30;
}



return 0;