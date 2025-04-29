using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using WarmLangCompiler.Binding;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public sealed class LocalFunctionSymbol : FunctionSymbol
{
    public LocalFunctionSymbol(SyntaxToken nameToken,
                               ImmutableArray<TypeSymbol> typeParameters, 
                               ImmutableArray<ParameterSymbol> parameters, 
                               TypeSymbol returnType)
    : base(nameToken, typeParameters, parameters, returnType)  { }

    public BoundBlockStatement? Body { get; set; }
    public HashSet<ScopedVariableSymbol>? Closure { get; set; }

    [MemberNotNullWhen(true, nameof(Body))]
    [MemberNotNullWhen(true, nameof(Closure))]
    public bool IsProper => Body is not null && Closure is not null;

    [MemberNotNullWhen(true, nameof(Closure))]
    public bool RequiresClosure => Closure is not null && Closure.Count > 0;

    public override string ToString() => base.ToString();

    public string ToStringWithBody()
    {
        if(Body is null)
            return ToString();
        return  $"{this}{{ {Body} }}";
    }
}