namespace WarmLangLexerParser.AST;
public abstract class ASTNode
{
    public abstract override string ToString();

    public TextLocation Location { get; }

    protected ASTNode(TextLocation location)
    {
        Location = location;
    }

    protected ASTNode(TextLocation from, TextLocation to)
    :this(TextLocation.FromTo(from,to)) { }
}