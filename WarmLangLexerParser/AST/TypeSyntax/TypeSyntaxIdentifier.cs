namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxIdentifier : TypeSyntaxNode
{
    public string Name { get; }
    public TypeSyntaxIdentifier(SyntaxToken name)
    :base(name.Location)
    {
        Name = name.Name!;
    }
    public override string ToString() => Name;
}