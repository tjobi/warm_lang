using System.Collections.Immutable;
using WarmLangCompiler.Binding.BoundAccessing;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

// public sealed class BoundCallExpression : BoundExpression
// {
//     public BoundCallExpression(ExpressionNode node, FunctionSymbol func, BoundAccess target, ImmutableArray<BoundExpression> arguments)
//     : base(node, func.Type)
//     {
//         Function = func;
//         Target = target;
//         Arguments = arguments;
//     }

//     public FunctionSymbol Function { get; }
//     public BoundAccess Target { get; }
//     public ImmutableArray<BoundExpression> Arguments { get; }

//     public override string ToString() => $"(Call {Function}({string.Join(",", Arguments)}))";
// }

public sealed class BoundCallExpression : BoundExpression
{
    public BoundCallExpression(ExpressionNode node, BoundAccess target, ImmutableArray<BoundExpression> arguments,
                                TypeSymbol returnedType, ImmutableDictionary<TypeSymbol, TypeSymbol> appliedTypeArguments)
    : base(node, returnedType)
    {
        Target = target;
        Arguments = arguments;
        AppliedTypeArguments = appliedTypeArguments;
    }
    public BoundAccess Target { get; }
    public ImmutableArray<BoundExpression> Arguments { get; }
    public ImmutableDictionary<TypeSymbol, TypeSymbol> AppliedTypeArguments { get; }

    public override string ToString() => $"(Call '{Target}'({string.Join(",", Arguments)}))";
}