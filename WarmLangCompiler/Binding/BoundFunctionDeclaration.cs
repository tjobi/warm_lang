using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundFunctionDeclaration : BoundStatement
{
    public BoundFunctionDeclaration(StatementNode node, FunctionSymbol symbol) : base(node)
    {
        Symbol = symbol;
    }

    public FunctionSymbol Symbol { get; }
}