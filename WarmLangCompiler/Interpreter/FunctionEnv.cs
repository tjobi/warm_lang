using System.Collections.Immutable;
using WarmLangCompiler.Binding;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Interpreter;

public sealed class FunctionEnv : IEnv<FunctionSymbol, BoundBlockStatement>
{
    private readonly IList<IDictionary<FunctionSymbol, BoundBlockStatement>> environment;

    public FunctionEnv()
    {
        environment = new List<IDictionary<FunctionSymbol, BoundBlockStatement>>
        {
            new Dictionary<FunctionSymbol, BoundBlockStatement>()
        };
    }

    public FunctionEnv(IImmutableDictionary<FunctionSymbol, BoundBlockStatement> initial)
    :this()
    {
        if(environment.Count == 0)
            environment.Add(new Dictionary<FunctionSymbol, BoundBlockStatement>());
        var globalScope = environment[0];
        foreach(var funcAndBody in initial)
        {
            globalScope.Add(funcAndBody);
        }
    }

    public (BoundBlockStatement, IEnv<FunctionSymbol, BoundBlockStatement>) Declare(FunctionSymbol name, BoundBlockStatement body)
    {
        var latestFuncScope = environment[^1];
        if(latestFuncScope.ContainsKey(name))
            throw new Exception("BoundFuncEnv -> Function is already defined");
        
        latestFuncScope.Add(name, body);
        return (body, this);
    }

    public BoundBlockStatement? Lookup(FunctionSymbol name)
    {
        for (int i = environment.Count - 1; i >= 0 ; i--)
        {
            var scope = environment[i];
            if(scope.TryGetValue(name, out var res))
            {
                return res;
            }
        }
        return null;
    }

    public IEnv<FunctionSymbol, BoundBlockStatement> Pop()
    {
        if(environment.Count > 0)
            environment.RemoveAt(environment.Count-1);
        return this;
    }

    public IEnv<FunctionSymbol, BoundBlockStatement> Push()
    {
        environment.Add(new Dictionary<FunctionSymbol, BoundBlockStatement>());
        return this;
    }
}