using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public sealed class LocalFunctionSymbol : FunctionSymbol
{
    public LocalFunctionSymbol(SyntaxToken nameToken,
                               ImmutableArray<TypeSymbol> typeParameters,
                               ImmutableArray<ParameterSymbol> parameters,
                               TypeSymbol funcType, TypeSymbol returnType)
    : base(nameToken, typeParameters, parameters, funcType, returnType, isGlobal: false) { }

    public HashSet<ScopedVariableSymbol>? Closure { get; set; }

    [MemberNotNullWhen(true, nameof(Closure))]
    public bool RequiresClosure => Closure is not null && Closure.Count > 0;

    public override string ToString() => base.ToString();
}