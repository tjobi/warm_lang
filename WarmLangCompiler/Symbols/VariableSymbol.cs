namespace WarmLangCompiler.Symbols;

public class VariableSymbol : EntitySymbol
{
    public VariableSymbol(string name, TypeSymbol type) 
        : base(name, type) { }

    public override string ToString() => $"{Type} {Name}";
}