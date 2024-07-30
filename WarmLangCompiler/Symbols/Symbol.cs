namespace WarmLangCompiler.Symbols;

public abstract class Symbol
{
    public Symbol(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public abstract class EntitySymbol : Symbol
{
    public EntitySymbol(string name, TypeSymbol type) : base(name)
    {
        Type = type;
    }

    public TypeSymbol Type { get; }

    public abstract override string ToString();
}