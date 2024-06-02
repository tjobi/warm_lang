namespace WarmLangLexerParser.AST.Typs;

public sealed class TypList : Typ
{
    public Typ InnerType { get; }

    public TypList(Typ typ)
    {
        InnerType = typ;
    }
    public override string ToString()
    {
        var innerStr = InnerType.ToString();
        return $"arr<{innerStr}>";
    }
}