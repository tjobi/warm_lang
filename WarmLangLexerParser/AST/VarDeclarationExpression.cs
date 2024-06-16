using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class VarDeclarationExpression : StatementNode
{
    private readonly TokenKind _kind;
    public override TokenKind Kind => _kind;

    public string Name { get; }

    public TypeSyntax.ATypeSyntax Type { get; set; }
    public ExpressionNode RightHandSide { get; }

    public VarDeclarationExpression(TypeSyntax.ATypeSyntax type, string name, ExpressionNode rightHandSide)
    {
        _kind = type.ToTokenKind();
        Name = name;
        RightHandSide = rightHandSide;
        Type = type;
    }

    public override string ToString()
    {
        var rhs = RightHandSide.ToString();
        return $"({Name}:{Type} = {rhs})";
    }

}