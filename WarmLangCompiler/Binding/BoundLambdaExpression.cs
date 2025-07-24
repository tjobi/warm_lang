using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundLambdaExpression : BoundExpression
{
    public BoundLambdaExpression(ExpressionNode node, LambdaFunctionSymbol symbol)
    : base(node, symbol.Type)
    {
        Symbol = symbol;
        if (!symbol.IsComplete) throw new Exception($"Compiler bug - {nameof(BoundLambdaExpression)} received symbol with no body, {node.Location}");
        Body = symbol.Body;
    }

    public LambdaFunctionSymbol Symbol { get; }
    public BoundBlockStatement Body { get; }
}
