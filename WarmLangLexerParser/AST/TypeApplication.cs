using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class FuncTypeApplication : ExpressionNode
{
    public FuncTypeApplication(Access access, SyntaxToken openAngle, List<TypeSyntaxNode> typeParams, SyntaxToken closeAngle)
    : base(TextLocation.FromTo(openAngle, closeAngle))
    {
        AppliedOn = access;
        TypeParams = typeParams;
    }

    public Access AppliedOn { get; }
    public List<TypeSyntaxNode> TypeParams { get; }

    public override string ToString()
    {
        return $"{AppliedOn}<{string.Join(", ", TypeParams)}>";
    }
}