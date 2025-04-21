namespace WarmLangLexerParser.AST;

public sealed class TopLevelError : TopLevelNode
{
    public TopLevelError(TextLocation location) : base(location) { }

    public override string ToString() => $"ParseError at {Location}";
}
