using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundAccessExpression : BoundExpression
{
    public BoundAccessExpression(ExpressionNode node, VariableSymbol variable)
    : base(node, variable.Type)
    {
        Variable = variable;
    }

    public VariableSymbol Variable { get; }
}