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

    public TypeDefinition? ClosureType { get; private set; }

    public VariableDefinition? ClosureVariable { get; private set; }

    public FunctionBodyState(FunctionSymbol func)
    {
        Labels = new();
        Locals = new();
        AwaitingLabels = new();
        SharedLocals = new();
        Func = func;
    }

    public void AddClosure(TypeDefinition closureType, VariableDefinition closureVariable)
    {
        if(ClosureType is null)
        {
            ClosureType = closureType;
            ClosureVariable = closureVariable;
            return;
        }
        throw new Exception($"'{nameof(AddClosure)}' called twice - Can only set closuretype of a body once!");
    }

    public void AddClosureField(FieldDefinition @field) 
    {
        ClosureType?.Fields.Add(@field);
    }

    public bool TryGetClosureField(VariableSymbol symbol, [NotNullWhen(true)] out FieldDefinition? @field)
    {
        @field = GetClosureField(symbol);
        return @field is not null;
    }

    public FieldDefinition? GetClosureField(VariableSymbol symbol) => GetClosureField(symbol.Name);
    public FieldDefinition? GetClosureField(string name) 
    {
        if(ClosureType is null)
            return null;
        foreach(var @field in ClosureType.Fields)
        {
            if(@field.Name == name)
                return @field;
        }
        return null;
    }
}