namespace WarmLangLexerParser.AST;

public sealed class AccessExpression : ExpressionNode
{
    public Access Access {get;}
    public override TokenKind Kind => TokenKind.TIdentifier;

    public AccessExpression(Access acc)
    {
        Access = acc;
    }
    public override string ToString() => Access.ToString();
}
