namespace WarmLangCompiler.Symbols;

public class TypeSymbol : Symbol
{
    public static readonly TypeSymbol Int = new("int");
    public static readonly TypeSymbol Bool = new("bool");
    public static readonly TypeSymbol String = new("string");
    public static readonly TypeSymbol Void = new("void");
    public static readonly TypeSymbol EmptyList = new("unspecified empty list");
    public static readonly TypeSymbol IntList = new ListTypeSymbol("list<int>", Int); //TODO: how to generic?
    public static readonly TypeSymbol Error = new("err");

    public TypeSymbol(string name) : base(name) { }

    public override string ToString() => Name;


    public TypeSymbol ResolveDeelpyNestedType() => Resolver(this);

    private TypeSymbol Resolver(TypeSymbol t)
    {
        if(t is ListTypeSymbol lts)
        {
            return Resolver(lts.InnerType);
        }
        return t;
    }

    public TypeSymbol NestedTypeOrThis()
    {
        if(this is ListTypeSymbol lts)
            return lts.InnerType;
        if(this == String)
            return Int;
        return this;
    }

    public static bool operator ==(TypeSymbol a, TypeSymbol b)
    {
        if(a is null || b is null)
            return false;
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
        int hashCode = Name.GetHashCode();
        return hashCode;
    }
}