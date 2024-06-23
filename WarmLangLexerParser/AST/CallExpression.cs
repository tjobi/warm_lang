using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class CallExpression : ExpressionNode
{
    public SyntaxToken Called { get; set; }
    public IList<ExpressionNode> Arguments { get; }

    public CallExpression(SyntaxToken called, SyntaxToken openPar, IList<ExpressionNode> arguments, SyntaxToken closePar)
    :base(TextLocation.FromTo(called.Location, closePar.Location))
    {
        Called = called;
        Arguments = arguments;
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder($"(Call {Called.Name}(");
        for (int i = 0; i < Arguments.Count; i++)
        {
            if(i > 0)
                sb.Append(',');
            sb.Append(Arguments[i]);
        }
        sb.Append("))");
        return sb.ToString();
    }
}