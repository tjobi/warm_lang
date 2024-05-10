namespace WarmLangLexerParser.AST;

public sealed class VarAssignmentExpression : ExpressionNode
{
    public override TokenKind Kind => TokenKind.TAssign;

    public string Name { get; }
    public ExpressionNode RightHandSide { get; }
    public VarAssignmentExpression(SyntaxToken nametoken, ExpressionNode rightHandSide)
    {
        Name = nametoken.Name!;
        RightHandSide = rightHandSide;
    }
    public override string ToString() => $"(Assign {Name} = {RightHandSide})";
}