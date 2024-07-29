namespace WarmLangCompiler.Symbols;

public sealed class ListTypeSymbol : TypeSymbol
{
    public ListTypeSymbol(string name, TypeSymbol innerType) : base(name)
    {
        InnerType = innerType;
    }

    public ListTypeSymbol(TypeSymbol innerType) : this(string.Intern($"list<{innerType}>"), innerType) { }

    public TypeSymbol InnerType { get; }

    public static TypeSymbol BasicList => ListBase;

    public static bool operator == (ListTypeSymbol a, ListTypeSymbol b)
    {
        if (ReferenceEquals(a, b)) 
            return true;
        if (a is null) 
            return false;
        if (b is null)
            return false;
        return a.Equals(b);
    }

    public static bool operator != (ListTypeSymbol a, ListTypeSymbol b)
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
        if(obj is ListTypeSymbol lts)
            return Name == lts.Name && lts.InnerType == InnerType;
        return false;
    }

    public override int GetHashCode()
    {
        int hashCode = Name.GetHashCode();
        hashCode = (hashCode * 397) ^ InnerType.GetHashCode();
        return hashCode; 
    }
}