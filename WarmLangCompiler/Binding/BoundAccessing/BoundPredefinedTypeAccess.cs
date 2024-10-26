using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundPredefinedTypeAccess : BoundAccess
{
    public BoundPredefinedTypeAccess(TypeSymbol type) : base(type) { }

    public override string ToString() => $"(AccessPredef {Type})";
}