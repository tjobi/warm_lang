using System.Collections;
using System.Text;

namespace WarmLangLexerParser.ErrorReporting;

public sealed class ErrorWarrningBag : IEnumerable<ReportedErrorWarning>
{
    private readonly List<ReportedErrorWarning> _reported;
    private bool isMuted;

    public ErrorWarrningBag()
    {
        _reported = new List<ReportedErrorWarning>();
        isMuted = false;
    }

    public IEnumerator<ReportedErrorWarning> GetEnumerator() => _reported.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _reported.GetEnumerator();

    public void Clear() => _reported.Clear();

    public void Mute() => isMuted = true;
    public void UnMute() => isMuted = false;

    private void Report(string message, bool isError, int line, int col)
    {
        if(!isMuted)
        {
            _reported.Add(new ReportedErrorWarning(message, isError, line, col));
        }
    }

    public void ReportInvalidCharacter(char c, int line, int col)
    {
        var message = $"Invalid character '{c}'";
        Report(message, true, line, col);
    }

    public void ReportExpectedIdentifierInParamDeclaration(SyntaxToken token)
    {
        var message = $"Identifier expected, got: '{token.Kind}'";
        int line = token.Line;
        int col = token.Column;
        Report(message, true, line, col);
    }

    public void ReportInvalidExpression(SyntaxToken token)
    {
        var message = $"Invalid expression term '{token.Kind}'";
        Report(message, true, token.Line, token.Column);
    }

    public void ReportUnexpectedToken(TokenKind expected, TokenKind received, int line, int col)
    {
        var message = $"Invalid token expected: '{expected}' but got '{received}'";
        Report(message, true, line, col);
    }

    public void ReportUnexpectedTokenFromMany(TokenKind[] kinds, TokenKind received, int line, int col)
    {
        var sb = new StringBuilder().Append('<');
        for (int i = 0; i < kinds.Length; i++)
        {
            var kind = kinds[i];
            sb.Append(kind);
            if(i < kinds.Length-1)
            {
                sb.Append(", ");
            }
        }
        sb.Append('>');
        var message = $"Invalid token expected to be in: '{sb}' but got '{received}'";
        Report(message, true, line, col);
    }
}
