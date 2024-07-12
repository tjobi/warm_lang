namespace WarmLangLexerParser;
using static TokenKind;
public static class TokenKindPrecedence
{   
    //precedence levels goes from high to low
    private readonly static int CLASS3 = 1000, CLASS4 = 950,
                                CLASS5 = 900,  CLASS6 = 800,
                                CLASS7 = 750,  CLASS9 = 700,
                                CLASS10 = 600, CLASS13 = 400, 
                                CLASS14 = 360, CLASS15 = 350,
                                REST = -1;

    public static int GetBinaryPrecedence(this TokenKind kind)
    {
        return kind switch
        {
            TDoubleStar               => CLASS4,    //Exponents 2 ** 2
            TStar or TSlash           => CLASS5,    //Multiplication and division, modulo
            TPlus or TMinus           => CLASS6,    //Addition and subtraction
            TDoubleColon              => CLASS7,   //Homemade list_add '::'
            TLessThan 
            or TLessThanEqual
            or TGreaterThan
            or TGreaterThanEqual      => CLASS9,    //Relational operators
            TEqualEqual or TBangEqual => CLASS10,   //Equality operators
            TSeqAND                   => CLASS14,   // '&&' logical and
            TSeqOR                    => CLASS15,   // '||' logical or
            _ => REST                               //The rest shouldn't have any precedence - I think
        };
    }

    public static int GetUnaryPrecedence(this TokenKind kind)
    {
        return kind switch
        {
            TBang                     => CLASS3,    //logical not, should really be right-associative
            TMinus or TPlus           => CLASS3,    //Unary plus and minus
            TLeftArrow                => CLASS13,   //Homemade list_remove '<- [3,5,6]'
            _ => REST
        };
    }

    public static bool IsUnaryExpression(this TokenKind kind)
    {
        return IsPrefixUnaryExpression(kind) || IsPostfixUnaryExpression(kind);
    }

    public static bool IsPrefixUnaryExpression(this TokenKind kind)
    {
        return kind switch
        {
            TBang or TPlus or TMinus or TLeftArrow => true,
            _ => false,
        };
    }

    public static bool IsPostfixUnaryExpression(this TokenKind kind)
    {
        return kind switch 
        {
            _ => false
        };
    }
}