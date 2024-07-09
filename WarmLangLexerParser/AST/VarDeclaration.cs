using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class VarDeclaration : StatementNode
{
    public SyntaxToken Identifier { get; }

    public TypeSyntaxNode Type { get; set; }
    public ExpressionNode RightHandSide { get; }


    public VarDeclaration(TypeSyntaxNode type, SyntaxToken name, ExpressionNode rightHandSide)
    :base(TextLocation.FromTo(type.Location, rightHandSide.Location))
    {
        Identifier = name;
        RightHandSide = rightHandSide;
        Type = type;
    }

    public VarDeclaration(TypeSyntaxNode type, SyntaxToken name, SyntaxToken equal, ExpressionNode rightHandSide)
    :this(type, name, rightHandSide){ }

    public override string ToString()
    {
        var rhs = RightHandSide.ToString();
        return $"({Identifier.Name!}:{Type} = {rhs})";
    }

}