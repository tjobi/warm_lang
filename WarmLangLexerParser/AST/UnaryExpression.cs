namespace WarmLangLexerParser.AST;

public sealed class UnaryExpression : ExpressionNode
{
    public string Operation { get; }
    public ExpressionNode Expression { get; }

    private readonly TokenKind _kind;

    public UnaryExpression(SyntaxToken op, ExpressionNode expr)
    {
        Expression = expr;
        _kind = op.Kind;
        Operation = op.Kind.AsString();
    }

    public override TokenKind Kind => _kind;

    public override string ToString()
    {
        if(_kind.IsPrefixUnaryExpression())
        {
            return $"({Operation}{Expression})";
        }
        return $"({Expression}{Operation})";
    }
}