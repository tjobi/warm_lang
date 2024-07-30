using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangLexerParser.AST;

public sealed class AccessPredefinedType : Access
{
    public AccessPredefinedType(TypeSyntaxNode syntax) : base(syntax.Location)
    {
        Syntax = syntax;
    }

    public TypeSyntaxNode Syntax { get; }

    public override string ToString() => $"{Syntax.ToTokenKind().AsString()}";
}