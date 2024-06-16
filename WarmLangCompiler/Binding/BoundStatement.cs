using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public abstract class BoundStatement
{
    public BoundStatement(StatementNode node)
    {
        Node = node;
    }

    public StatementNode Node { get; }
    public override string ToString() => Node.ToString();
}