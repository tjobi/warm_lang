namespace WarmLangCompiler.Symbols;

public sealed class VariabelSymbol : Symbol
{
    public VariabelSymbol(string name, TypeSymbol type) 
        : base(name)
    {
        Type = type;
    }

    public TypeSymbol Type { get; }
}