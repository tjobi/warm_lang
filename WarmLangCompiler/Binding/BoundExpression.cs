using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public abstract class BoundExpression
{
    public BoundExpression(ExpressionNode node, TypeSymbol type)
    {
        Node = node;
        Type = type;
    }

    public ExpressionNode Node { get; }
    public TypeSymbol Type { get; }

    public override string ToString() => Node.ToString();
}