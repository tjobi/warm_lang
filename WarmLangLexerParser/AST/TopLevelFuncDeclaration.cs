namespace WarmLangLexerParser.AST;

public sealed class TopLevelFuncDeclaration : TopLevelStamentNode
{
    public TopLevelFuncDeclaration(FuncDeclaration funcDecl) : base(funcDecl)
    {
        Declaration = funcDecl;
    }

    public FuncDeclaration Declaration { get; }

    public override StatementNode Statement => Declaration;

    public override string ToString() => Declaration.ToString();
}