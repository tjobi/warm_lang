using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.ILGen;

public sealed class FunctionBodyState
{
    public Dictionary<BoundLabel, Instruction> Labels { get; }
    public Dictionary<VariableSymbol, VariableDefinition> Locals { get; }
    public Dictionary<VariableSymbol, FieldDefinition> SharedLocals { get; }
    public Dictionary<int, BoundLabel> AwaitingLabels { get; }
    public FunctionSymbol Func { get; }

    public ClosureState? Closure { get; private set; }

    public FunctionBodyState(FunctionSymbol func)
    {
        Labels = new();
        Locals = new();
        AwaitingLabels = new();
        SharedLocals = new();
        Func = func;
    }

    public void AddClosure(ClosureState closure)
    {
        if(Closure is not null) throw new Exception($"'{nameof(AddClosure)}' called twice - Can only set closuretype of a body once!");
        Closure = closure;        
    }

    public void AddClosureField(FieldDefinition @field) => Closure?.AddField(@field);

    public FieldDefinition GetClosureFieldOrthrow(VariableSymbol symbol) => Closure!.GetFieldOrThrow(symbol);

    public VariableDefinition ClosureVariable => Closure!.VariableDef;
}