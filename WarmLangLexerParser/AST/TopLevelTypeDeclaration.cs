using System.Text;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class TopLevelTypeDeclaration : TopLevelNode
{
    public TopLevelTypeDeclaration(SyntaxToken typeKeyword, SyntaxToken nameToken, 
                                   IList<TypeSyntaxParameterType>? typeParameters,
                                   SyntaxToken curlOpen, IList<FieldDeclaration> members, 
                                   SyntaxToken curlClose)
    : base(TextLocation.FromTo(typeKeyword, curlClose))
    {
        if(!TypeSyntaxNode.TryGetAsUserDefined(nameToken, out var type)) 
            throw new Exception($"{nameof(TopLevelTypeDeclaration)}: Tried to create a type with a non-identifier token");
        Type = type;
        TypeParameters = typeParameters;
        Members = members;
    }

    public TypeSyntaxIdentifier Type { get; }
    public IList<FieldDeclaration> Members { get; }
    public IList<TypeSyntaxParameterType>? TypeParameters { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("Type: ").Append(Type.Name);
        if(TypeParameters is not null)
        {
            sb.Append('<').AppendJoin(",", TypeParameters).Append('>');
        }
        sb.Append(" = {");
        for (int i = 0; i < Members.Count; i++)
        {
            sb.Append(Members[i]);
            if(i < Members.Count - 1) sb.Append(", ");
        }
        sb.Append('}');
        return sb.ToString();

    }
}
