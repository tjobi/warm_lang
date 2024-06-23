using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundErrorStatement : BoundStatement
{
    public BoundErrorStatement(StatementNode node)
    : base(node) { }
}