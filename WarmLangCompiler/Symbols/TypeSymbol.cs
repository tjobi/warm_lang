namespace WarmLangCompiler.Symbols;

public sealed class TypeSymbol : Symbol
{
    public static TypeSymbol Int => new("int");
    public static TypeSymbol List => new("list"); //?
    public static TypeSymbol Error => new("err");

    public TypeSymbol(string name) : base(name)
    {
        
    }
}