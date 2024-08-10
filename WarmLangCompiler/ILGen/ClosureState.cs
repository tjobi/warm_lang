using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.ILGen;

public sealed class ClosureState 
{
    public ClosureState(TypeDefinition closureType, VariableDefinition closureVariable)
    {
        TypeDef = closureType;
        VariableDef = closureVariable;
        ReferenceType = TypeDef.MakeByReferenceType();
    }

    public TypeDefinition TypeDef { get; }

    public VariableDefinition VariableDef { get; }

    public ByReferenceType ReferenceType { get; } 

    public void AddField(FieldDefinition @field) 
    {
        TypeDef?.Fields.Add(@field);
    }

    public bool TryGetField(VariableSymbol symbol, [NotNullWhen(true)] out FieldDefinition? @field)
    {
        @field = GetField(symbol);
        return @field is not null;
    }

    public FieldDefinition? GetField(VariableSymbol symbol) => GetField(symbol.Name);
    public FieldDefinition? GetField(string name) 
    {
        if(TypeDef is null)
            return null;
        foreach(var @field in TypeDef.Fields)
        {
            if(@field.Name == name)
                return @field;
        }
        return null;
    }

    public FieldDefinition GetFieldOrThrow(VariableSymbol symbol) => GetField(symbol.Name) ?? throw new Exception($"{nameof(ClosureState)} couldn't find '{symbol}'");

    public ParameterDefinition FindMatchingClosureParameter(FunctionSymbol funcSymbol, MethodDefinition func)
    {
        var parameters = func.Parameters;
        for(int i = funcSymbol.Parameters.Length; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if(parameter.ParameterType == ReferenceType || parameter.ParameterType == TypeDef)
                return parameter;
        }
        throw new Exception($"Couldn't find closure '{TypeDef}' in parameters of '{funcSymbol}'");
    }

}