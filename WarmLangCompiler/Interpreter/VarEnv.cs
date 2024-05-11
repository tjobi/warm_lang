namespace WarmLangCompiler.Interpreter;
using System.Collections.Immutable;
public sealed class VarEnv : IAssignableEnv<int>
{
    private readonly List<ImmutableDictionary<string,int>> env;
    public VarEnv()
    {
        env = new List<ImmutableDictionary<string, int>>()
        {
            ImmutableDictionary<string, int>.Empty
        };
    }
    public (int, IEnv<int>) Declare(string name, int value)
    {
        var mostRecentScope = env.Last();
        try
        {
            var newEnv = mostRecentScope.Add(name, value);
            env[^1] = newEnv;
            return (value, this);
        } catch
        {
            throw new Exception("Failed: Variable is already defined");
        }
    }

    public (int, IAssignableEnv<int>) Assign(string name, int value)
    {
        for (int i = env.Count - 1; i >= 0 ; i--)
        {
            var scope = env[i];
            if(scope.ContainsKey(name))
            {
                env[i] = scope.SetItem(name, value);
                return (value, this);
            }
        }
        throw new Exception($"Failed: {name} does not exist");
    }

    public int Lookup(string name)
    {
        for (int i = env.Count - 1; i >= 0 ; i--)
        {
            var scope = env[i];
            if(scope.TryGetValue(name, out var res))
            {
                return res;
            }
        }
        throw new Exception($"Failed: Variable {name} has not been declared.");
    }

    public IEnv<int> Pop()
    {
        env.RemoveAt(env.Count-1);
        return this;
    }

    public IEnv<int> Push()
    {
        env.Add(ImmutableDictionary<string, int>.Empty);
        return this;
    }
}