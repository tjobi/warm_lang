using System.Collections.Immutable;
using WarmLangCompiler.Binding;

namespace WarmLangCompiler.Symbols;

public sealed class FunctionSymbol : Symbol
{
    //Function symbol contains: name, parameters, returnType, body
    public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, BoundStatement body) : base(name)
    {
        Parameters = parameters;
        Type = type;
        Body = body;
    }

    public ImmutableArray<ParameterSymbol> Parameters { get; }
    public TypeSymbol Type { get; }
    public BoundStatement Body { get; }
}
