namespace WarmLangLexerParser.AST;

public sealed class UnaryExpression : ExpressionNode
{
    public ExpressionNode Expression { get; }
    public SyntaxToken Operator { get; }
    public override TokenKind Kind => Operator.Kind;
    public string Operation => Kind.AsString();


    public UnaryExpression(SyntaxToken op, ExpressionNode expr)
    {
        Expression = expr;
        Operator = op;
    }
    public override string ToString()
    {
        if(Operator.Kind.IsPrefixUnaryExpression())
        {
            return $"({Operation}{Expression})";
        }
        return $"({Expression}{Operation})";
    }
}