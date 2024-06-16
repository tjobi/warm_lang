namespace WarmLangCompiler.Symbols;

public sealed class TypeSymbol : Symbol
{
    public static readonly TypeSymbol Int = new("int");
    public static readonly TypeSymbol List = new("list"); //TODO: how to generic?
    public static readonly TypeSymbol Error = new("err");

    public static readonly TypeSymbol Bool = Int; //TODO: Add bools.

    public TypeSymbol(string name) : base(name)
    {
        
    }

    public override string ToString() => Name;
}