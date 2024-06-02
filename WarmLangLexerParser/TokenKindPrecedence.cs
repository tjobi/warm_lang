namespace WarmLangLexerParser;

public static class TokenKindPrecedence
{
    public static int GetBinaryPrecedence(this TokenKind kind)
    {
        return kind switch
        {
            // * 
            TokenKind.TStar => 1000,
            // + 
            TokenKind.TPlus => 100,
            TokenKind.TMinus => 100,
            TokenKind.TLessThan 
                or TokenKind.TLessThanEqual => 75,
            TokenKind.TEqualEqual => 50,
            TokenKind.TDoubleColon => 40,
            //The rest shouldn't have any precedence - I think
            _ => -1
        };
    }

    public static int GetUnaryPrecedence(this TokenKind kind)
    {
        return kind switch
        {
            TokenKind.TMinus 
            or TokenKind.TPlus => 10_000,
            TokenKind.TColonBang => 40, //TODO: Very whacky!
            _ => -1
        };
    }
}