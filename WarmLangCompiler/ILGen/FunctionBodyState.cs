using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
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

    public Dictionary<string, (VariableDefinition? variable, ParameterDefinition? parameter)>? AvailableClosures { get; private set;}

    public FunctionBodyState(FunctionSymbol func)
    {
        Labels = new();
        Locals = new();
        AwaitingLabels = new();
        SharedLocals = new();
        Func = func;
    }

    public void AddClosureVariable(ClosureState closure)
    {
        if(Closure is not null) throw new Exception($"'{nameof(AddClosureVariable)}' called twice - Can only set closuretype of a body once!");
        Closure = closure;
        AvailableClosures ??= new();
        AvailableClosures[closure.ReferenceType.FullName] = (closure.VariableDef, null);
    }

    public void AddAvailableClosure(TypeDefinition closureType, VariableDefinition variable) => AddAvailableClosure(closureType, (variable, null));
    public void AddAvailableClosure(TypeDefinition closureType, ParameterDefinition parameter) => AddAvailableClosure(closureType, (null, parameter));
    public void AddAvailableClosure(TypeReference closureType, ParameterDefinition parameter) => AddAvailableClosure(closureType, (null, parameter));

    public void AddAvailableClosure(ClosureState closureType, ParameterDefinition parameter) => AddAvailableClosure(closureType.ReferenceType, parameter);
    private void AddAvailableClosure(TypeReference closureType, (VariableDefinition?, ParameterDefinition?) elm)
    {
        EnsureCreated();
        if(!AvailableClosures.ContainsKey(closureType.FullName))
            AvailableClosures[closureType.FullName] = elm;
    }

    public bool TryGetAvailableClosure(TypeReference closureType, out (VariableDefinition? variable, ParameterDefinition? parameter) closure)
    {
        EnsureCreated();
        return AvailableClosures.TryGetValue(closureType.FullName, out closure);
    }

    public bool TryGetAvailableClosure(ClosureState closureState, out (VariableDefinition? variable, ParameterDefinition? parameter) closure)
    {
        return TryGetAvailableClosure(closureState.ReferenceType, out closure);
    }

    [MemberNotNull(nameof(AvailableClosures))]
    private void EnsureCreated()
    {
        AvailableClosures ??= new();
    }

    public void AddClosureField(FieldDefinition @field) => Closure?.AddField(@field);

    public FieldDefinition GetClosureFieldOrthrow(VariableSymbol symbol) => Closure!.GetFieldOrThrow(symbol);

    public VariableDefinition ClosureVariable => Closure!.VariableDef;
}