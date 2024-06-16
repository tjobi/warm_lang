namespace WarmLangLexerParser.AST;
using static TokenKind;

public class BinaryExpression : ExpressionNode
{
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }
    public SyntaxToken Operator { get; set; }

    public string Operation => Operator.Kind.AsString();
    public override TokenKind Kind => Operator.Kind; 

    public BinaryExpression(ExpressionNode left, SyntaxToken op, ExpressionNode right)
    {
        Left = left;
        Right = right;
        Operator = op;
    }

    public override string ToString()
    {
        var leftStr = Left.ToString();
        var rightStr = Right.ToString();
        return $"({leftStr} {Operation} {rightStr})";
    }
}