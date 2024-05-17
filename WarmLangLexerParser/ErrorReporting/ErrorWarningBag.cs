using System.Collections;

namespace WarmLangLexerParser.ErrorReporting;

public sealed class ErrorWarrningBag : IEnumerable<ReportedErrorWarning>
{
    private readonly List<ReportedErrorWarning> _reported;

    public ErrorWarrningBag()
    {
        _reported = new List<ReportedErrorWarning>();
    }

    public IEnumerator<ReportedErrorWarning> GetEnumerator() => _reported.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _reported.GetEnumerator();

    public void Clear() => _reported.Clear();

    private void Report(string message, bool isError, int line, int col)
    {
        _reported.Add(new ReportedErrorWarning(message, isError, line, col));
    }

    public void ReportInvalidCharacter(char c, int line, int col)
    {
        var message = $"Invalid character '{c}'";
        Report(message, true, line, col);
    }

    public void ReportInvalidParameterParameterDeclartion(SyntaxToken token)
    {
        var message = $"Unexpected expression in parameter declaration: '{token}'";
        int line = token.Line;
        int col = token.Column;
        Report(message, true, line, col);
    }

    public void ReportInvalidExpression(SyntaxToken token)
    {
        var message = $"Invalid expression term '{token.Kind}'";
        Report(message, true, token.Line, token.Column);
    }
}
