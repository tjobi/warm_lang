using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class BlockStatement : StatementNode
{
    public override TokenKind Kind => TokenKind.TBlock;

    public IList<ASTNode> Children { get; }

    public BlockStatement(IList<ASTNode> children)
    {
        Children = children;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append('{');
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