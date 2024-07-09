using System.Collections;
using WarmLangCompiler.Symbols;
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
            case TypeSyntaxList list: 
            {
                if(list.InnerType is TypeSyntaxInt)
                    return TypeSymbol.IntList;
                return ResolveNestedTypeSyntax(type);
            }
            default:
                throw new NotImplementedException($"BinderTypeExntensions doesn't know {type}");
        }
    }
}