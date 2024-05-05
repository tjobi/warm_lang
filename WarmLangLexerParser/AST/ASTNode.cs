namespace WarmLangLexerParser.AST;
public abstract class ASTNode
{
    public abstract TokenKind Kind { get; }

    public abstract override string ToString();
}

public abstract class ExpressionNode : ASTNode { }

public abstract class StatementNode : ASTNode { }