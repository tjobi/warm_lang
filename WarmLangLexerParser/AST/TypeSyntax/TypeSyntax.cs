namespace WarmLangLexerParser.AST.TypeSyntax;

public abstract class ATypeSyntax : ASTNode
{
    public abstract override string ToString();

    public ATypeSyntax(TextLocation location)
    :base(location) { }
}