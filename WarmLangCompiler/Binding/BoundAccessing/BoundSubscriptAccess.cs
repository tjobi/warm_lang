using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundSubscriptAccess : BoundTargetedAccess
{
    public BoundSubscriptAccess(BoundAccess target, BoundExpression index, TypeSymbol resultType) : base(target, resultType)
    {
        Index = index;
    }
    public BoundExpression Index { get; }

    public override string ToString() => $"(SubscriptAccess {Target}[{Index}])";
}
