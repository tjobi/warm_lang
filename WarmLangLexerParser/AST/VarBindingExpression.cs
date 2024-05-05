namespace WarmLangLexerParser.AST;

public sealed class VarBindingExpression : ExpressionNode
{
    private readonly TokenKind _kind;
    public override TokenKind Kind => _kind;

    public string Name { get; }
    public ExpressionNode RightHandSide { get; }

    public VarBindingExpression(TokenKind type, string name, ExpressionNode rightHandSide)
    {
        _kind = type;
        Name = name;
        RightHandSide = rightHandSide;
    }

    public override string ToString()
    {
        var rhs = RightHandSide.ToString();
        return $"({Name}:{_kind} = {rhs})";
    }

}