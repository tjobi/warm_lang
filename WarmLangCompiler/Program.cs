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
var outfile = pArgs.OutPath ?? Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(program));
if(Path.GetExtension(outfile) != ".dll")
{
    outfile = Path.ChangeExtension(outfile, ".dll");
}

var diagnostics = new ErrorWarrningBag();
var lexer = Lexer.FromFile(program, diagnostics);
var tokens = lexer.Lex();

if(pArgs.LexerDebug)
{
    foreach(var token in tokens) Console.WriteLine(token);
    if(diagnostics.Any())
    {
        Console.WriteLine("--Lexer problems--");
        DisplayErrorsAndWarnings();
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
        DisplayErrorsAndWarnings();
    }
}

var binder = new Binder(diagnostics);
var boundProgram = binder.BindProgram(root);
if(pArgs.BinderDebug) Console.WriteLine($"Bound: {boundProgram}");

if(ContainsErrors())
{
    Console.WriteLine("--Compilation failed on: --");
    DisplayErrorsAndWarnings();
    Console.WriteLine("Exitting...");
    return 1;
}

if (pArgs.Evaluate)
{
    DisplayErrorsAndWarnings();
    var res = BoundInterpreter.Run(boundProgram);
    Console.WriteLine($"Evaluated '{program}' -> {res}");
    return 0;
}
else
{
    File.WriteAllText(outfile, string.Empty);
    Emitter.EmitProgram(outfile, boundProgram, diagnostics, debug: pArgs.EmitterDebug);
    if(ContainsErrors())
    {
        DisplayErrorsAndWarnings();
        Console.WriteLine("Exitting...");
        return 1;
    }
    DisplayErrorsAndWarnings();
    DefaultRuntimeConfig.Write(outfile);
    Console.WriteLine($"Compiled '{program}' to '{outfile}'");
    return 0;
}

void DisplayErrorsAndWarnings()
{
    foreach (var record in diagnostics)
    {
        if (record.IsError) Console.ForegroundColor = ConsoleColor.Red;
        else Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(record);
    }
    Console.ResetColor();
}

bool ContainsErrors() => diagnostics.Any(reported => reported.IsError);