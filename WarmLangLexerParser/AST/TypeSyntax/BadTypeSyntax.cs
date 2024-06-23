namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class BadTypeSyntax : ATypeSyntax
{
    public BadTypeSyntax(TextLocation location) : base(location) { }

    public override string ToString() => "BadTyp";
}