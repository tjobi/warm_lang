namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundSubscriptAccess : BoundTargetedAccess
{
    public BoundSubscriptAccess(BoundAccess target, BoundExpression index) : base(target, target.Type.NestedTypeOrThis())
    {
        Index = index;
    }
    public BoundExpression Index { get; }

    public override string ToString() => $"(SubscriptAccess {Target}[{Index}])";
}
