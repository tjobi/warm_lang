using System.Collections.Immutable;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundWhileStatement : BoundStatement
{
    public BoundWhileStatement(
        StatementNode node, BoundExpression condition, 
        BoundStatement body, ImmutableArray<BoundExpression> @continue)
    : base(node)
    {
        Condition = condition;
        Body = body;
        Continue = @continue;
    }

    public BoundExpression Condition { get; }
    public BoundStatement Body { get; }
    public ImmutableArray<BoundExpression> Continue { get; }
}