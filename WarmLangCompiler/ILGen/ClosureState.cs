using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.ILGen;

public sealed class ClosureState 
{
    public ClosureState(FunctionSymbol func, TypeDefinition closureType, MethodDefinition ctor, MethodDefinition funcDef, VariableDefinition closureVariable)
    {
        Func = func;
        TypeDef = closureType;
        Constructor = ctor;
        FuncDef = funcDef;
        VariableDef = closureVariable;
        ReferenceType = TypeDef.MakeByReferenceType();

        closureType.Methods.Add(ctor);
        closureType.Methods.Add(funcDef);
    }

    public FunctionSymbol Func { get; }
    public TypeDefinition TypeDef { get; }
    public MethodDefinition Constructor { get; }
    public MethodDefinition FuncDef { get; }
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

    public bool HasField(VariableSymbol symbol) => GetField(symbol) is not null;

    public FieldDefinition GetFieldOrThrow(VariableSymbol symbol) => GetField(symbol.Name) ?? throw new Exception($"{nameof(ClosureState)} of '{TypeDef.FullName}' couldn't find '{symbol}'");

}