namespace WarmLangLexerParser;

public static class TokenKindPrecedence
{
    public static int GetBinaryPrecedence(this TokenKind kind)
    {
        return kind switch
        {
            TokenKind.TStar => 1000,
            TokenKind.TPlus => 100,
            //The rest shouldn't have any precedence - I think
            _ => -1,
        };
    }
}