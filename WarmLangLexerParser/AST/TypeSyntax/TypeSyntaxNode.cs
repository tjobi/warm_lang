namespace WarmLangLexerParser.AST.TypeSyntax;
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
            TIdentifier => new TypeSyntaxUserDefined(token), //user-defined types
            //fine to throw an exception here, means we need to implement another case 
            _ => throw new NotImplementedException($"{nameof(TypeSyntaxNode)} doesn't recognize {token.Kind} yet")
        };
    }
}