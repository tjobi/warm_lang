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
            TokenKind.TStringLiteral => constant.Name!,
            _ => throw new NotImplementedException($"ConstExpression doesn't know {constant.Kind.AsString()} yet!"),
        };
    }

    public ConstExpression(int value, TextLocation location)
    :base(location)
    {
        Value = value;
    }

    public override string ToString() 
    {
        if(Value is string s)
        {
            return $"Cst \"{s}\"";
        }
        return $"Cst {Value}";
    }
    
}