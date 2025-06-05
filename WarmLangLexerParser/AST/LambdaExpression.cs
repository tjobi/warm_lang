using System.Text;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class LambdaExpression : ExpressionNode
{
    public LambdaExpression(TextLocation location,
                            IList<(TypeSyntaxNode? type, SyntaxToken nameToken)> parameters,
                            ExpressionNode body)
    : base(location)
    {
        Parameters = parameters;
        Body = body;
    }

    public IList<(TypeSyntaxNode? type, SyntaxToken nameToken)> Parameters { get; }
    public ExpressionNode Body { get; }

    public override string ToString()
    {
        var sb = new StringBuilder().Append("((");

        for (int i = 0; i < Parameters.Count; i++)
        {
            var (type, nameToken) = Parameters[i];
            if (type is not null) sb.Append(type).Append(' ');
            sb.Append(nameToken.Name);
            if (i < Parameters.Count - 1) sb.Append(", ");
        }
        sb.Append(") => ").Append(Body).Append(')');
        return sb.ToString();
    }
}