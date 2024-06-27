namespace WarmLangLexerParser.AST;

public sealed class ReturnStatement : StatementNode
{
    public ReturnStatement(SyntaxToken returnToken, ExpressionNode? expression)
    : base(TextLocation.FromTo(returnToken.Location, expression?.Location ?? returnToken.Location))
    {
        ReturnToken = returnToken;
        Expression = expression;
    }

    public SyntaxToken ReturnToken { get; }
    public ExpressionNode? Expression { get; }

    public override string ToString() => $"return {Expression?.ToString() ?? string.Empty};";
}