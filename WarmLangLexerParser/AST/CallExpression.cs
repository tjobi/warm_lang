using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class CallExpression : ExpressionNode
{
    public override TokenKind Kind => TokenKind.TCall;

    public string Name { get; }
    public IList<ExpressionNode> Arguments { get; }


    public CallExpression(SyntaxToken name, IList<ExpressionNode> arguments)
    {
        Name = name.Name!; //Little ugly withthe BANG! :)
        Arguments = arguments;
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder($"Call {Name}(");
        for (int i = 0; i < Arguments.Count; i++)
        {
            if(i > 0)
                sb.Append(',');
            sb.Append(Arguments[i]);
        }
        sb.Append(')');
        return sb.ToString();
    }
}