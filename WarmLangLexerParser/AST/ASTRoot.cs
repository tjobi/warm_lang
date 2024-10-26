using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class ASTRoot : ASTNode
{
    public ASTRoot(List<TopLevelNode> children, TextLocation location) : base(location)
    {
        Children = children;
    }

    public ASTRoot(List<TopLevelNode> children)
    :this(children, children.Count > 0 ? TextLocation.FromTo(children[0].Location, children[^1].Location) : TextLocation.EmptyFile) { }

    public List<TopLevelNode> Children { get; }

    public IEnumerable<T> GetChildrenOf<T>() 
    where T : TopLevelNode
    {
        foreach(var child in Children) 
            if(child is T t) yield return t;
    }

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