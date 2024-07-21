using System.Text;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

using ParameterList = IList<(TypeSyntaxNode type, SyntaxToken name)> ;

public sealed class FuncDeclaration : StatementNode //should it be a different thing entirely?
{
    public SyntaxToken NameToken { get; }
    public ParameterList Params { get; }
    public TypeSyntaxNode? ReturnType { get; }
    public BlockStatement Body { get; }

    public FuncDeclaration(SyntaxToken funcKeyword, SyntaxToken nameToken, ParameterList parameters, BlockStatement body)
    :this(funcKeyword, nameToken, parameters, null, body) 
    { }

    public FuncDeclaration(SyntaxToken funcKeyword, SyntaxToken nameToken, ParameterList parameters, TypeSyntaxNode? returnType, BlockStatement body)
    : base(TextLocation.FromTo(funcKeyword.Location, body.Location))
    {
        NameToken = nameToken;
        Params = parameters;
        ReturnType = returnType;
        Body = body;
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"{NameToken.Name!} (");
        for (int i = 0; i < Params.Count; i++)
        {
            var (typ, name) = Params[i];
            if(i > 0)
                sb.Append(',');
            sb.Append(typ.ToString()).Append(' ');
            sb.Append(name.Name!);
        }
        if(ReturnType is not null)
        {
            sb.Append($") : {ReturnType} => ");
        } else 
        {
            sb.Append(") => ");
        }
        sb.Append(Body);
        return sb.ToString();
    }
}