using WarmLangCompiler.Binding;
using WarmLangLexerParser;
using WarmLangLexerParser.ErrorReporting;

namespace WarmLangCompiler.Interpreter;

public static class Interactive
{
    public static int Loop()
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
            var parser = new Parser(tokens, diagnostics);
            var parsed = parser.Parse();
            var binder = new Binder(diagnostics);
            var prog = binder.BindProgram(parsed);

            if(diagnostics.Any())
            {
                diagnostics.ToList().ForEach(Console.WriteLine);
            }
            else
            {
                Console.WriteLine(prog);
                var run = BoundInterpreter.Run(prog);
                Console.WriteLine($"Evaluated to: {run}");
            }
        }
        return 0;
    }   
}