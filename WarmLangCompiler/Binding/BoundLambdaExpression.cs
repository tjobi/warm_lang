using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundLambdaExpression : BoundExpression
{
    public BoundLambdaExpression(ExpressionNode node, FunctionSymbol symbol)
    : base(node, symbol.Type)
    {
        Symbol = symbol;
    }

    public FunctionSymbol Symbol { get; }
}