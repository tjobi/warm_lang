using WarmLangLexerParser;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangCompiler.Symbols;

public sealed class TypeParameterSymbol : TypeSymbol
{   
    public TextLocation Location { get; }


    //TODO: uniqueName is based on the location - come back when we allow multiple files!
    //Make typesymbols unique globally (across multiple functions) - but not locally functions!
    //       Is there a better way, that is also consistent in BinderTypeScope?
    //       This is bound to be such a GOTCHA - haha *pains*
    public readonly string? uniqueName;

    public TypeParameterSymbol(TypeSyntaxParameterType p) : this(p.Name, p.Location) { }

    public TypeParameterSymbol(string name, TextLocation location) 
    : base(name) 
    {
        uniqueName = $"{name}@{location}";
        Location = location;
    }

    //TODO: All of these use uniqueName - please do fix - if the ToString is removed
    //      code inside of BoundBinaryOperator will break ... because it uses the overloaded '==' 
    //      operator on typesymbols... What a mess! 
    public override string ToString()
    {
        return uniqueName ?? base.ToString();
    }

    public override int GetHashCode()
    {
        if(uniqueName is not null) return uniqueName.GetHashCode();
        return base.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if(uniqueName is not null && obj is TypeParameterSymbol other) 
        {
            return uniqueName == other.uniqueName;
        }
        return base.Equals(obj);
    }
}