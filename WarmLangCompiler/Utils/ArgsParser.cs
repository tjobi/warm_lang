namespace WarmLangCompiler.Utils;

[Flags]
public enum CompilerOptions
{
    None = 0,
    ParserDebug = 1 << 0,
    LexerDebug = 1 << 1,
    BinderDebug = 1 << 2,
    TraceExceptions = 1 << 3,
    Interactive = 1 << 4,
    Evaluate = 1 << 5,
    EmitterDebug = 1 << 6
}

public readonly record struct ParsedArgs(
    string Program,
    CompilerOptions Options = CompilerOptions.None,
    string? OutPath = null
)
{
    public bool ParserDebug => (Options & CompilerOptions.ParserDebug) != 0;
    public bool LexerDebug => (Options & CompilerOptions.LexerDebug) != 0;
    public bool BinderDebug => (Options & CompilerOptions.BinderDebug) != 0;
    public bool EmitterDebug => (Options & CompilerOptions.EmitterDebug) != 0;
    public bool TraceExceptions => (Options & CompilerOptions.TraceExceptions) != 0;
    public bool Interactive => (Options & CompilerOptions.Interactive) != 0;
    public bool Evaluate => (Options & CompilerOptions.Evaluate) != 0;
}

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
        var program = defaultProgram;
        string? outPath = null;
        CompilerOptions options = CompilerOptions.None;
        var isLookingForFile = true;
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
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
                        if (i + 1 < args.Length)
                        {
                            i++;
                            outPath = args[i];
                        }
                        else
                        {
                            ReportError($"Must provide an output path when using \"{arg}\"");
                            Help();
                            return null;
                        }
                    }
                    break;
                case "-is":
                case "--interactive":
                    options |= CompilerOptions.Interactive;
                    break;
                case "--lex-debug":
                    options |= CompilerOptions.LexerDebug;
                    break;
                case "--emitter-debug":
                    options |= CompilerOptions.EmitterDebug;
                    break;
                case "--parser-debug":
                    options |= CompilerOptions.ParserDebug;
                    break;
                case "--binder-debug":
                    options |= CompilerOptions.BinderDebug;
                    break;
                case "--trace":
                    options |= CompilerOptions.TraceExceptions;
                    break;
                case "--eval":
                    options |= CompilerOptions.Evaluate;
                    break;
                default:
                    {
                        if (arg.StartsWith("-"))
                        {
                            ReportError($"Unrecognized command-line option \"{arg}\"");
                            Help();
                            return null;
                        }
                        if (!isLookingForFile)
                        {
                            ReportError($"At most 1 input file may be provided");
                            return null;
                        }
                        program = arg;
                        isLookingForFile = false;
                    }
                    break;
            }
        }
        if (isLookingForFile)
        {
            ReportError("No input file provided");
            return null;
        }
        if (!File.Exists(program))
        {
            ReportError($"No such file '{program}'");
            return null;
        }
        return new ParsedArgs(program, options, outPath);
    }

    public static void ReportError(string msg) => Console.Error.WriteLine("ERROR: " + msg);
}