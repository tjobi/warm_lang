namespace WarmLangCompiler.Interpreter;

using System.Collections.Immutable;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;

public sealed record Funct(ImmutableList<(TokenKind, string)> ParamNames, StatementNode Body)
{
    //Separate constructor to make IList to an immutable list, derp
    public Funct(IList<(TokenKind, string)> paramss, StatementNode body) : this(paramss.ToImmutableList(), body) { }
};