namespace WarmLangCompiler.Symbols;

using System.Collections.Immutable;
using System.Text;
using WarmLangLexerParser;

public static class BuiltInFunctions
{
    private static readonly TextLocation BUILT_IN_LOCATION = new(0, 0);
    private static FunctionSymbol MakeFunction(string name, TypeSymbol returnType, ImmutableArray<ParameterSymbol> parameters)
    {
        var funcType = new TypeSymbol("BUILTIN:" + name);
        return new(name, ImmutableArray<TypeSymbol>.Empty, parameters, funcType, returnType, BUILT_IN_LOCATION);
    } 

    public static readonly FunctionSymbol StdWrite = MakeFunction("stdWrite", TypeSymbol.Void, ImmutableArray.Create(new ParameterSymbol("toPrint", TypeSymbol.String, 0)));
    public static readonly FunctionSymbol StdWriteC = MakeFunction("stdWritec", TypeSymbol.Void, ImmutableArray.Create(new ParameterSymbol("toPrint", TypeSymbol.Int, 0)));
    public static readonly FunctionSymbol StdWriteLine = MakeFunction("stdWriteLine", TypeSymbol.Void, ImmutableArray.Create(new ParameterSymbol("toPrint", TypeSymbol.String, 0)));
    public static readonly FunctionSymbol StdRead = MakeFunction("stdRead", TypeSymbol.String, ImmutableArray<ParameterSymbol>.Empty);
    public static readonly FunctionSymbol StdClear = MakeFunction("stdClear", TypeSymbol.Void, ImmutableArray<ParameterSymbol>.Empty);

    public static readonly FunctionSymbol StrLen = MakeFunction("strLen", TypeSymbol.Int, ImmutableArray.Create(new ParameterSymbol("s", TypeSymbol.String, 0)));

    public static IEnumerable<FunctionSymbol> GetBuiltInFunctions()
    {
        yield return StdWrite;
        yield return StdWriteLine;
        yield return StdWriteC;
        yield return StdRead;
        yield return StrLen;
        yield return StdClear;
    }
    public static bool IsBuiltInFunction(this FunctionSymbol function)
    {
        foreach (var func in GetBuiltInFunctions())
            if (func == function)
                return true;
        return false;
    } 
}