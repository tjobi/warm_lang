namespace WarmLangLexerParser.AST;

public sealed class ParenthesesExpression : ExpressionNode
{
    public override TokenKind Kind => TokenKind.TParentheses;
    public override string ToString() => $"({Expression})";

    public ExpressionNode Expression{ get; }
    public ParenthesesExpression(ExpressionNode expr)
    {
        Expression = expr;
    }

    
}