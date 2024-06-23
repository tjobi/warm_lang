namespace WarmLangCompiler.Interpreter;

using System.Collections.Immutable;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;

public sealed record Funct(ImmutableList<(ATypeSyntax, string)> ParamNames, StatementNode Body)
{
    //Separate constructor to make IList to an immutable list, derp
    public Funct(IList<(ATypeSyntax, string)> paramss, StatementNode body) : this(paramss.ToImmutableList(), body) { }

    public Funct(IList<(ATypeSyntax, SyntaxToken)> paramss, StatementNode body) 
    : this(paramss.Select(s => (s.Item1, s.Item2.Name!)).ToImmutableList(), body) { }
};