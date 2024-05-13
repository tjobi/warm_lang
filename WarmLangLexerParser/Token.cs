namespace WarmLangLexerParser;

public enum TokenKind
{
    TConst,
    TIdentifier, //Variable names, function names...
    TVar, //var x = 5, the keyword 'var'
    TSemiColon, TComma, TDot, TEOF,
    TEqual, 
    TEqualEqual, TLessThan, TLessThanEqual, TPlus, TStar, TSlash, TMinus,
    TBlock, TParentheses, TCurLeft, TCurRight, TParLeft, TParRight,
    TAssign, //Assignment x = 10;
    TIfStmnt, TIf, TThen, TElse, //To allow if <cond> then <statement> else <statement>
    TFunc, TCall //function & function calls
}

public record SyntaxToken 
{
    public TokenKind Kind { get; init; }
    public string? Name { get; init; }
    public int? IntValue { get; init; }

    public int Line { get; set; }
    public int Column { get; set; }

    public SyntaxToken(TokenKind kind, int line, int col)
    {
        Kind = kind;
        Line = line;
        Column = col;
    }

    public SyntaxToken(TokenKind kind, int line, int col, string? name, int intValue) 
        : this(kind,line,col)
    {
        Name = name;
        IntValue = intValue;
    }

    public static SyntaxToken MakeToken(TokenKind kind, int line, int col, string? name = null, int? intValue = null)
    {
        return kind switch 
        {
            TokenKind.TIdentifier or TokenKind.TConst => new SyntaxToken(kind, line, col, name, intValue ?? 0),
            _ => new SyntaxToken(kind, line, col)
        };
    }
}