using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundFuncAccess : BoundAccess
{
    public BoundFuncAccess(FunctionSymbol func) : base(func.Type)
    {
        Func = func;
    }

    public FunctionSymbol Func { get; }

    public override string ToString() => $"BoundFuncAccess: {Func}";
}