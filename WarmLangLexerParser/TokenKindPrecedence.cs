namespace WarmLangLexerParser;
using static TokenKind;
public static class TokenKindPrecedence
{
    public static int GetBinaryPrecedence(this TokenKind kind)
    {
        return kind switch
        {
            // * 
            TStar => 1000,
            // + 
            TPlus => 100,
            TMinus => 100,
            TLessThan 
                or TLessThanEqual => 75,
            TEqualEqual => 50,
            TDoubleColon => 40,
            //The rest shouldn't have any precedence - I think
            _ => -1
        };
    }

    public static int GetUnaryPrecedence(this TokenKind kind)
    {
        return kind switch
        {
            //TBracketLeft => 20_000, //xs[2]
            TMinus 
            or TPlus => 10_000,
            TColonBang => 35, //TODO: Very whacky!
            _ => -1
        };
    }

    public static bool IsUnaryExpression(this TokenKind kind)
    {
        return IsPrefixUnaryExpression(kind) || IsSuffixUnaryExpression(kind);
    }

    public static bool IsPrefixUnaryExpression(this TokenKind kind)
    {
        return kind switch 
        {
            TPlus or TMinus => true,
            _ => false
        };
    }

    public static bool IsSuffixUnaryExpression(this TokenKind kind)
    {
        return kind switch 
        {
            TColonBang or TBracketLeft => true,
            _ => false
        };
    }
}