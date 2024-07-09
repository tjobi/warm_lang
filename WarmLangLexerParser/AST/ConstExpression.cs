namespace WarmLangLexerParser.AST;

public sealed class ConstExpression : ExpressionNode
{
    public object Value { get; private init; }

    public ConstExpression(SyntaxToken constant)
    :base(constant.Location)
    {
        Value = constant.Kind switch {
            TokenKind.TConst => constant.IntValue!,
            TokenKind.TTrue => true,
            TokenKind.TFalse => false,
            _ => throw new NotImplementedException($"ConstExpression doesn't know {constant.Kind.AsString()} yet!"),
        };
    }

    public ConstExpression(int value, TextLocation location)
    :base(location)
    {
        Value = value;
    }

    public override string ToString() => $"Cst {Value}";
    
}