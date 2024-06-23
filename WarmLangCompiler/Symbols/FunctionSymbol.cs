using System.Collections.Immutable;
using WarmLangLexerParser.AST;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : Symbol
{
    //Function symbol contains: name, parameters, returnType, body
    public FunctionSymbol(FuncDeclaration declaration, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type) : base(declaration.NameToken.Name!)
    {
        Declaration = declaration;
        Parameters = parameters;
        Type = type;
    }

    public FuncDeclaration Declaration { get; }
    public ImmutableArray<ParameterSymbol> Parameters { get; }
    public TypeSymbol Type { get; }
}
