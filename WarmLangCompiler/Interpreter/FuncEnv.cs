namespace WarmLangCompiler.Interpreter;

using System.Collections.Immutable;

public sealed class FuncEnv : IEnv<Funct>
{
    private readonly IList<ImmutableDictionary<string,Funct>> env;

    public FuncEnv()
    {
        env = new List<ImmutableDictionary<string,Funct>>(){
            ImmutableDictionary<string, Funct>.Empty
        };
    }
    public (Funct, IEnv<Funct>) Declare(string name, Funct value)
    {
        var latestFuncScope = env[^1];
        try
        {
            var newEnv = latestFuncScope.Add(name, value);
            env[^1] = newEnv;
            return (value, this);
        } catch
        {
            throw new Exception("Function is already defined");
        }
    }

    public Funct Lookup(string name)
    {
        for (int i = env.Count - 1; i >= 0 ; i--)
        {
            var scope = env[i];
            if(scope.TryGetValue(name, out var res))
            {
                return res;
            }
        }
        throw new Exception($"Variable {name} has not been declared.");
    }

    public IEnv<Funct> Pop()
    {
        env.RemoveAt(env.Count - 1);
        return this;
    }

    public IEnv<Funct> Push()
    {
        env.Add(ImmutableDictionary<string, Funct>.Empty);
        return this;
    }
}