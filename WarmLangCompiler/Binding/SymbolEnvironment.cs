using System.Diagnostics.CodeAnalysis;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class SymbolEnvironment
{
    private readonly List<Dictionary<string,EntitySymbol>> _scopeStack;
    // private readonly BinderTypeHelper _typeManagerGlobal;
    private readonly BinderTypeScope _typeManagerGlobal;

    public Dictionary<string, EntitySymbol> GlobalScope => _scopeStack[0];

    public int Depth => _scopeStack.Count;

    public SymbolEnvironment(BinderTypeScope typeHelper)
    {
        _scopeStack = new();
        _typeManagerGlobal = typeHelper;
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

    public bool TryDeclareVariable(VariableSymbol variable, bool allowShadowing = false) => TryDeclare(variable, allowShadowing);

    public bool TryDeclareFunction(FunctionSymbol function) => TryDeclare(function, allowShadowing:false);

    //TODO: Should this instead return the reason it failed? it is a function, variable, type...
    private bool TryDeclare(EntitySymbol symbol, bool allowShadowing)
    {
        if(!allowShadowing && AnyScopeContainsOrIsAType(symbol.Name)) return false;
        var scope = _scopeStack[^1];
        if (symbol is GlobalVariableSymbol) scope = GlobalScope;
        scope.Add(symbol.Name, symbol);
        return true;
    }

    private bool AnyScopeContainsOrIsAType(string name) 
    {
        if(_typeManagerGlobal.ContainsTypeWithNameOf(name)) return true;

        foreach(var scope in _scopeStack)
            if(scope.ContainsKey(name)) return true;
        
        return false;
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