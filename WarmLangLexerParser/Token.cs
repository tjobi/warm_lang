namespace WarmLangLexerParser;

public enum TokenKind
{
    TInt, TVariableName, TConst, TEqual, TSemiColon, TPlus, TNumber, TEOF, TNewLine,
}

public record SyntaxToken 
{
    public TokenKind Kind { get; init; }
    public string? Name { get; init; }
    public int? IntValue { get; init; }
}