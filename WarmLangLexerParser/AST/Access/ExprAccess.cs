namespace WarmLangLexerParser.AST;

public sealed class ExprAccess : Access
{
    public ExpressionNode Expression { get; }

    public ExprAccess(ExpressionNode expr)
    {
        Expression = expr;
    }

    public override string ToString() => $"Access {Expression}";
}