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
            if(to == TypeSymbol.String || to == Bool)
                return Explicit;
        
        if(from == Bool)
            if(to == TypeSymbol.String || to == Int)
                return Explicit;

        if(from == TypeSymbol.String)
        {
            if(to == Int)
                return Explicit;
        }

        if(from is ListTypeSymbol && to == TypeSymbol.String)
            return Explicit;

        if(from.ResolveDeelpyNestedType() == EmptyList && to is ListTypeSymbol)
        {
            return Implicit;
        }

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