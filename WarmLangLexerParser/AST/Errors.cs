namespace WarmLangLexerParser.AST;

public sealed class ErrorExpressionNode : ExpressionNode
{
    public ErrorExpressionNode(SyntaxToken token) :base(token.Location) { }

    public ErrorExpressionNode(TextLocation loc) :base(loc) { }

    public override string ToString() => $"ParseErr({Location.StartLine},{Location.StartColumn})";
}
