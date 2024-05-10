namespace WarmLangLexerParser.AST;

public sealed class IfStatement : StatementNode
{
    public override TokenKind Kind => TokenKind.TIfStmnt;

    public ExpressionNode Condition { get; }

    public StatementNode Then { get; }
    public StatementNode Else { get; }

    public IfStatement(ExpressionNode condition, StatementNode thenPath, StatementNode elsePath)
    {
        Condition = condition;
        Then = thenPath;
        Else = elsePath;
    }
    public override string ToString() => $"(If ({Condition}) then ({Then}) else ({Else}))";
}