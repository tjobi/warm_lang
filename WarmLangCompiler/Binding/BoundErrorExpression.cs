using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public class BoundErrorExpression : BoundExpression
{
    public BoundErrorExpression(ExpressionNode expr)
    : base(expr, TypeSymbol.Error)
    { }
}