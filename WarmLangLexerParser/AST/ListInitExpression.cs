using System.Collections.Immutable;
using System.Text;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class ListInitExpression : ExpressionNode
{
    public ImmutableList<ExpressionNode> Elements { get; set; }
    
    public TypeSyntaxNode? ElementType { get; }

    public bool IsEmptyList => Elements.Count == 0;

    public ListInitExpression(SyntaxToken openBracket, IList<ExpressionNode> elements, SyntaxToken closeBracket)
    :base(TextLocation.FromTo(openBracket, closeBracket))
    {
        Elements = elements.ToImmutableList();
    }

    public ListInitExpression(SyntaxToken openBracket, SyntaxToken closeBracket, TypeSyntaxNode? elementType)
    : base(TextLocation.FromTo(openBracket.Location, elementType?.Location ?? closeBracket.Location)) 
    {
        Elements = ImmutableList<ExpressionNode>.Empty;
        ElementType = elementType;
    }

    public override string ToString()
    {
        if(IsEmptyList)
        {
            return ElementType is null ? "[]" : $"({ElementType})[]";
        }
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