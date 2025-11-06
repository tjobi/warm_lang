using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
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

    public TextLocation Location => Node.Location;

    public override string ToString() => Node.ToString();
}


public sealed class BoundNullExpression : BoundExpression
{
    public BoundNullExpression(ExpressionNode node) : base(node, TypeSymbol.Null) { }
}