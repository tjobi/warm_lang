namespace WarmLangLexerParser.AST;

public abstract class ExpressionNode : ASTNode
{
    protected ExpressionNode(TextLocation location) : base(location) { }
}
