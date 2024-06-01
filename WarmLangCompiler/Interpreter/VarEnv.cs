namespace WarmLangCompiler.Interpreter;
using System.Collections.Immutable;
using WarmLangCompiler.Interpreter.Values;
public sealed class VarEnv : IAssignableEnv<Value>
{
    private readonly List<ImmutableDictionary<string,Value>> env;
    public VarEnv()
    {
        env = new List<ImmutableDictionary<string, Value>>()
        {
            ImmutableDictionary<string, Value>.Empty
        };
    }
    public (Value, IEnv<Value>) Declare(string name, Value value)
    {
        var mostRecentScope = env.Last();
        try
        {
            var newEnv = mostRecentScope.Add(name, value);
            env[^1] = newEnv;
            return (value, this);
        } catch
        {
            throw new Exception("Variable is already defined");
        }
    }

    public (Value, IAssignableEnv<Value>) Assign(string name, Value value)
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
        throw new Exception($"Name {name} does not exist");
    }

    public Value Lookup(string name)
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

    public IEnv<Value> Pop()
    {
        env.RemoveAt(env.Count-1);
        return this;
    }

    public IEnv<Value> Push()
    {
        env.Add(ImmutableDictionary<string, Value>.Empty);
        return this;
    }
}