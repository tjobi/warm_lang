using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundCallExpression : BoundExpression
{
    public BoundCallExpression(ExpressionNode node, FunctionSymbol func, ImmutableArray<BoundExpression> arguments)
    : base(node, func.Type)
    {
        Function = func;
        Arguments = arguments;
    }

    public FunctionSymbol Function { get; }
    public ImmutableArray<BoundExpression> Arguments { get; }

    public override string ToString() => $"(Call {Function}({string.Join(",", Arguments)}))";
}