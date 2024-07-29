namespace WarmLangCompiler.Symbols;

public abstract class MemberSymbol
{
    protected MemberSymbol(TypeSymbol type, string name, bool isBuiltin = false)
    {
        Type = type;
        Name = name;
        IsBuiltin = isBuiltin;
    }

    public TypeSymbol Type { get; }
    public string Name { get; }
    public bool IsBuiltin { get; }
}

public sealed class MemberFieldSymbol : MemberSymbol
{
    public MemberFieldSymbol(string name, TypeSymbol type,  bool isBuiltin = false) : base(type, name, isBuiltin) { }
}

public sealed class MemberFuncSymbol : MemberSymbol
{
    public MemberFuncSymbol(FunctionSymbol function) : base(function.Type, function.Name, function.IsBuiltInFunction())
    {
        Function = function;
    }
    
    public FunctionSymbol Function { get; }
}