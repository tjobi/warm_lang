namespace WarmLangLexerParser.AST;


public sealed class NameAccess : Access
{
    public string Name { get; }

    public NameAccess(SyntaxToken nameToken)
    {
        Name = nameToken.Name!;
    }

    public override string ToString() => Name;
}