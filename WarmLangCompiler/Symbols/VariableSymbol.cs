namespace WarmLangCompiler.Symbols;

public class VariableSymbol : Symbol
{
    public VariableSymbol(string name, TypeSymbol type) 
        : base(name)
    {
        Type = type;
    }

    public TypeSymbol Type { get; }
}