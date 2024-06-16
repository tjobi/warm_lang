using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundUnaryExpression : BoundExpression
{
    public BoundUnaryExpression(ExpressionNode node, BoundUnaryOperator op, BoundExpression left)
    : base(node, op.Type)
    {
        Operator = op;
        Left = left;
    }

    public BoundUnaryOperator Operator { get; }
    public BoundExpression Left { get; }
}