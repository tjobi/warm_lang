namespace WarmLangLexerParser.AST;

public sealed class AccessExpression : ExpressionNode
{
    public Access Access { get; }

    public AccessExpression(Access acc)
    :base(acc.Location)
    {
        Access = acc;
    }
    public override string ToString() => Access.ToString();
}
