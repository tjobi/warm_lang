namespace WarmLangLexerParser.AST;

public sealed class TopLevelVarDeclaration : TopLevelStamentNode
{
    public TopLevelVarDeclaration(VarDeclaration decl) : base(decl)
    {
        Declaration = decl;
    }

    public VarDeclaration Declaration { get; }

    public override StatementNode Statement => Declaration;

    public override string ToString() => Declaration.ToString();
}
