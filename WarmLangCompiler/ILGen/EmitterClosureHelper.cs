using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.ILGen;

public static class EmitterClosureHelper
{
    public static ParameterDefinition FindOrCreateMatchingClosureParameter(this Dictionary<FunctionSymbol, MethodDefinition> funcs, FunctionSymbol f, ClosureState closure)
    {
        if(funcs.TryFindMatchingClosureParameter(f, closure, out var parameter)) return parameter;
        return funcs.CreateMatchingClosureParameter(f, closure);
    }

    public static ParameterDefinition CreateMatchingClosureParameter(this Dictionary<FunctionSymbol, MethodDefinition> funcs, FunctionSymbol f, ClosureState closure)
    {
        TypeReference closureType = closure.ReferenceType;
        var closureParam = new ParameterDefinition("closure", ParameterAttributes.None, closureType);
        funcs[f].Parameters.Add(closureParam);
        return closureParam;
    }

    public static bool TryFindMatchingClosureParameter(this Dictionary<FunctionSymbol, MethodDefinition> funcs, FunctionSymbol f, ClosureState closure, [NotNullWhen(true)] out ParameterDefinition? parameter)
    {
        var func = funcs[f];
        var parameters = func.Parameters;
        for(int i = f.Parameters.Length; i < parameters.Count; i++)
        {
            parameter = parameters[i];
            if(parameter.ParameterType == closure.ReferenceType || parameter.ParameterType == closure.TypeDef)
                return true;
        }
        parameter = null;
        return false;
    }

    public static bool HasMatchingClosureParameter(this Dictionary<FunctionSymbol, MethodDefinition> funcs, FunctionSymbol f, ClosureState closure) 
        => TryFindMatchingClosureParameter(funcs, f, closure, out var _);
    
    public static IEnumerable<TypeReference> GetAllClosureParameters(this Dictionary<FunctionSymbol, MethodDefinition> funcs, FunctionSymbol f) 
    {
        var methodDef = funcs[f];
        var parameters = methodDef.Parameters;
        if(parameters.Count >= f.Parameters.Length)
        {
            for(int i = f.Parameters.Length; i < parameters.Count; i++)
            {
                yield return parameters[i].ParameterType;
            }
        }
    }

    public static bool TryGetAvailableClosureLoadInstruction(this FunctionBodyState state, TypeReference closure, ILProcessor processor, [NotNullWhen(true)] out Instruction? instruction)
    {
        instruction = null;
        if(!state.TryGetAvailableClosure(closure, out var foundClosure))
            return false;
        var (variable, parameter) = foundClosure;
        if(variable is not null) instruction = processor.Create(OpCodes.Ldloca, variable);
        else                     instruction = processor.Create(OpCodes.Ldarg, parameter);
        return true;
    }

    public static bool TryGetAvailableClosureLoadInstruction(this FunctionBodyState state, ClosureState closure, ILProcessor processor, [NotNullWhen(true)] out Instruction? instruction)
    {
        return TryGetAvailableClosureLoadInstruction(state, closure.ReferenceType, processor, out instruction);
    }

    public static bool HasReachedOwnerOrIsAlreadyKnown(this FunctionBodyState body, ClosureState closure)
    => body.Func is not LocalFunctionSymbol || body == closure.BelongsTo || body.TryGetAvailableClosure(closure, out var _);
}