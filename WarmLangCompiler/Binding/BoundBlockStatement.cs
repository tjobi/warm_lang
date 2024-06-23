using System.Collections.Immutable;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public class BoundBlockStatement : BoundStatement
{
    public BoundBlockStatement(StatementNode syntax, ImmutableArray<BoundStatement> statements) 
    : base(syntax)
    {
        Statements = statements;
    }

    public ImmutableArray<BoundStatement> Statements { get; }
}
