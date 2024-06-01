using System.Text;
using WarmLangLexerParser.AST.Typs;

namespace WarmLangLexerParser.AST;

using ParameterList = IList<(Typ,string)> ;

public sealed class FuncDeclaration : ExpressionNode //should it be a different thing entirely?
{
    public override TokenKind Kind => TokenKind.TFunc;

    public string Name { get; }
    public ParameterList Params { get; set; }
    public StatementNode Body { get; set; }

    //TODO: Remember to fix <parameters> when we add typing?
    public FuncDeclaration(SyntaxToken nameToken, ParameterList parameters, StatementNode body)
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
            var (typ, name) = Params[i];
            if(i > 0)
                sb.Append(',');
            sb.Append(typ.ToString()).Append(' ');
            sb.Append(name);
        }
        sb.Append(") => ");
        sb.Append(Body);
        return sb.ToString();
    }
}