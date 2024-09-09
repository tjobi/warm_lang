using WarmLangCompiler.Symbols;
using System.Collections.Immutable;
namespace WarmLangCompiler.ILGen;
public static class EmitterTypeSymbolHelpers
{
    private static readonly TypeSymbol _cilBaseType = new("object");
    private static readonly TypeSymbol _closureType = new("wl-closure");
    private static readonly TypeSymbol _list = new("non-generic-list");
    private static readonly ImmutableDictionary<TypeSymbol, string> _toCIL = new (TypeSymbol, string)[]
    {
        (TypeSymbol.Int, "System.Int32"),       (TypeSymbol.Bool, "System.Boolean"), 
        (TypeSymbol.String, "System.String"),   (TypeSymbol.Void, "System.Void"), 
        (_cilBaseType, "System.Object"),        (_list, "System.Collections.ArrayList"),
        (_closureType, "System.ValueType")
    }.ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
    private static readonly ImmutableDictionary<string,TypeSymbol> _fromCIL = _toCIL.ToImmutableDictionary(entry => entry.Value, entry => entry.Key);

    public static TypeSymbol FromCilName(string name) => _fromCIL[name];
    public static IEnumerable<TypeSymbol> BuiltInTypes() => _toCIL.Select(t => t.Key);

    public static TypeSymbol CILBaseTypeSymbol => _cilBaseType;
    public static TypeSymbol CILClosureType => _closureType;

    public static string[] GetCilParamNames() => Array.Empty<string>();

    public static string[] GetCilParamNames(params TypeSymbol[] types)
    {
        var res = new string[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            res[i] = types[i].ToCilName();
        }
        return res;
    }

    public static string ToCilName(this TypeSymbol type) => _toCIL[type.AsRecognisedType()];

    public static TypeSymbol AsRecognisedType(this TypeSymbol type)
        => type is ListTypeSymbol || type == TypeSymbol.EmptyList ? _list : type;
    public static bool NeedsBoxing(this TypeSymbol type) => type == TypeSymbol.Bool || type == TypeSymbol.Int; 

}