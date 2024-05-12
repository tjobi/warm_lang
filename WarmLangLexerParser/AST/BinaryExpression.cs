namespace WarmLangLexerParser.AST;

public class BinaryExpression : ExpressionNode
{
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }
    public string Operation { get; init; }
    private readonly TokenKind _kind; 

    public override TokenKind Kind => _kind; 

    public BinaryExpression(ExpressionNode left, SyntaxToken op, ExpressionNode right)
    {
        Left = left;
        Right = right;
        Operation = op.Kind switch {
            TokenKind.TPlus => "+",
            TokenKind.TStar => "*",
            TokenKind.TMinus => "-",
            _ => throw new NotImplementedException($"{op.Kind} is not yet supported!")
        };
        _kind = op.Kind;
    }

    public override string ToString()
    {
        var leftStr = Left.ToString();
        var rightStr = Right.ToString();
        return $"({leftStr} {Operation} {rightStr})";
    }
}