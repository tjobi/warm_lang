namespace WarmLangLexerParser;

public enum TokenKind
{
    TInt, TConst,
    TIdentifier, //Variable names, function names...
    TSemiColon, TNewLine, TEOF,
    TEqual, TPlus, TStar, 
    TBlock, TCurLeft, TCurRight
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