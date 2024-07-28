using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class ASTRoot : ASTNode
{
    public ASTRoot(List<TopLevelStamentNode> children, TextLocation location) : base(location)
    {
        Children = children;
    }

    public ASTRoot(List<TopLevelStamentNode> children)
    :this(children, children.Count > 0 ? TextLocation.FromTo(children[0].Location, children[^1].Location) : TextLocation.EmptyFile) { }

    public List<TopLevelStamentNode> Children { get; }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("Root: {");
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            sb.Append(child.ToString());
            if(i < Children.Count - 1)
                sb.Append(", ");
        }
        sb.Append('}');
        return sb.ToString();
    }
}