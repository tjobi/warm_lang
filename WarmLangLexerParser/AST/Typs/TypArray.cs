namespace WarmLangLexerParser.AST.Typs;

public sealed class TypArray : Typ
{
    public Typ InnerType { get; }

    public TypArray(Typ typ)
    {
        InnerType = typ;
    }
    public override string ToString()
    {
        var innerStr = InnerType.ToString();
        return $"arr<{innerStr}>";
    }
}