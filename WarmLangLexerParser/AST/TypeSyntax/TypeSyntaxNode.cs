namespace WarmLangLexerParser.AST.TypeSyntax;

using System.Diagnostics.CodeAnalysis;
using static WarmLangLexerParser.TokenKind;

public abstract class TypeSyntaxNode : ASTNode
{
    public abstract override string ToString();

    public TypeSyntaxNode(TextLocation location)
    :base(location) { }

    public static TypeSyntaxNode FromSyntaxToken(SyntaxToken token)
    {
        if(!token.Kind.IsPossibleType())
            return new BadTypeSyntax(token.Location);
        return token.Kind switch
        {
            TInt => new TypeSyntaxInt(token.Location),
            TBool => new TypeSyntaxBool(token.Location),
            TString => new TypeSyntaxString(token.Location),
            TIdentifier => new TypeSyntaxIdentifier(token), //user-defined types
            //fine to throw an exception here, means we need to implement another case 
            _ => throw new NotImplementedException($"{nameof(TypeSyntaxNode)} doesn't recognize {token.Kind} yet")
        };
    }

    public static bool TryGetAsUserDefined(SyntaxToken token, [NotNullWhen(true)] out TypeSyntaxIdentifier? res)
    {
        res = null;
        if(token.Kind == TIdentifier && token.Kind.IsPossibleType())
            res = new TypeSyntaxIdentifier(token);
        return res is not null;
    }
}

public sealed class TypeSyntaxParameterType : TypeSyntaxNode
{
    public TypeSyntaxParameterType(SyntaxToken token) : base(token.Location)
    {
        Name = token.Name!;
    }

    public string Name { get; }

    public override string ToString() => Name;
}

public sealed class TypeSyntaxTypeApplication : TypeSyntaxNode
{
    public TypeSyntaxTypeApplication(TypeSyntaxNode genericType, SyntaxToken angleOpen, IList<TypeSyntaxNode> typeArguments, SyntaxToken angleClose ) 
    : base(TextLocation.FromTo(genericType.Location, angleClose.Location))
    {
        GenericType = genericType;
        TypeArguments = typeArguments;
    }

    public TypeSyntaxNode GenericType { get; }
    public IList<TypeSyntaxNode> TypeArguments { get; }

    public override string ToString() 
    {
        return $"{GenericType}<{string.Join(",", TypeArguments)}>";
    }
}