namespace WarmLangLexerParser.AST.TypeSyntax;

public static class TypeSyntaxExtensions
{
    public static TokenKind ToTokenKind(this TypeSyntaxNode typ) => typ switch 
    {
        TypeSyntaxInt => TokenKind.TInt,
        TypeSyntaxList => TokenKind.TArray,
        TypeSyntaxString => TokenKind.TString,
        TypeSyntaxBool => TokenKind.TString,
        _ => TokenKind.TVar //TODO: user-defined types?
    };
}