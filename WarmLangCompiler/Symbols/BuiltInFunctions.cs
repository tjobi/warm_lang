namespace WarmLangCompiler.Symbols;

using System.Collections.Immutable;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;

public static class BuiltInFunctions
{
    private static FunctionSymbol MakeFunction(string name, TypeSymbol type, ImmutableArray<ParameterSymbol> parameters)
    {
        var dummyDecl = new FuncDeclaration(
            SyntaxToken.MakeToken(TokenKind.TFunc,0,0),
            SyntaxToken.MakeToken(TokenKind.TIdentifier,0,0,name),
            new List<(TypeSyntaxNode,SyntaxToken)>(){},
            new BlockStatement(SyntaxToken.MakeToken(TokenKind.TCurLeft,0,0), new List<StatementNode>(),SyntaxToken.MakeToken(TokenKind.TCurRight,0,0))
        );

        return new FunctionSymbol(name, parameters, type, dummyDecl);
    }

    public static readonly FunctionSymbol StdWrite = MakeFunction("stdWrite", TypeSymbol.Void, ImmutableArray.Create(new ParameterSymbol("toPrint", TypeSymbol.Any)));
    public static readonly FunctionSymbol StdWriteLine = MakeFunction("stdWriteLine", TypeSymbol.Void, ImmutableArray.Create(new ParameterSymbol("toPrint", TypeSymbol.Any)));
    public static readonly FunctionSymbol StdRead = MakeFunction("stdRead", TypeSymbol.String, ImmutableArray<ParameterSymbol>.Empty);

    public static IEnumerable<FunctionSymbol> GetBuiltInFunctions() 
    {
        yield return StdWrite;
        yield return StdWriteLine;
        yield return StdRead;
    }
    public static bool IsBuiltInFunction(this FunctionSymbol function)
    {
        foreach(var func in GetBuiltInFunctions())
            if(func == function)
                return true;
        return false;
    }

}