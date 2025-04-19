using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Symbols;

public sealed class ListTypeSymbol : TypeSymbol
{

    public ListTypeSymbol(string name, TypeSymbol innerType) : base(name)
    {
        InnerType = innerType;
    }

    public ListTypeSymbol(TypeSymbol innerType) : this(string.Intern($"list<{innerType}>"), innerType) { }

    public TypeSymbol InnerType { get; private set;}

    public static TypeSymbol BasicList => List;

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
            return lts.InnerType == InnerType;
        return false;
    }

    public override int GetHashCode()
    {
        int hashCode = Name.GetHashCode();
        hashCode = (hashCode * 397) ^ InnerType.GetHashCode();
        return hashCode; 
    }

    // public override TypeSymbol Resolve()
    // {
    //     InnerType = InnerType.Resolve();
    //     return this;
    // }
}

public sealed class PlaceholderTypeSymbol : TypeSymbol
{
    private static int COUNT = 0;
    public TypeSymbol? ActualType { get; private set;}
    public int Depth { get; }

    public PlaceholderTypeSymbol(int depth) : base($"unknown{++COUNT}-{depth}")
    {
        Depth = depth;
    }

    public void Union(TypeSymbol a) => ActualType = a;

    private bool Wins() => ActualType is not null && ActualType is PlaceholderTypeSymbol pt && pt.Depth <= Depth;
    // public override TypeSymbol Resolve()
    // {
    //     if(ActualType is null || Wins()) return this;
    //     return ActualType.Resolve();
    // }

    public override string ToString()
    {
        if(ActualType is null || Wins()) return base.ToString();
        return $"Wrapped({ActualType})";
    }
}