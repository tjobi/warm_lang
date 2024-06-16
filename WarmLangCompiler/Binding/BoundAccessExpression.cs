using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundAccessExpression : BoundExpression
{
    public BoundAccessExpression(ExpressionNode node, TypeSymbol type, BoundAccess access)
    : base(node, type)
    {
        Access = access;
    }

    public BoundAccessExpression(ExpressionNode node, VariableSymbol symbol)
    : this(node, symbol.Type, new BoundNameAccess(symbol))
    { }

    public BoundAccess Access { get; }
}