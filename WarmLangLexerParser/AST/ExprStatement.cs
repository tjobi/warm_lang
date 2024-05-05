namespace WarmLangLexerParser.AST;

public sealed class ExprStatement : StatementNode
{

    public ExpressionNode Expression { get; }
    public override TokenKind Kind => Expression.Kind;

    public ExprStatement(ExpressionNode expr)
    {
        Expression = expr;
    }
    public override string ToString() => Expression.ToString() + ";";
}