namespace WarmLangLexerParser.AST;

public class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }
    public string Operation { get; init; }
    private readonly TokenKind _kind; 

    public override TokenKind Kind => _kind; 

    public BinaryExpressionNode(ExpressionNode left, string op, ExpressionNode right)
    {
        Left = left;
        Right = right;
        Operation = op;
        //TODO:
        _kind = TokenKind.TPlus;
    }
}