using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundTypeConversionExpression : BoundExpression
{
    public BoundTypeConversionExpression(ExpressionNode node, TypeSymbol type, BoundExpression expression) : base(node, type)
    {
        Expression = expression;
    }

    public BoundExpression Expression { get; }
}