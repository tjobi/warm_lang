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
    
    public string NodeString() => Node.ToString();

    public abstract override string ToString();
}