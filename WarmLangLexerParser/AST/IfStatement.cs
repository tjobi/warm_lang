namespace WarmLangLexerParser.AST;

public sealed class IfStatement : StatementNode
{
    public ExpressionNode Condition { get; }

    public StatementNode Then { get; }
    public StatementNode? Else { get; }

    public IfStatement(SyntaxToken ifToken, ExpressionNode condition, StatementNode thenPath, StatementNode? elsePath)
    :base(TextLocation.FromTo(ifToken.Location, elsePath?.Location ?? thenPath.Location))
    {
        Condition = condition;
        Then = thenPath;
        Else = elsePath;
    }
    public override string ToString()
    {
        var output = $"(If ({Condition}) then ({Then})";
        if(Else is not null)
        {
            output += $" else ({Else}))";
        }
        return output;
    }
}