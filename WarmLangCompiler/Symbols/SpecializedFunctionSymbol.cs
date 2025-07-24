using System.Collections.Immutable;
using System.Text;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public sealed class SpecializedFunctionSymbol : FunctionSymbol
{
    public List<TypeSymbol> TypeArguments { get; }
    public FunctionSymbol SpecializedFrom { get; }

    private static string FuncName(FunctionSymbol func, List<TypeSymbol> typeParams)
    {
        var sb = new StringBuilder(func.Name);
        sb.Append('<');
        sb.AppendJoin(", ", typeParams);
        sb.Append('>');
        return sb.ToString();
    }
    public SpecializedFunctionSymbol(FunctionSymbol func,
                                     List<TypeSymbol> typeArguments, ImmutableArray<ParameterSymbol> parameters,
                                     TypeSymbol funcType, TypeSymbol returnType,
                                     TextLocation location)
    : base(FuncName(func, typeArguments), func.TypeParameters,
           parameters, funcType, returnType, location)
    {
        TypeArguments = typeArguments;
        SpecializedFrom = func;
    }

    public override string ToString()
    {
        return Name;
    }
}