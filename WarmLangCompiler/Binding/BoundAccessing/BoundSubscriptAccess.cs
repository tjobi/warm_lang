namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundSubscriptAccess : BoundAccess
{
    public BoundSubscriptAccess(BoundAccess target, BoundExpression index) : base(target.Type.NestedTypeOrThis())
    {
        Target = target;
        Index = index;
    }

    public BoundAccess Target { get; }
    public BoundExpression Index { get; }
    public override bool HasNested => true;
}
