using System.Diagnostics.CodeAnalysis;
using Mono.Cecil.Cil;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class BoundSymbolScope
{
    private readonly List<Dictionary<string,EntitySymbol>> _scopeStack;
    
    public Dictionary<string, EntitySymbol> GlobalScope => _scopeStack[0];

    public BoundSymbolScope()
    {
        _scopeStack = new();
    }

    public void PushScope()
    {
        _scopeStack.Add(new());
    }

    public Dictionary<string, EntitySymbol> PopScope()
    {
        var mostRecentScope = _scopeStack[^1];
        if(_scopeStack.Count > 0)
            _scopeStack.RemoveAt(_scopeStack.Count-1);
        return mostRecentScope;
    }

    public bool TryLookup(string name, [NotNullWhen(true)] out EntitySymbol? type)
    {
        type = null;
        for (int i = _scopeStack.Count - 1; i >= 0 ; i--)
        {
            var scope  = _scopeStack[i];
            if(scope.TryGetValue(name, out type))
                return true;
        }
        return false;
    }

    public bool IsUnboundInCurrentAndGlobalScope(string name)
    {
        var curScope = _scopeStack[^1];
        return !(curScope.ContainsKey(name) || GlobalScope.ContainsKey(name));
    }

    public bool IsUnboundInCurrentAndGlobalScope(Symbol symbol) => IsUnboundInCurrentAndGlobalScope(symbol.Name);

    public bool TryDeclareVariable(VariableSymbol variable) => TryDeclare(variable.Name, variable);

    public bool TryDeclareFunction(FunctionSymbol function) => TryDeclare(function.Name, function);

    public bool TryDeclare(string name, EntitySymbol type)
    {
        if(type is GlobalVariableSymbol)
            return TryDeclareGlobal(name, type);
        var mostRecentScope = _scopeStack[^1];
        if(mostRecentScope.ContainsKey(name))
        {
            return false;
        }
        mostRecentScope.Add(name, type);
        return true;
    }

    private bool TryDeclareGlobal(string name, EntitySymbol type)
    {
        if(GlobalScope.ContainsKey(name))
            return false;
        GlobalScope.Add(name, type);
        return true;
    }

    public IList<FunctionSymbol> GetFunctions() => GetSymbol<FunctionSymbol>();

    public IList<TSymbol> GetSymbol<TSymbol>()
        where TSymbol : EntitySymbol
    {
        List<TSymbol> res = new();
        foreach(var scope in _scopeStack)
        {
            foreach(var entry in scope)
            {
                if(entry.Value is TSymbol x)
                    res.Add(x);
            }
        }
        return res;
    } 

    public void Print()
    {
        for (int i = _scopeStack.Count-1; i >= 0; i--)
        {
            Console.WriteLine($"Scope: {i}");
            var layer = _scopeStack[i];
            foreach(var key in layer.Keys)
            {
                Console.WriteLine($"  ({key},{layer[key]})");
            }
        }
    }
}