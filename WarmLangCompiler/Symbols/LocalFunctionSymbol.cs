using System.Collections.Immutable;
using WarmLangCompiler.Binding;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Symbols;

public sealed class LocalFunctionSymbol : FunctionSymbol
{
    public LocalFunctionSymbol(FuncDeclaration declaration, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type)
    : base(declaration, parameters, type)
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