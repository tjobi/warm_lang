namespace WarmLangCompiler.Symbols;

public class TypeSymbol : Symbol
{
    //FIXME: What if multithreading?
    private static int NEXT_ID = 0;

    public static readonly TypeSymbol Error = new("err");
    public static readonly TypeSymbol Null = new("null");
    public static readonly TypeSymbol Void = new("void");

    public static readonly TypeSymbol Int = new("int", isValueType:true);
    public static readonly TypeSymbol Bool = new("bool", isValueType:true);
    public static readonly TypeSymbol String = new("string");
    public static readonly TypeSymbol List = new("List`");
    public static readonly TypeSymbol Func = new("Func");

    public bool IsValueType { get; }
    public int ID { get; }

    public TypeSymbol(string name, bool isValueType = false) 
    : base(name)
    {
        ID = NEXT_ID++;
        IsValueType = isValueType;
    }

    public override string ToString() => Name;

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
            return ID == ts.ID;
        return false;
    }

    public override int GetHashCode()
    {
        // int hashCode = Name.GetHashCode();
        int hashCode = ID.GetHashCode();
        return hashCode;
    }
}