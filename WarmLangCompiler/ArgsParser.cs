namespace WarmLangCompiler;

public record struct ParsedArgs(
    string Program, 
    bool ParserDebug = false, 
    bool LexerDebug = false, 
    bool TraceExceptions = false,
    bool Interactive = false
    );
public static class ArgsParser
{
    private static void Help()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("\tprogram            - path to a .test file to lex, parse, and evaluate");
        Console.WriteLine("\t-lh, --lang-help   - Show command line help");
        Console.WriteLine("\t--lex-debug        - enables debug information for lexer");
        Console.WriteLine("\t--parser-debug     - enables debug information for parser");
        Console.WriteLine("\t--trace            - prints a stacktrace if an exception goes uncaught");
        Console.WriteLine("\t-is,--interactive  - enables interactive mode");
    }

    public static ParsedArgs? ParseArgs(string[] args, string defaultProgram)
    {
        var parsedArgs = new ParsedArgs(defaultProgram);

        var isLookingForFile = true;
        var argsContainedProgram = false;
        foreach (var arg in args)
        {
            switch(arg)
            {
                case "-lh": //short for "lang help" otherwise it is caught by "dotnet run"
                case "--lang-help": 
                {
                    Help();
                    return null;
                }
                case "-is":
                case "--interactive":
                {
                    parsedArgs.Interactive = true;
                } break;
                case "--lex-debug":
                {
                    parsedArgs.LexerDebug = true;
                } break;
                case "--parser-debug":
                {
                    parsedArgs.ParserDebug = true;
                } break;
                case "--trace":
                {
                    parsedArgs.TraceExceptions = true;
                } break;
                default: 
                {
                    if(File.Exists(arg) && isLookingForFile)
                    {
                        parsedArgs.Program = arg;
                        isLookingForFile = false;
                        argsContainedProgram = true;
                    } else 
                    {
                        Console.WriteLine($"INFO ARGS: Invalid arg \"{arg}\"");
                        Help();
                        return null;
                    }
                } break;
            }
        }
        if(parsedArgs.Program == defaultProgram && !argsContainedProgram)
        {
            Console.WriteLine("INFO ARGS: No program provided, using default: \"" + defaultProgram + "\"");
        }
        return parsedArgs;
    }

    public static void Deconstruct(this ParsedArgs args, out string program, out bool parserDebug,
                                   out bool lexerDebug, out bool longExceptions, out bool interactive )
    {
        program = args.Program;
        parserDebug = args.ParserDebug;
        lexerDebug = args.LexerDebug;
        longExceptions = args.TraceExceptions;
        interactive = args.Interactive;
        return;
    }
}