namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundExprAccess : BoundAccess
{
    public BoundExprAccess(BoundExpression expression) : base(expression.Type)
    {
        Expression = expression;
    }

    public BoundExpression Expression { get; }
}
