using System.Text;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;


public abstract class TopLevelNode : ASTNode
{
    protected TopLevelNode(TextLocation location) : base(location) { }
}

/// <summary>
/// Any statement that exists outside of a function body
/// The idea is to allow either TopLevelStatments or a main function.
///     Any TopLevelStatements will be collected into an implicit main function
/// </summary>
public abstract class TopLevelStamentNode : TopLevelNode
{
    public TopLevelStamentNode(StatementNode stmnt) : base(stmnt.Location) {}

    public abstract StatementNode Statement { get; }
}

public sealed class TopLevelTypeDeclaration : TopLevelNode
{
    public TopLevelTypeDeclaration(SyntaxToken nameToken, IList<MemberDeclaration> members) : base(nameToken.Location)
    {
        if(!TypeSyntaxNode.TryGetAsUserDefined(nameToken, out var type)) 
            throw new Exception($"{nameof(TopLevelTypeDeclaration)}: Tried to create a type with a non-identifier token");
        Type = type;
        Members = members;
    }

    public TypeSyntaxUserDefined Type { get; }
    public IList<MemberDeclaration> Members { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("Type: ").Append(Type.Name).Append("={");
        for (int i = 0; i < Members.Count; i++)
        {
            sb.Append(Members[i]);
            if(i < Members.Count - 1) sb.Append(", ");
        }
        sb.Append('}');
        return sb.ToString();

    }
}

public sealed class MemberDeclaration : ASTNode
{
    public MemberDeclaration(TypeSyntaxNode type, SyntaxToken name) : base(type.Location, name.Location)
    {
        Type = type;
        NameToken = name;
    }

    public TypeSyntaxNode Type { get; }
    public SyntaxToken NameToken { get; }

    public string Name => NameToken.Name!;

    public override string ToString() => $"({Name}:{Type})";
}