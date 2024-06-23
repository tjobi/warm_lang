using System.Collections.Immutable;
using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class BlockStatement : StatementNode
{
    public ImmutableList<StatementNode> Children { get; }

    public BlockStatement(SyntaxToken leftCurlyBrace, IList<StatementNode> children, SyntaxToken rightCurlyBrace)
    :base(TextLocation.FromTo(leftCurlyBrace, rightCurlyBrace))
    {
        Children = children.ToImmutableList();
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