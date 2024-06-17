namespace WarmLangCompiler.Symbols;

public class TypeSymbol : Symbol
{
    public static readonly TypeSymbol Int = new("int");
    public static readonly TypeSymbol IntList = new ListTypeSymbol("list<int>", Int); //TODO: how to generic?
    public static readonly TypeSymbol EmptyList = new("list<empty>");
    public static readonly TypeSymbol Void = new("void");
    public static readonly TypeSymbol Error = new("err");

    public static readonly TypeSymbol Bool = Int; //TODO: Add bools.

    public TypeSymbol(string name) : base(name) { }

    public override string ToString() => Name;

    public static bool operator ==(TypeSymbol a, TypeSymbol b)
    {
        return a.Name == b.Name;
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