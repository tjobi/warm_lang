namespace WarmLangLexerParser.AST;
public abstract class ASTNode
{
    public abstract TokenKind Kind { get; }
}

public abstract class ExpressionNode : ASTNode { }