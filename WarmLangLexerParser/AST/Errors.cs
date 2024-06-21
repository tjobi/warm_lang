namespace WarmLangLexerParser.AST;

public sealed class ErrorExpressionNode : ExpressionNode
{
    public override TokenKind Kind => TokenKind.TBadToken;

    public int Line { get; }
    public int Column { get; set; }
    public ErrorExpressionNode(int line, int col)
    {
        Line = line;
        Column = col;
    }

    public ErrorExpressionNode(SyntaxToken token) :this(token.Location) { }

    public ErrorExpressionNode(TextLocation loc) :this(loc.StartLine, loc.StartColumn) { }

    public override string ToString() => $"ParseErr({Line},{Column})";
}
