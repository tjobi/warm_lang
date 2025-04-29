using System.Text;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class ObjectInitExpression : ExpressionNode
{
    public ObjectInitExpression(TypeSyntaxNode objectType, SyntaxToken curlOpen, List<(SyntaxToken, ExpressionNode)> exprs, SyntaxToken curlClose)
    : base(TextLocation.FromTo(objectType.Location, curlClose.Location))
    {
        ObjectType = objectType;
        Members = exprs;
    }

    public TypeSyntaxNode ObjectType { get; }
    public List<(SyntaxToken NameToken, ExpressionNode Rhs)> Members { get; }
    public string Name => ObjectType.ToString();
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