using WarmLangLexerParser;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangCompiler.Symbols;

public sealed class TypeParameterSymbol : TypeSymbol
{
    public TextLocation Location { get; }

    public TypeParameterSymbol(TypeSyntaxParameterType p) : base(p.Name) 
    {
        Location = p.Location;
    }
}