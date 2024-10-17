using WarmLangCompiler.Utils;
using WarmLangCompiler.Binding;
using WarmLangCompiler.ILGen;
using WarmLangCompiler.Interpreter;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.ErrorReporting;

var DEFAULT_PROGRAM = "SyntaxTest/test.wl";

ParsedArgs? parsedArgs = ArgsParser.ParseArgs(args, DEFAULT_PROGRAM); 
if(parsedArgs is null)
{
    return 16;
}
ParsedArgs pArgs = parsedArgs.Value;

if(pArgs.Interactive) 
{
    return Interactive.Loop();
}

var program = pArgs.Program;
var outfile = pArgs.OutPath ?? Path.Combine(Directory.GetCurrentDirectory(), "out.dll");
if(Path.GetExtension(outfile) != ".dll")
{
    outfile = Path.ChangeExtension(outfile, ".dll");
}

var diagnostics = new ErrorWarrningBag();
var lexer = Lexer.FromFile(program, diagnostics);
var tokens = lexer.Lex();

if(pArgs.LexerDebug)
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
if(pArgs.ParserDebug)
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
if(pArgs.BinderDebug)
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
    Console.WriteLine("Exitting...");
    return 1;
}

if(pArgs.Evaluate)
{
    var res = BoundInterpreter.Run(boundProgram);
    Console.WriteLine($"Evaluated '{program}' -> {res}");
    return 0;
}

File.WriteAllText(outfile, string.Empty);
Emitter.EmitProgram(outfile, boundProgram, diagnostics, debug: pArgs.EmitterDebug);
DefaultRuntimeConfig.Write(outfile);
foreach(var err in diagnostics)
{
    Console.WriteLine(err);
}

Console.WriteLine($"Compiled '{program}' to '{Path.GetFileName(outfile)}' with {diagnostics.Count()} errors");

return 0;