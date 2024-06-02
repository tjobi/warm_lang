using System.Collections.Immutable;
using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class ListInitExpression : ExpressionNode
{
    public override TokenKind Kind => TokenKind.TArray;

    public ImmutableList<ExpressionNode> Elements { get; set; }

    public ListInitExpression(IList<ExpressionNode> elements)
    {
        Elements = elements.ToImmutableList();
    }

    public override string ToString()
    {
        var sb = new StringBuilder().Append('[');
        
        for(int i = 0; i < Elements.Count; i++)
        {
            var expr = Elements[i];
            sb.Append(expr.ToString());
            if(i < Elements.Count-1)
            {
                sb.Append(", ");
            }
        }
        return sb.Append(']').ToString();
    }
}