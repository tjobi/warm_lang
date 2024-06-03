namespace WarmLangLexerParser;
using static TokenKind;
public static class TokenKindExtension
{
    public static string AsString(this TokenKind kind)
    {
        return kind switch 
        {
            TPlus           => "+",
            TStar           => "*",
            TMinus          => "-",
            TEqualEqual     => "==",
            TLessThan       => "<",
            TLessThanEqual  => "<=",
            TDoubleColon    => "::",
            TColonBang      => ":!",
            TLeftArrow      => "<-",
            _ => kind.ToString()
        };
    }
}