using System.Collections.Immutable;
using WarmLangCompiler.Binding;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Symbols;

public sealed class LocalFunctionSymbol : FunctionSymbol
{
    public LocalFunctionSymbol(FuncDeclaration declaration, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type)
    : base(declaration, parameters, type)
    { }

    public BoundStatement? Body { get; set; }
}