namespace WarmLangCompiler.Interpreter;

using System.Collections.Immutable;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.Typs;

public sealed record Funct(ImmutableList<(TypeClause, string)> ParamNames, StatementNode Body)
{
    //Separate constructor to make IList to an immutable list, derp
    public Funct(IList<(TypeClause, string)> paramss, StatementNode body) : this(paramss.ToImmutableList(), body) { }
};