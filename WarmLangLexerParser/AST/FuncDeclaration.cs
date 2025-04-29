using System.Text;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

using ParameterList = IList<(TypeSyntaxNode type, SyntaxToken name)>;
using TypeParameterList = IList<TypeSyntaxParameterType>;

public sealed class FuncDeclaration : StatementNode //should it be a different thing entirely?
{
    public TypeSyntaxNode? OwnerType { get; }
    public SyntaxToken NameToken { get; }
    public TypeParameterList TypeParams { get; }
    public ParameterList Params { get; }
    public TypeSyntaxNode? ReturnType { get; }
    public BlockStatement Body { get; }

    public FuncDeclaration(SyntaxToken funcKeyword, SyntaxToken nameToken, ParameterList parameters, BlockStatement body)
    :this(null, funcKeyword, nameToken, new List<TypeSyntaxParameterType>(), parameters, null, body) 
    { }

    public FuncDeclaration(TypeSyntaxNode? ownerType, SyntaxToken funcKeyword, SyntaxToken nameToken, 
                           TypeParameterList typeParams, ParameterList parameters, 
                           TypeSyntaxNode? returnType, BlockStatement body)
    : base(TextLocation.FromTo(funcKeyword.Location, body.Location))
    {
        OwnerType = ownerType;
        NameToken = nameToken;
        TypeParams = typeParams;
        Params = parameters;
        ReturnType = returnType;
        Body = body;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if(OwnerType is not null)
            sb.Append($"{OwnerType}.");
        sb.Append(NameToken.Name);

        if(TypeParams.Count > 0)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", TypeParams));
            sb.Append('>');
        }

        sb.Append('(');
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