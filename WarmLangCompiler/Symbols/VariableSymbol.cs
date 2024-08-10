namespace WarmLangCompiler.Symbols;

public class VariableSymbol : EntitySymbol
{
    public VariableSymbol(string name, TypeSymbol type) 
    : base(name, type) { }

    public bool IsFree { get; set; } = false;
    public override string ToString() => $"{Type} {Name}";
}