using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class FieldDeclaration : ASTNode
{
    public FieldDeclaration(TypeSyntaxNode type, SyntaxToken name) : base(type.Location, name.Location)
    {
        Type = type;
        NameToken = name;
    }

    public TypeSyntaxNode Type { get; }
    public SyntaxToken NameToken { get; }

    public string Name => NameToken.Name!;

    public override string ToString() => $"({Name}:{Type})";
}