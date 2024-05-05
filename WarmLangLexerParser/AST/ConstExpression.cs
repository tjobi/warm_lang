namespace WarmLangLexerParser.AST;

public sealed class ConstExpression : ExpressionNode
{
    public int Value { get; private init; }

    public override TokenKind Kind => TokenKind.TConst;

    public ConstExpression(int value)
    {
        Value = value;
    }

    public override string ToString() => $"CstI {Value}";
    
}