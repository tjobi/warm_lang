namespace WarmLangLexerParser.AST.TypeSyntax;

public sealed class TypeSyntaxUserDefined : TypeSyntaxNode
{
    public string Name { get; }
    public TypeSyntaxUserDefined(SyntaxToken name)
    :base(name.Location)
    {
        Name = name.Name!;
    }
    public override string ToString() => Name;
}