using System.Collections.Immutable;
using WarmLangCompiler.Binding;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public sealed class FunctionSymbol : Symbol
{
    //Function symbol contains: name, parameters, returnType, body
    public FunctionSymbol(SyntaxToken nameToken, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, BoundStatement body) : base(nameToken.Name!)
    {
        Parameters = parameters;
        Type = type;
        Body = body;
    }

    public ImmutableArray<ParameterSymbol> Parameters { get; }
    public TypeSymbol Type { get; }
    public BoundStatement Body { get; }
}
