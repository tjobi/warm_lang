namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxString : TypeSyntaxNode
{
    public TypeSyntaxString(TextLocation location) : base(location) { }

    public override string ToString() => "string";
}