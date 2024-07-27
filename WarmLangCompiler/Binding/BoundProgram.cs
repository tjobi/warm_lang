using System.Collections.Immutable;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class BoundProgram
{
    public BoundProgram(FunctionSymbol entrypoint, BoundBlockStatement statement, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions)
    {
        EntryPoint = entrypoint;
        Statement = statement;
        Functions = functions;
    }

    public FunctionSymbol EntryPoint { get; }
    public BoundBlockStatement Statement { get; }
    public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }

    public override string ToString() => $"Bound program for:\n  {EntryPoint}";
}
