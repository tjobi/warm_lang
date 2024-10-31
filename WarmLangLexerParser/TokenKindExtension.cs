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
            TSeqAND             => "&&",
            TSeqOR              => "||",
            TType               => "type",
            _ => kind.ToString()
        };
    }

    public static bool IsPossibleType(this TokenKind kind) => kind switch
    {
        TInt => true,
        TBool => true,
        TIdentifier => true,
        TString => true,
        _ => false,
    };

    public static IEnumerable<TokenKind> GetPossibleTypeKinds()
    {
        foreach(var kind in Enum.GetValues<TokenKind>())
        {
            if(kind.IsPossibleType())
                yield return kind;
        }
    }
}