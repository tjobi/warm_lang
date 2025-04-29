namespace WarmLangLexerParser.AST;

public sealed class TopLevelArbitraryStament : TopLevelStamentNode
{
    public TopLevelArbitraryStament(StatementNode statement) : base(statement)
    {
        Statement = statement;
    }

    public override StatementNode Statement { get; }

    public bool IsBlock => Statement is BlockStatement;

    public override string ToString() => Statement.ToString();

}