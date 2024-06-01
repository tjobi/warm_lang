namespace WarmLangLexerParser.AST;

public sealed class SubscriptAccess : Access
{
    public Access Target { get; }
    public ExpressionNode Index { get; set; }

    public SubscriptAccess(Access target, ExpressionNode idx)
    {
        Target = target;
        Index = idx;
    }
    public override string ToString() => $"({Target}[{Index}])";
}