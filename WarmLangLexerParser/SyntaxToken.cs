namespace WarmLangLexerParser;

public record SyntaxToken 
{
    public TokenKind Kind { get; private set; }
    public string? Name { get; private set; }
    public int? IntValue { get; private set; }

    public TextLocation Location { get; private set; }

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

    public SyntaxToken Extend(TokenKind newKind, TextLocation extraLocation) 
    {
        Location = TextLocation.FromTo(Location, extraLocation);
        Kind = newKind;
        return this;
    }

    public SyntaxToken Extend(TokenKind newKind, int extraRow, int extraCol) 
    {
        Location = TextLocation.FromTo(Location, new TextLocation(extraRow, extraCol));
        Kind = newKind;
        return this;
    }

    public static SyntaxToken MakeToken(TokenKind kind, int line, int col, string? name = null, int? intValue = null)
    {
        return MakeToken(kind, new TextLocation(line, col), name, intValue);
    }

    public static SyntaxToken MakeToken(TokenKind kind, int startLine, int startCol, int endLine, int endCol, string? name = null, int? intValue = null)
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