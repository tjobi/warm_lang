namespace WarmLangLexerParser.AST;


public sealed class NameAccess : Access
{
    public string Name { get; }

    public NameAccess(SyntaxToken nameToken)
    :base(nameToken.Location)
    {
        Name = nameToken.Name!;
    }

    public override string ToString() => Name;
}