namespace WarmLangLexerParser;

public enum TokenKind
{
    TInt, TConst,
    TVariableName, 
    TSemiColon, TNewLine, TEOF, 
    TEqual, TPlus, TStar
}

public record SyntaxToken 
{
    public TokenKind Kind { get; init; }
    public string? Name { get; init; }
    public int? IntValue { get; init; }

    public SyntaxToken(TokenKind kind)
    {
        Kind = kind;
    }

    public SyntaxToken(TokenKind kind, string? name, int intValue)
    {
        Kind = kind;
        Name = name;
        IntValue = intValue;
    }
}