namespace WarmLangLexerParser.AST;

public abstract class Access : ASTNode
{
    protected Access(TextLocation location) : base(location) { }

    public abstract override string ToString();
}