namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class BadTypeSyntax : TypeSyntaxNode
{
    public BadTypeSyntax(TextLocation location) : base(location) { }

    public override string ToString() => "BadTyp";
}