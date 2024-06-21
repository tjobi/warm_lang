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
    public TextLocation(int startLine, int startColumn) : this(startLine, startColumn, startLine, startColumn+1)
    {
        
    }
}