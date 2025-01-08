namespace WarmLangLexerParser.AST;

public abstract class ExpressionNode : ASTNode
{
    protected ExpressionNode(TextLocation location) : base(location) { }
}

public sealed class NullExpression : ExpressionNode
{
    public NullExpression(SyntaxToken nullToken) : base(nullToken.Location)
    {
        Token = nullToken;
    }

    public SyntaxToken Token { get; }

    public override string ToString() => "null";
}