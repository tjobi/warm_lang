using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundBinaryExpression : BoundExpression
{
    public BoundBinaryExpression(BinaryExpression node, BoundExpression left, BoundBinaryOperator op, BoundExpression right)
    : base(node, op.Type)
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public BoundExpression Left { get; }
    public BoundBinaryOperator Op { get; }
    public BoundExpression Right { get; }
}