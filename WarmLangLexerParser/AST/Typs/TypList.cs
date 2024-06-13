namespace WarmLangLexerParser.AST.Typs;

public sealed class TypList : TypeClause
{
    public TypeClause InnerType { get; }

    public TypList(TypeClause typ)
    {
        InnerType = typ;
    }
    public override string ToString()
    {
        var innerStr = InnerType.ToString();
        return $"arr<{innerStr}>";
    }
}