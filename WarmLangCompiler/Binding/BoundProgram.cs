using System.Collections.Immutable;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class BoundProgram
{
    public BoundProgram(BoundBlockStatement statement, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions)
    {
        Statement = statement;
        Functions = functions;
    }
    public BoundBlockStatement Statement { get; }
    public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }

    public override string ToString() => $"Bound program for:\n  {Statement}";
}
