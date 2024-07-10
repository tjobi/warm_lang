namespace WarmLangLexerParser;
using static TokenKind;
public static class TokenKindExtension
{
    public static string AsString(this TokenKind kind)
    {
        return kind switch 
        {
            TPlus               => "+",
            TStar               => "*",
            TDoubleStar         => "**",
            TSlash              => "/",
            TMinus              => "-",
            TEqualEqual         => "==",
            TLessThan           => "<",
            TLessThanEqual      => "<=",
            TGreaterThan        => ">",
            TGreaterThanEqual   => ">=",
            TDoubleColon        => "::",
            TColonBang          => ":!",
            TLeftArrow          => "<-",
            TEqual              => "=",
            TBang               => "!",
            TBangEqual          => "!=",
            TInt                => "int",
            TBool               => "bool",
            TString             => "string",
            _ => kind.ToString()
        };
    }
}