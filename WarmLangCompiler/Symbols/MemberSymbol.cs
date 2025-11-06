namespace WarmLangCompiler.Symbols;

public abstract class MemberSymbol : EntitySymbol
{
    protected MemberSymbol(TypeSymbol type, string name, bool isReadOnly = false, bool isBuiltin = false)
    : base(name, type)
    {
        IsReadOnly = isReadOnly;
        IsBuiltin = isBuiltin;
    }

    public bool IsReadOnly { get; }
    public bool IsBuiltin { get; }
}

public sealed class ErrorMemberSymbol : MemberSymbol
{
    public static readonly ErrorMemberSymbol Instance = new();
    private ErrorMemberSymbol() : base(TypeSymbol.Error, "error", true) { }

    public override string ToString() => "memberError";
}

public sealed class MemberFieldSymbol : MemberSymbol
{
    public MemberFieldSymbol(string name, TypeSymbol type, bool isReadOnly = false, bool isBuiltin = false)
    : base(type, name, isReadOnly, isBuiltin) { }

    public override string ToString() => $"field({Type} {Name})";
}

public sealed class MemberFuncSymbol : MemberSymbol
{
    public MemberFuncSymbol(FunctionSymbol function)
    : base(function.Type, function.Name, isReadOnly:true, function.IsBuiltInFunction())
    {
        Function = function;
    }

    public override string ToString() => $"memberFunc({Function})";
    
    public FunctionSymbol Function { get; }
}