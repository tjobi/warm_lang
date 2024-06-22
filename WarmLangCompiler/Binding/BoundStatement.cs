using WarmLangLexerParser;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public abstract class BoundStatement
{
    public BoundStatement(StatementNode node)
    {
        Node = node;
    }

    public StatementNode Node { get; }

    public TextLocation Location => Node.Location;
    public override string ToString() => Node.ToString();
}