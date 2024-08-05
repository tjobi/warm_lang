namespace WarmLangLexerParser.AST;

public sealed class AssignmentExpression : ExpressionNode
{
    public Access Access { get; }
    public ExpressionNode RightHandSide { get; }

    public SyntaxToken Operator { get; }

    public string Operation => Operator.Kind.AsString();

    public AssignmentExpression(Access target, SyntaxToken op, ExpressionNode rightHandSide)
    :base(TextLocation.FromTo(target.Location, rightHandSide.Location))
    {
        Access = target;
        RightHandSide = rightHandSide;
        Operator = op;
    }

    public AssignmentExpression(ExpressionNode expr, SyntaxToken op, ExpressionNode rightHandSide)
    : this(expr is AccessExpression ae ? ae.Access : new ExprAccess(expr), op, rightHandSide) { }
    
    public override string ToString() => $"(Assign {Access} = {RightHandSide})";
}