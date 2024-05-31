namespace WarmLangCompiler;

public record struct ParsedArgs(string Program, bool ParserDebug, bool LexerDebug, bool TraceExceptions);

public static class ArgsParser
{
    private static void Help()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("\tprogram        - path to a .test file to lex, parse, and evaluate");
        Console.WriteLine("\t--lex-debug    - enables debug information for lexer");
        Console.WriteLine("\t--parser-debug - enables debug information for parser");
        Console.WriteLine("\t--trace        - prints a stacktrace if an exception goes uncaught");
    }

    public static ParsedArgs? ParseArgs(string[] args, string defaultProgram)
    {
        var program = defaultProgram;
        var lexerDebug = false;
        var longExceptions = false;
        var parserDebug = false;

        var isLookingForFile = true;
        var argsContainedProgram = false;
        foreach (var arg in args)
        {
            switch(arg)
            {
                case "-lh": //short for "lang help" otherwise it is caught by "dotnet run"
                case "--lang-help": {
                    Help();
                    return null;
                }
                case "--lex-debug":
                {
                    lexerDebug = true;
                } break;
                case "--parser-debug":
                {
                    parserDebug = true;
                } break;
                case "--trace":
                {
                    longExceptions = true;
                } break;
                default: {
                    if(File.Exists(arg) && isLookingForFile)
                    {
                        program = arg;
                        isLookingForFile = false;
                        argsContainedProgram = true;
                    } else 
                    {
                        Console.WriteLine($"INFO ARGS: Invalid arg \"{arg}\"");
                        Help();
                        return null;
                    }
                }break;
            }
        }
        if(program == defaultProgram && !argsContainedProgram)
        {
            Console.WriteLine("INFO ARGS: No program provided, using default: \"" + defaultProgram + "\"");
        }
        return new ParsedArgs(program, parserDebug, lexerDebug, longExceptions);
    }

    public static void Deconstruct(this ParsedArgs args, out string program, out bool parserDebug,
                                   out bool lexerDebug, out bool longExceptions )
    {
        program = args.Program;
        parserDebug = args.ParserDebug;
        lexerDebug = args.LexerDebug;
        longExceptions = args.TraceExceptions;
        return;
    }
}