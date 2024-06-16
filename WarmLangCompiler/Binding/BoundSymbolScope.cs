using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class BoundSymbolScope
{
    private readonly List<Dictionary<string,Symbol>> _scopeStack;

    public BoundSymbolScope()
    {
        _scopeStack = new();
    }

    public void PushScope()
    {
        _scopeStack.Add(new());
    }

    public Dictionary<string, Symbol> PopScope()
    {
        var mostRecentScope = _scopeStack[^1];
        return mostRecentScope;
    }

    public bool TryLookup(string name, out Symbol? type)
    {
        var mostRecentScope = _scopeStack[^1];
        return mostRecentScope.TryGetValue(name, out type);
    }

    public bool TryDeclareVariable(VariableSymbol variable) => TryDeclare(variable.Name, variable);

    public bool TryDeclare(string name, Symbol type)
    {
        var mostRecentScope = _scopeStack[^1];
        if(mostRecentScope.ContainsKey(name))
        {
            return false;
        }
        mostRecentScope.Add(name, type);
        return true;
    }
}