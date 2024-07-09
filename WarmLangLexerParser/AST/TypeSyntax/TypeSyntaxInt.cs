namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxInt : TypeSyntaxNode
{
    public TypeSyntaxInt(TextLocation location) : base(location) { }

    public override string ToString() => "int";
}