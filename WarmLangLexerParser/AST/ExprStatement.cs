namespace WarmLangLexerParser.AST;

public sealed class ExprStatement : StatementNode
{

    public ExpressionNode Expression { get; }

    public ExprStatement(ExpressionNode expr)
    :base(expr.Location)
    {
        Expression = expr;
    }
    public override string ToString() => Expression.ToString() + ";";
}