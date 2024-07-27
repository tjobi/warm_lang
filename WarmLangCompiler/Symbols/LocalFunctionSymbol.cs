using System.Collections.Immutable;
using WarmLangCompiler.Binding;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public sealed class LocalFunctionSymbol : FunctionSymbol
{
    public LocalFunctionSymbol(SyntaxToken nameToken, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type)
    : base(nameToken, parameters, type)
    { }

    public BoundBlockStatement? Body { get; set; }

    public override string ToString()
    {
        var baseStr = base.ToString();
        if(Body is null)
            return baseStr;
        return baseStr + $"{{ {Body} }}";
    }
}