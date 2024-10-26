using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public abstract class BoundAccess 
{
    public BoundAccess(TypeSymbol type)
    {
        Type = type;
    }

    public TypeSymbol Type { get; }

    public abstract override string ToString();
}

public sealed class BoundInvalidAccess : BoundAccess 
{
    public BoundInvalidAccess(): base(TypeSymbol.Error) { }

    public override string ToString() => "InvalidAccess";
}

public abstract class BoundTargetedAccess : BoundAccess
{
    protected BoundTargetedAccess(BoundAccess target, TypeSymbol type) : base(type)
    {
        Target = target;
    }

    public BoundAccess Target { get; }
}