namespace WarmLangLexerParser.AST.Typs;

public static class TypExtensions
{
    public static TokenKind ToTokenKind(this Typ typ) => typ switch 
    {
        TypInt => TokenKind.TInt,
        _ => TokenKind.TVar //TODO: user-defined types?
    };
}