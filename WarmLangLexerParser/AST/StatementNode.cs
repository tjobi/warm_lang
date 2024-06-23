namespace WarmLangLexerParser.AST;

public abstract class StatementNode : ASTNode
{
    protected StatementNode(TextLocation location) : base(location){ }
}