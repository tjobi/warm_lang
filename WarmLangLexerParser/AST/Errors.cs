namespace WarmLangLexerParser.AST;

public sealed class ExpressionErrorNode : ExpressionNode
{
    public override TokenKind Kind => TokenKind.TBadToken;

    public int Line { get; }
    public int Column { get; set; }
    public ExpressionErrorNode(int line, int col)
    {
        Line = line;
        Column = col;
    }

    public override string ToString() => $"ParseErr({Line},{Column})";
}
