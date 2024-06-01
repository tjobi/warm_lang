namespace WarmLangLexerParser.AST;

public sealed class AssignmentExpression : ExpressionNode
{
    public override TokenKind Kind => TokenKind.TAssign;

    public Access Access { get; }
    public ExpressionNode RightHandSide { get; }
    public AssignmentExpression(Access target, ExpressionNode rightHandSide)
    {
        Access = target;
        RightHandSide = rightHandSide;
    }
    public override string ToString() => $"(Assign {Access} = {RightHandSide})";
}