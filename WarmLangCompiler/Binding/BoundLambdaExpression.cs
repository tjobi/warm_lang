using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundLambdaExpression : BoundExpression
{
    public BoundLambdaExpression(ExpressionNode node, FunctionSymbol symbol, BoundBlockStatement body)
    : base(node, symbol.Type)
    {
        Symbol = symbol;
        Body = body;
    }

    public FunctionSymbol Symbol { get; }
    public BoundBlockStatement Body { get; }
}
