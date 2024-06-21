namespace WarmLangLexerParser;

public record SyntaxToken 
{
    public TokenKind Kind { get; init; }
    public string? Name { get; init; }
    public int? IntValue { get; init; }

    public TextLocation Location { get; }

    public SyntaxToken(TokenKind kind, TextLocation location)
    {
        Kind = kind;
        Location = location;
    }

    public SyntaxToken(TokenKind kind, TextLocation location, 
                       string? name, int intValue) 
    : this(kind, location)
    { 
        Name = name;
        IntValue = intValue;
    }

    public SyntaxToken(TokenKind kind, int startLine, 
                       int endLine, int startCol, 
                       int endCol, string? name, 
                       int intValue) 
    : this(kind, new TextLocation(startLine, endLine, startCol, endCol), name, intValue)
    { }

    public static SyntaxToken MakeToken(TokenKind kind, int line, int col, string? name = null, int? intValue = null)
    {
        return MakeToken(kind, new TextLocation(line, col), name, intValue);
    }

    public static SyntaxToken MakeToken(TokenKind kind, int startLine, int endLine, int startCol, int endCol, string? name = null, int? intValue = null)
    {
        return MakeToken(kind, new TextLocation(startLine, startCol, endLine, endCol), name, intValue);
    }

    public static SyntaxToken MakeToken(TokenKind kind, TextLocation location, string? name = null, int? intValue = null)
    {
        return kind switch 
        {
            TokenKind.TIdentifier or TokenKind.TConst => new SyntaxToken(kind, location, name, intValue ?? 0),
            _ => new SyntaxToken(kind, location, null, 0)
        };
    }
}