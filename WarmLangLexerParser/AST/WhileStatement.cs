using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class WhileStatement : StatementNode
{
    public ExpressionNode Condition { get; }

    public IList<ExpressionNode> Continue { get; }
    public StatementNode Body { get; }

    public WhileStatement(SyntaxToken whileKeyword, ExpressionNode cond, StatementNode body)
    :base(TextLocation.FromTo(whileKeyword.Location, body.Location))
    {
        Condition = cond;
        Body = body;
        Continue = new List<ExpressionNode>();
    }

    public WhileStatement(SyntaxToken whileKeyword, ExpressionNode cond, StatementNode body, IList<ExpressionNode> cont) 
    : this(whileKeyword, cond, body)
    {
        Continue = cont;
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"(While {Condition}");
        if(Continue.Any())
        {
            sb.Append(" : (");
            for (int i = 0; i < Continue.Count; i++)
            {
                var expr = Continue[i];
                sb.Append(expr);
                if(i < Continue.Count-1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(')');
        }
        return sb.Append(Body).Append(')').ToString();
    }
}