namespace WarmLangLexerParser.AST.TypeSyntax;

public abstract class TypeSyntaxNode : ASTNode
{
    public abstract override string ToString();

    public TypeSyntaxNode(TextLocation location)
    :base(location) { }
}