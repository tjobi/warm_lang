using Mono.Cecil.Cil;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.ILGen;

public sealed class FunctionBodyState
{
    public Dictionary<BoundLabel, Instruction> Labels { get; }
    public Dictionary<VariableSymbol, VariableDefinition> Locals { get; }
    public Dictionary<int, BoundLabel> AwaitingLabels { get; }

    public FunctionBodyState()
    {
        Labels = new();
        Locals = new();
        AwaitingLabels = new();
    }
}