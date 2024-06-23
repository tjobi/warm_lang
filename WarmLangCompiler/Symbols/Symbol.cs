namespace WarmLangCompiler.Symbols;

public abstract class Symbol
{
    public Symbol(string name)
    {
        Name = name;
    }

    public string Name { get; }
}