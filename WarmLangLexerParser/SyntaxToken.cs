namespace WarmLangLexerParser;

public record SyntaxToken 
{
    public TokenKind Kind { get; init; }
    public string? Name { get; init; }
    public int? IntValue { get; init; }

    public int Line { get; set; }
    public int Column { get; set; }

    public SyntaxToken(TokenKind kind, int line, int col)
    {
        Kind = kind;
        Line = line;
        Column = col;
    }

    public SyntaxToken(TokenKind kind, int line, int col, string? name, int intValue) 
        : this(kind,line,col)
    {
        Name = name;
        IntValue = intValue;
    }

    public static SyntaxToken MakeToken(TokenKind kind, int line, int col, string? name = null, int? intValue = null)
    {
        return kind switch 
        {
            TokenKind.TIdentifier or TokenKind.TConst => new SyntaxToken(kind, line, col, name, intValue ?? 0),
            _ => new SyntaxToken(kind, line, col)
        };
    }
}