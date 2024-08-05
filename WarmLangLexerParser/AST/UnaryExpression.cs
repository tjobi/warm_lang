namespace WarmLangLexerParser.AST;

public sealed class UnaryExpression : ExpressionNode
{
    public ExpressionNode Expression { get; }
    public SyntaxToken Operator { get; }
    public string Operation => Operator.Kind.AsString();


    public UnaryExpression(SyntaxToken op, ExpressionNode expr)
    :base(TextLocation.FromTo(op.Location, expr.Location))
    {
        Expression = expr;
        Operator = op;
    }
    public override string ToString()
    {
        if(Operator.Kind.IsPrefixUnaryOperator())
        {
            return $"({Operation}{Expression})";
        }
        return $"({Expression}{Operation})";
    }
}