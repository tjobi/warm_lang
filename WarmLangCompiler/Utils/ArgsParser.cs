namespace WarmLangCompiler.Utils;

public record struct ParsedArgs(
    string Program, 
    bool ParserDebug = false, 
    bool LexerDebug = false, 
    bool BinderDebug = false,
    bool TraceExceptions = false,
    bool Interactive = false,
    bool Evaluate = false,
    bool EmitterDebug = false,
    string? OutPath = null
    );
public static class ArgsParser
{
    private static void Help()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("\tinput file         - path to a .wl file");
        Console.WriteLine("\t-o, --out          - specifies the outpath of compiled dll");
        Console.WriteLine("\t-lh, --lang-help   - Show command line help");
        Console.WriteLine("\t--lex-debug        - enables debug information for lexer");
        Console.WriteLine("\t--parser-debug     - enables debug information for parser");
        Console.WriteLine("\t--binder-debug     - enables debug information for binder");
        Console.WriteLine("\t--emitter-debug    - enables debug information from emitter");
        Console.WriteLine("\t--trace            - prints a stacktrace if an exception goes uncaught");
        Console.WriteLine("\t-is,--interactive  - enables interactive mode");
        Console.WriteLine("\t--eval             - evaluates the compiled program used the interpreter");
    }

    public static ParsedArgs DefaultArgs(string program) => new(program);

    public static ParsedArgs? ParseArgs(string[] args, string defaultProgram)
    {
        var parsedArgs = new ParsedArgs(defaultProgram);
        var isLookingForFile = true;
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch(arg)
            {
                case "-lh": //short for "lang help" otherwise it is caught by "dotnet run"
                case "--lang-help": 
                {
                    Help();
                    return null;
                }
                case "-o":
                case "--out":
                {
                    if(i+1 < args.Length)
                    {
                        i++;
                        parsedArgs.OutPath = args[i];
                    } else
                    {
                        ReportError($"Must provide an output path when using \"{arg}\"");
                        Help();
                        return null;
                    }
                } break;
                case "-is":
                case "--interactive":
                {
                    parsedArgs.Interactive = true;
                } break;
                case "--lex-debug":
                {
                    parsedArgs.LexerDebug = true;
                } break;
                case "--emitter-debug":
                {
                    parsedArgs.EmitterDebug = true;
                } break;
                case "--parser-debug":
                {
                    parsedArgs.ParserDebug = true;
                } break;
                case "--binder-debug":
                {
                    parsedArgs.BinderDebug = true;
                } break;
                case "--trace":
                {
                    parsedArgs.TraceExceptions = true;
                } break;
                case "--eval":
                {
                    parsedArgs.Evaluate = true;
                } break;
                default: 
                {
                    if(arg.StartsWith("-"))
                    {
                        ReportError($"Unrecognized command-line option \"{arg}\"");
                        Help();
                        return null;
                    }
                    if(!isLookingForFile)
                    {
                        ReportError($"At most 1 input file may be provided");
                        return null;
                    }
                    parsedArgs.Program = arg;
                    isLookingForFile = false;
                } break;
            }
        }
        if(isLookingForFile)
        {
            ReportError("No input file provided");
            return null;
        }
        return parsedArgs;
    }

    public static void ReportError(string msg) => Console.WriteLine("ERROR: " + msg);

    public static void Deconstruct(this ParsedArgs args, out string program, out bool parserDebug,
                                   out bool lexerDebug, out bool binderDebug,
                                   out bool longExceptions, out bool interactive, out bool shouldEvaluate,
                                   out bool emitterDebug, out string? outPath)
    {
        program = args.Program;
        parserDebug = args.ParserDebug;
        lexerDebug = args.LexerDebug;
        longExceptions = args.TraceExceptions;
        interactive = args.Interactive;
        binderDebug = args.BinderDebug;
        shouldEvaluate = args.Evaluate;
        emitterDebug = args.EmitterDebug;
        outPath = args.OutPath;
        return;
    }
}