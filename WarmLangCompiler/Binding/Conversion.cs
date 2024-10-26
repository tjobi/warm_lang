using System.Text;
using WarmLangCompiler.Symbols;
using static WarmLangCompiler.Symbols.TypeSymbol;

namespace WarmLangCompiler.Binding;

public sealed class Conversion
{
    public static readonly Conversion None = new(false, false, false);
    public static readonly Conversion Implicit = new (true, false, false);
    public static readonly Conversion Explicit = new (true, true, false);
    public static readonly Conversion Identity = new(true, false, true);

    public static readonly TypeSymbol WLString = TypeSymbol.String; 

    public Conversion(bool exists, bool isExplicit, bool isIdentity)
    {
        Exists = exists;
        IsExplicit = isExplicit;
        IsIdentity = isIdentity;
    }

    public bool Exists { get; }
    public bool IsExplicit { get; }
    public bool IsImplicit => Exists && !IsExplicit;
    public bool IsIdentity { get; }

    public bool IsNone() => this == None;

    public static Conversion GetConversion(TypeSymbol from, TypeSymbol to)
    {
        if(from == to)
            return Identity;
        
        if(from == Int)
            if(to == WLString || to == Bool)
                return Explicit;
        
        if(from == Bool)
            if(to == WLString || to == Int)
                return Explicit;

        if(from == WLString)
        {
            if(to == Int)
                return Explicit;
        }

        if(from.ResolveDeelpyNestedType() == EmptyList && to is ListTypeSymbol)
        {
            return Implicit;
        }

        //Everything is explicitly convertible to a string - Could even remove the checks for Int and Bool above 
        if(from != WLString && to == WLString)
            return Explicit;

        //There is no conversion
        return None;
    }

    public override string ToString()
    {
        var sb = new StringBuilder().Append("Conv(");

        if(Exists)
        {
            sb.Append("Exists,");
            sb.Append(IsExplicit ? "Explicit" : "Implicit");
            if(IsIdentity)
            {
                sb.Append(',');
                sb.Append("Identity");
            }
        }
        else
        {
            sb.Append("None");
        }
        return sb.Append(')').ToString();
    }
}