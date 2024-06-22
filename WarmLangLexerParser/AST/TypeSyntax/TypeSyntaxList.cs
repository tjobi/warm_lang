namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxList : ATypeSyntax
{
    public ATypeSyntax InnerType { get; }

    public TypeSyntaxList(TextLocation location, ATypeSyntax typ): base(location)
    {
        InnerType = typ;
    }
    public override string ToString()
    {
        var innerStr = InnerType.ToString();
        return $"list<{innerStr}>";
    }
}