namespace WarmLangLexerParser.AST.Typs;

public static class TypeClauseExtensions
{
    public static TokenKind ToTokenKind(this TypeClause typ) => typ switch 
    {
        TypInt => TokenKind.TInt,
        TypList => TokenKind.TArray,
        _ => TokenKind.TVar //TODO: user-defined types?
    };
}