using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundFuncAccess : BoundAccess
{
    public BoundFuncAccess(FunctionSymbol func) : base(func.Type)
    {
        Func = func;
    }

    public BoundFuncAccess(BoundAccess target, FunctionSymbol func, TypeSymbol type) : base(type)
    {
        Func = func;
        Target = target;
    }

    public FunctionSymbol Func { get; }

    /// <value><c>Target</c> represent the target of a method reference</value>
    /// <remarks><c>obj</c> is the <c>Target</c> in <code>var f = obj.Method;</code></remarks>
    /// TODO: is really just an ugly closure ;(
    public BoundAccess? Target { get; } = null;

    public override string ToString() => $"Access {Func}";
}