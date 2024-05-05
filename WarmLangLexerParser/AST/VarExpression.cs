namespace WarmLangLexerParser.AST;

public sealed class VarExpression : ExpressionNode
{
    public string Name {get;}
    public override TokenKind Kind => TokenKind.TIdentifier;

    public VarExpression(string name)
    {
        Name = name;
    }
    public override string ToString() => Name;
}
