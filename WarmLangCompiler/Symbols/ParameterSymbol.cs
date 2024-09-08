namespace WarmLangCompiler.Symbols;

public class ParameterSymbol : ScopedVariableSymbol
{
    public ParameterSymbol(string name, TypeSymbol type, int placement)
    : base(name, type)
    {
        Placement = placement;
    }

    public int Placement { get; }

    public override string ToString() => $"{Type} {Name}";
}