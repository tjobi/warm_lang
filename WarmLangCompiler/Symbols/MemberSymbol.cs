namespace WarmLangCompiler.Symbols;

public abstract class MemberSymbol
{
    protected MemberSymbol(TypeSymbol type, string name)
    {
        Type = type;
        Name = name;
    }

    public TypeSymbol Type { get; }
    public string Name { get; }
}

public sealed class MemberFieldSymbol : MemberSymbol
{
    public MemberFieldSymbol(string name, TypeSymbol type) : base(type, name) { }
}

public sealed class MemberFuncSymbol : MemberSymbol
{
    public MemberFuncSymbol(FunctionSymbol function) : base(function.Type, function.Name)
    {
        Function = function;
    }
    
    public FunctionSymbol Function { get; }
}