namespace WarmLangCompiler.Interpreter;

using System.Text;
using WarmLangCompiler.Interpreter.Values;
using WarmLangCompiler.Symbols;

public sealed class VariableEnv : IAssignableEnv<VariableSymbol,Value>
{
    private readonly List<Dictionary<VariableSymbol,Value>> env;
    public VariableEnv()
    {
        env = new List<Dictionary<VariableSymbol, Value>>() { new() };
    }

    public (Value, IEnv<VariableSymbol, Value>) Declare(VariableSymbol name, Value value)
    {
        Dictionary<VariableSymbol, Value> scope;
        if(name is GlobalVariableSymbol)
            scope = env[0];
        else
            scope = env.Last();
        scope[name] = value;
        return (value, this);
    }

    public (Value, IAssignableEnv<VariableSymbol, Value>) Assign(VariableSymbol name, Value value)
    {
        for (int i = env.Count - 1; i >= 0 ; i--)
        {
            var scope = env[i];
            if(scope.ContainsKey(name))
            {
                scope[name] = value;
                return (value, this);
            }
        }
        throw new Exception($"Name {name} does not exist");
    }

    public Value Lookup(VariableSymbol name)
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

    public IEnv<VariableSymbol, Value> Pop()
    {
        if(env.Count > 0)
            env.RemoveAt(env.Count-1);
        return this;
    }

    public IEnv<VariableSymbol, Value> Push()
    {
        env.Add(new Dictionary<VariableSymbol, Value>());
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