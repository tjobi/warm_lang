using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundListExpression : BoundExpression
{
    public BoundListExpression(ExpressionNode node, TypeSymbol type, ImmutableArray<BoundExpression> expressions) 
    : base(node, type)
    {
        Expressions = expressions;
    }

    public ImmutableArray<BoundExpression> Expressions { get; }
}