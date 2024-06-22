namespace WarmLangLexerParser.AST;

public sealed class ConstExpression : ExpressionNode
{
    public int Value { get; private init; }

    public ConstExpression(SyntaxToken constant)
    :base(constant.Location)
    {
        Value = constant.IntValue ?? 0;
    }

    public ConstExpression(int value, TextLocation location)
    :base(location)
    {
        Value = value;
    }

    public override string ToString() => $"CstI {Value}";
    
}