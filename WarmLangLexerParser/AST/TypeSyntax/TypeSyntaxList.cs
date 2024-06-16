namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxList : ATypeSyntax
{
    public ATypeSyntax InnerType { get; }

    public TypeSyntaxList(ATypeSyntax typ)
    {
        InnerType = typ;
    }
    public override string ToString()
    {
        var innerStr = InnerType.ToString();
        return $"arr<{innerStr}>";
    }
}