namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxUserDefined : ATypeSyntax
{
    public string Name { get; }
    public TypeSyntaxUserDefined(SyntaxToken name)
    {
        Name = name.Name!;
    }
    public override string ToString() => Name;
}