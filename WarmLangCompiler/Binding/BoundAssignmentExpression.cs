using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundAssignmentExpression : BoundExpression
{
    public BoundAssignmentExpression(ExpressionNode node, BoundAccess access, BoundExpression rightHandSide)
    : base(node, access.Type)
    {
        Access = access;
        RightHandSide = rightHandSide;
    }

    public BoundAccess Access { get; }
    public BoundExpression RightHandSide { get; }
}