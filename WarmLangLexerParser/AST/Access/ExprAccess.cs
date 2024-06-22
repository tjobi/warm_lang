namespace WarmLangLexerParser.AST;

public sealed class ExprAccess : Access
{
    public ExpressionNode Expression { get; }

    public ExprAccess(ExpressionNode expr)
    :base(expr.Location)
    {
        Expression = expr;
    }

    public override string ToString() => $"Access {Expression}";
}