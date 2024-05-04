namespace WarmLangLexerParser;

public enum Token
{
    TInt, TVariableName, TConst, TEqual, TSemiColon, TPlus, TNumber, TEOF, TNewLine,
}

public record SyntaxToken 
{
    public Token Kind { get; init; }
    public string? Name { get; init; }
    public int? IntValue { get; init; }
}