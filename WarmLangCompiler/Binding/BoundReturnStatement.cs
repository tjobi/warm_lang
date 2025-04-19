using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundReturnStatement(StatementNode node)
    : base(node) { }

    public BoundReturnStatement(StatementNode node, BoundExpression? expression)
    : base(node)
    {
        Expression = expression;
    }

    public BoundExpression? Expression { get; }

    public override string ToString() => $"(return {Expression})";
}