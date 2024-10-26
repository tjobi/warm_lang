using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundNameAccess : BoundAccess
{
    public BoundNameAccess(VariableSymbol symbol) : base(symbol.Type)
    {
        Symbol = symbol;
    }

    public VariableSymbol Symbol { get; }

    public override string ToString() => $"(NameAccess {Symbol})";
}
