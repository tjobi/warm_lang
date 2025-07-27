using System.Diagnostics.CodeAnalysis;
using System.Text;
using WarmLangCompiler.Symbols;
using static WarmLangCompiler.Symbols.TypeSymbol;

namespace WarmLangCompiler.Binding;

public sealed class Conversion
{
    private static readonly Conversion None = new(false, false, false);
    private static readonly Conversion Implicit = new (true, false, false);
    private static readonly Conversion Explicit = new (true, true, false);

    private static readonly Conversion ExplicitWithBoxInt = new (true, true, false, Int);
    private static readonly Conversion ExplicitWithBoxBool = new (true, true, false, Bool);
    
    private static readonly Conversion Identity = new(true, false, true);

    private static readonly TypeSymbol WLString = TypeSymbol.String; 

    private Conversion(bool exists, bool isExplicit, bool isIdentity, TypeSymbol? boxType = null)
    {
        Exists = exists;
        IsExplicit = isExplicit;
        IsIdentity = isIdentity;
        BoxType = boxType;
    }

    public bool Exists { get; }
    public bool IsExplicit { get; }
    public bool IsImplicit => Exists && !IsExplicit;
    public bool IsIdentity { get; }

    [MemberNotNullWhen(true, nameof(BoxType))]
    public bool RequiresBoxing => BoxType is not null;
    public TypeSymbol? BoxType { get; }

    public bool IsNone() => this == None;

    public static Conversion GetConversion(TypeSymbol from, TypeSymbol to, Func<TypeSymbol, TypeSymbol, bool> eql)
    {
        //Any CLI reference type is nullable - for now
        if(from == Null && !to.IsValueType) return Implicit;

        if (from == to || eql(from, to))
            return Identity;
        
        if(eql(Int, from))
        {
            if(eql(WLString, to))  return ExplicitWithBoxInt;
            if(to == Bool)         return Explicit;
        }
        
        if(eql(Bool, from))
        {
            if(to == Int)       return Explicit;
            if(eql(WLString, to))  return ExplicitWithBoxBool;
        }

        if(from == WLString)
        {
            if(to == Int)
                return Explicit;
        }

        //Everything is explicitly convertible to a string - Could even remove the checks for Int and Bool above 
        if(from != WLString && from != Error && from != TypeSymbol.Void && to == WLString)
            return Explicit;

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