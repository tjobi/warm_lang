namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxList : TypeSyntaxNode
{
    public TypeSyntaxNode InnerType { get; }

    public TypeSyntaxList(TextLocation location, TypeSyntaxNode typ): base(location)
    {
        InnerType = typ;
    }
    public override string ToString()
    {
        var innerStr = InnerType.ToString();
        return $"list<{innerStr}>";
    }
}