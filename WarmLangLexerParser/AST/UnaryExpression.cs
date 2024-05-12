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
        Operation = op.Kind switch 
        {
            TokenKind.TMinus => "-",
            TokenKind.TPlus => "+",
            _ => throw new Exception($"On line {op.Line}, col: {op.Column} - Invalid Unary operator {op.Kind}")
        };
    }

    public override TokenKind Kind => _kind;

    public override string ToString() => $"({Operation}{Expression})";
}