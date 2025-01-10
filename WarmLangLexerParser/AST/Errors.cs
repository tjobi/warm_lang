namespace WarmLangLexerParser.AST;

public sealed class ErrorExpression : ExpressionNode
{
    public ErrorExpression(SyntaxToken token) :base(token.Location) { }

    public ErrorExpression(TextLocation loc) :base(loc) { }

    public override string ToString() => $"ParseErr({Location.StartLine},{Location.StartColumn})";
}

public sealed class ErrorStatement : StatementNode
{
    public ErrorStatement(TextLocation location) : base(location) { }
    public ErrorStatement(SyntaxToken start, SyntaxToken end) : this(TextLocation.FromTo(start, end)) { }

    public override string ToString() => $"ParseStatementErr({Location})";
}
