namespace WarmLangCompiler.Symbols;

public class GlobalVariableSymbol : VariableSymbol
{
    public GlobalVariableSymbol(string name, TypeSymbol type) : base(name, type) { }

    public override string ToString() => $"Global:{base.ToString()}";
}