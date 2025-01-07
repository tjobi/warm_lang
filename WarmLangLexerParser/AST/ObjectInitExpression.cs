using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class ObjectInitExpression : ExpressionNode
{
    public ObjectInitExpression(SyntaxToken nameToken, SyntaxToken curlOpen, List<(SyntaxToken, ExpressionNode)> exprs, SyntaxToken curlClose)
    : base(TextLocation.FromTo(nameToken.Location, curlClose.Location))
    {
        NameToken = nameToken;
        Members = exprs;
    }

    public SyntaxToken NameToken { get; }
    public List<(SyntaxToken NameToken, ExpressionNode Rhs)> Members { get; }
    public string? Name => NameToken.Name;
    public override string ToString()
    {
        var sb = new StringBuilder().Append("(Init ").Append(Name).Append('{');
        for (int i = 0; i < Members.Count; i++)
        {
            var (n, r) = Members[i];
            sb.Append('(').Append(n.Name).Append('=').Append(r).Append(')');
            if(i < Members.Count-1) sb.Append(", ");
        }
        return sb.Append("})").ToString();
    }
}