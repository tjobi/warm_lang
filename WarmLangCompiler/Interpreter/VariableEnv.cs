namespace WarmLangCompiler.Interpreter;
using System.Collections.Immutable;
using System.Text;
using WarmLangCompiler.Interpreter.Values;
public sealed class VariableEnv : IAssignableEnv<string,Value>
{
    private readonly List<ImmutableDictionary<string,Value>> env;
    public VariableEnv()
    {
        env = new List<ImmutableDictionary<string, Value>>()
        {
            ImmutableDictionary<string, Value>.Empty
        };
    }
    public (Value, IEnv<string, Value>) Declare(string name, Value value)
    {
        var mostRecentScope = env.Last();
        try
        {
            var newEnv = mostRecentScope.Add(name, value);
            env[^1] = newEnv;
            return (value, this);
        } catch
        {
            throw new Exception($"Variable '{name}' is already defined");
        }
    }

    public (Value, IAssignableEnv<string, Value>) Assign(string name, Value value)
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

    public IEnv<string, Value> Pop()
    {
        if(env.Count > 0)
            env.RemoveAt(env.Count-1);
        return this;
    }

    public IEnv<string, Value> Push()
    {
        env.Add(ImmutableDictionary<string, Value>.Empty);
        return this;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < env.Count; i++)
        {
            var layer = env[i];
            sb.Append($"Layer: {i}");
            for (int j = 0; j < i; j++)
            {
                sb.Append(' ');
            }
            foreach(var variable in layer.Keys)
            {
                sb.Append($"{variable}-({layer[variable]}),");
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}