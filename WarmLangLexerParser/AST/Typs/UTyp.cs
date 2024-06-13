namespace WarmLangLexerParser.AST.Typs;

public sealed class UTyp : TypeClause
{
    public string Name { get; }
    public UTyp(SyntaxToken name)
    {
        Name = name.Name!;
    }
    public override string ToString() => Name;
}