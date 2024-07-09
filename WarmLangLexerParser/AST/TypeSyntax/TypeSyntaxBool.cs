namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxBool : TypeSyntaxNode
{
    public TypeSyntaxBool(TextLocation location) : base(location) { }

    public override string ToString() => "bool";
}