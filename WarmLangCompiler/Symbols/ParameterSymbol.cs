namespace WarmLangCompiler.Symbols;

public class ParameterSymbol : VariableSymbol
{
    public ParameterSymbol(string name, TypeSymbol type) : base(name, type) { }

    public override string ToString() => $"{Type} {Name}";
}