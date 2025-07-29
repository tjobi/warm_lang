using System.Collections.Immutable;
using WarmLangCompiler.Binding.BoundAccessing;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

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