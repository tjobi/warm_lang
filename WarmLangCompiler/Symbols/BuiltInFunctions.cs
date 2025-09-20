namespace WarmLangCompiler.Symbols;

using static WarmLangCompiler.Symbols.FunctionFactory;
public static class BuiltInFunctions
{
    public static readonly FunctionSymbol StdWrite = CreateBuiltinFunction("stdWrite", TypeSymbol.Void, new ParameterSymbol("toPrint", TypeSymbol.String, 0));
    public static readonly FunctionSymbol StdWriteC = CreateBuiltinFunction("stdWritec", TypeSymbol.Void, new ParameterSymbol("toPrint", TypeSymbol.Int, 0));
    public static readonly FunctionSymbol StdWriteLine = CreateBuiltinFunction("stdWriteLine", TypeSymbol.Void, new ParameterSymbol("toPrint", TypeSymbol.String, 0));
    public static readonly FunctionSymbol StdRead = CreateBuiltinFunction("stdRead", TypeSymbol.String);
    public static readonly FunctionSymbol StdClear = CreateBuiltinFunction("stdClear", TypeSymbol.Void);

    public static readonly FunctionSymbol StrLen = CreateBuiltinFunction("strLen", TypeSymbol.Int, new ParameterSymbol("s", TypeSymbol.String, 0));

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