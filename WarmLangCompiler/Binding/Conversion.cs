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
        
        if(from.ResolveNestedType() == EmptyList && to is ListTypeSymbol)
        {
            return Implicit;
        }

        if(from == Int && to == Bool)
            return Explicit;
        if(from == Bool && to == Int)
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