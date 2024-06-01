namespace WarmLangLexerParser.AST;

public sealed class SubscriptExpression : ExpressionNode
{
    //TODO: Not really a call, but kind of?
    public override TokenKind Kind => TokenKind.TCall; 

    public string Name { get; }

    public ExpressionNode Subscript { get; }

    public SubscriptExpression(SyntaxToken name, ExpressionNode value)
    {
        if(name.Name is null)
        {
            throw new Exception("Something has gone horribly wrong in parser, trying to subscript null?");
        }
        Name = name.Name!;
        Subscript = value;
    }

    public override string ToString() => $"({Name}[{Subscript}])";
}