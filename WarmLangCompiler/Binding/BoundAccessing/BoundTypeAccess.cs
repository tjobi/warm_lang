using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public class BoundTypeAccess : BoundAccess
{
    public BoundTypeAccess(TypeSymbol type) : base(type) { }

    public override string ToString() => $"(Static access {Type})";
}