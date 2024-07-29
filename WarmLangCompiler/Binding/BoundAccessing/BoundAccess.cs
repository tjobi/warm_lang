using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public abstract class BoundAccess 
{
    public BoundAccess(TypeSymbol type)
    {
        Type = type;
    }

    public TypeSymbol Type { get; }

    public virtual bool HasNested => false;
}

public sealed class BoundInvalidAccess : BoundAccess 
{
    public BoundInvalidAccess(): base(TypeSymbol.Error) { }
}
