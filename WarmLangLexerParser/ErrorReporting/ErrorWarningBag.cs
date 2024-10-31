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

    public void Report(string message, bool isError, int line, int col)
    {
        if(!isMuted)
        {
            _reported.Add(new ReportedErrorWarning(message, isError, line, col));
        }
    }

    public void Report(string message, bool isError, TextLocation location)
    {
        Report(message, isError, location.StartLine, location.StartColumn);
    }

    public void ReportInvalidCharacter(char c, int line, int col)
    {
        var message = $"Invalid character '{c}'";
        Report(message, true, line, col);
    }

    public void ReportExpectedIdentifierInParamDeclaration(SyntaxToken token)
    {
        var message = $"Identifier expected, got: '{token.Kind}'";
        Report(message, true, token.Location);
    }

    public void ReportInvalidExpression(SyntaxToken token)
    {
        var message = $"Invalid expression term '{token.Kind}'";
        Report(message, true, token.Location);
    }

    public void ReportUnexpectedToken(TokenKind expected, TokenKind received, TextLocation location)
    {
        var message = $"Invalid token expected: '{expected}' but got '{received}'";
        Report(message, true, location.StartLine, location.StartColumn);
    }

    public void ReportUnexpectedTokenFromMany(TokenKind[] kinds, TokenKind received, TextLocation location)
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
        Report(message, true, location);
    }

    public void ReportWhileExpectedBlockStatement(SyntaxToken read)
    {
        var message = "The body of a while statement must be a block {}";
        Report(message, true, read.Location);
    }

    internal void ReportExpectedIfStatement(SyntaxToken falseToken)
    {
        var message = "Expected an if-block to form an else-if (or a regular block)";
        Report(message, true, falseToken.Location);
    }

    public void ReportNewLineStringLiteral(TextLocation textLocation)
    {
        var message = "Newline in constant";
        Report(message, true, textLocation);
    }

    public void ReportKeywordOnlyAllowedInTopScope(TokenKind keyword, TextLocation location) 
    {
        var message = $"The keyword '{keyword.AsString()}' may only appear in the top level";
        Report(message, true, location);
    }
}
