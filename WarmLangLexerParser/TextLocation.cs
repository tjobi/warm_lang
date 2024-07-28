namespace WarmLangLexerParser;

/// <summary>
/// Represents a location in the source code.
/// </summary>
/// <param name="StartLine"></param>
/// <param name="StartColumn"></param>
/// <param name="EndLine">By default is the same as the startLine</param>
/// <param name="EndColumn">The column after the last character following a token -> it is exclusive</param>
public record TextLocation(int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public TextLocation(int startLine, int startColumn, int length = 1)
    : this(startLine, startColumn, startLine, startColumn+length) { }

    public static TextLocation FromTo(TextLocation start, TextLocation end)
    {
        return new TextLocation(start.StartLine, start.StartColumn, end.EndLine, end.EndColumn);
    }

    public int Length => EndColumn - StartColumn;
    
    public static TextLocation FromTo(SyntaxToken from, SyntaxToken to) => FromTo(from.Location, to.Location);

    public static readonly TextLocation EmptyFile = new(1,1, 1);  

    public override string ToString()
    {
        return $"({StartLine},{StartColumn})-({EndLine},{EndColumn})";
    }
}