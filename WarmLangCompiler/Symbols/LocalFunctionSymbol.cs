using System.Collections.Immutable;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public sealed class LocalFunctionSymbol : FunctionSymbol
{
    public LocalFunctionSymbol(SyntaxToken nameToken,
                               ImmutableArray<TypeSymbol> typeParameters,
                               ImmutableArray<ParameterSymbol> parameters,
                               TypeSymbol funcType, TypeSymbol returnType)
    : base(nameToken, typeParameters, parameters, funcType, returnType, isGlobal: false) { }

    public override string ToString() => base.ToString();
}