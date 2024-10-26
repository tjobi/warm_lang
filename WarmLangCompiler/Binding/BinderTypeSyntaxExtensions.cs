using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangCompiler.Binding;

public static class BinderTypeSyntaxExtensions
{
    private static TypeSymbol ResolveNestedTypeSyntax(TypeSyntaxNode aType)
    {
        if(aType is TypeSyntaxList tsl)
        {
            TypeSymbol res = ResolveNestedTypeSyntax(tsl.InnerType);
            return new ListTypeSymbol($"list<{res}>", res);
        }
        return aType.ToTypeSymbol();
    }
    public static TypeSymbol ToTypeSymbol(this TypeSyntaxNode? type)
    {
        if(type is null)
        {
            return TypeSymbol.Void;
        }
        switch(type)
        {
            case TypeSyntaxInt: 
                return TypeSymbol.Int;
            case TypeSyntaxBool: 
                return TypeSymbol.Bool;
            case TypeSyntaxString:
                return TypeSymbol.String;
            case TypeSyntaxList list: 
            {
                if(list.InnerType is TypeSyntaxInt)
                    return TypeSymbol.IntList;
                return ResolveNestedTypeSyntax(type);
            }
            case TypeSyntaxUserDefined u:
                if(!TypeSymbol.DefinedTypes.TryGetValue(u.Name, out var res)) 
                    TypeSymbol.DefinedTypes[u.Name] = res = new TypeSymbol(u.Name);
                return res;
            default:
                throw new NotImplementedException($"BinderTypeExntensions doesn't know {type}");
        }
    }
}

public static class BinderTokenKindExtensions 
{
    public static TypeSymbol? ToTypeSymbol(this TokenKind kind)
    {
        return kind switch
        {
            TokenKind.TInt => TypeSymbol.Int,
            TokenKind.TBool => TypeSymbol.Bool,
            TokenKind.TString => TypeSymbol.String,
            _ => null,
        };
    }
}