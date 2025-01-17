namespace WarmLangCompiler.Symbols;

public class TypeSymbol : Symbol
{
    public static readonly TypeSymbol Int = new("int", isValueType:true);
    public static readonly TypeSymbol Bool = new("bool", isValueType:true);
    public static readonly TypeSymbol String = new("string");
    public static readonly TypeSymbol Void = new("void");
    public static readonly TypeSymbol Null = new("null");
    public static readonly TypeSymbol IntList = new ListTypeSymbol("list<int>", Int); //TODO: how to generic?
    public static readonly TypeSymbol Error = new("err");

    public static readonly TypeSymbol ListBase = new("only-for-use-by-compiler");

    public bool IsValueType { get; }

    public TypeSymbol(string name, bool isValueType = false) 
    : base(name)
    {
        IsValueType = isValueType;
    }

    public override string ToString() => Name;

    public TypeSymbol NestedTypeOrThis()
    {
        if(this is ListTypeSymbol lts)
            return lts.InnerType;
        if(this == String)
            return Int;
        return this;
    }

    //In case of any placeholder types - this method removes those
    public virtual TypeSymbol Resolve() => this;

    public static bool operator ==(TypeSymbol a, TypeSymbol b)
    {
        if(a is null || b is null)
            return false;
        a = a.Resolve();
        b = b.Resolve();
        return a.Equals(b);
    }

    public static bool operator !=(TypeSymbol a, TypeSymbol b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if(obj is TypeSymbol ts)
            return Name == ts.Name;
        return false;
    }

    public override int GetHashCode()
    {
        int hashCode = Resolve().Name.GetHashCode();
        return hashCode;
    }
}