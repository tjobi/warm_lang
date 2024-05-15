using System.Text;

namespace WarmLangLexerParser.AST;

public sealed class FuncDeclaration : ExpressionNode //should it be a different thing entirely?
{
    public override TokenKind Kind => TokenKind.TFunc;

    public string Name { get; }
    public IList<(TokenKind, string)> Params { get; set; }
    public StatementNode Body { get; set; }

    //TODO: Remember to fix <parameters> when we add typing?
    public FuncDeclaration(SyntaxToken nameToken, IList<(TokenKind, string)> parameters, StatementNode body)
    {
        Name = nameToken.Name!;
        Params = parameters;
        Body = body;
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"{Name} (");
        for (int i = 0; i < Params.Count; i++)
        {
            var param = Params[i];
            if(i > 0)
                sb.Append(',');
            sb.Append(param);
        }
        sb.Append(") => ");
        sb.Append(Body);
        return sb.ToString();
    }
}