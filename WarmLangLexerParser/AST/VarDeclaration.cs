using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class VarDeclaration : StatementNode
{
    public string Name { get; }

    public ATypeSyntax Type { get; set; }
    public ExpressionNode RightHandSide { get; }


    public VarDeclaration(ATypeSyntax type, string name, ExpressionNode rightHandSide)
    :base(TextLocation.FromTo(type.Location, rightHandSide.Location))
    {
        Name = name;
        RightHandSide = rightHandSide;
        Type = type;
    }

    public VarDeclaration(ATypeSyntax type, string name, SyntaxToken equal, ExpressionNode rightHandSide)
    :this(type, name, rightHandSide){ }

    public override string ToString()
    {
        var rhs = RightHandSide.ToString();
        return $"({Name}:{Type} = {rhs})";
    }

}