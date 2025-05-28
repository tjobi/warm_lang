using System.Text;
using WarmLangLexerParser.Read;
using WarmLangLexerParser.ErrorReporting;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangLexerParser;
public class Lexer
{
    private readonly TextWindow _window;
    private readonly ErrorWarrningBag _diag;

    public Lexer(TextWindow window, ErrorWarrningBag reporter)
    { 
        _window = window;
        _diag = reporter;
    }

    public static Lexer FromFile(string path, ErrorWarrningBag bag) => new(new FileWindow(path), bag);
    public static Lexer FromString(string target, ErrorWarrningBag bag) => new(new StringWindow(target), bag);
    
    private char Current => _window.Peek();
    private bool IsEndOfFile => _window.IsEndOfFile;
    
    private int Column => _window.Column + 1;
    private int Line => _window.Line + 1; 

    private void AdvanceText() => _window.AdvanceText();

    private void AdvanceLine() => _window.AdvanceLine(); 

    private TextLocation CurrentLocation => new(Line,Column);

    public IList<SyntaxToken> Lex()
    {
        IList<SyntaxToken> tokens = new List<SyntaxToken>();
        while(!IsEndOfFile)
        {
            SyntaxToken token;
            if(char.IsWhiteSpace(Current)) 
            {
                AdvanceText();
                continue;
            }
            switch(Current)
            {
                case ';': {
                    token = SyntaxToken.MakeToken(TSemiColon, Line, Column);
                    AdvanceText();
                } break;
                case '/': {
                    token = SyntaxToken.MakeToken(TSlash, Line, Column);
                    AdvanceText();
                    if(Current == '/')
                    {
                        //We've reached a comment
                        AdvanceLine();
                        continue;
                    }
                } break;
                case ',': {
                    token = SyntaxToken.MakeToken(TComma, Line, Column);
                    AdvanceText();
                } break;
                case '.': {
                    token = SyntaxToken.MakeToken(TDot, Line, Column);
                    AdvanceText();
                } break;
                case '=': {
                    token = SyntaxToken.MakeToken(TEqual, Line, Column);
                    AdvanceText();
                    switch(Current)
                    {
                        case '=': { //we've hit a ==
                            token.Extend(TEqualEqual, Line, Column);
                            AdvanceText();
                        } break;
                    }
                } break;
                case '<': {
                    token = SyntaxToken.MakeToken(TLessThan, Line, Column);
                    AdvanceText();
                    switch(Current)
                    {
                        case '=': {
                            token.Extend(TLessThanEqual, CurrentLocation);
                            AdvanceText();
                        } break;
                        case '-': {
                            token.Extend(TLeftArrow, CurrentLocation);
                            AdvanceText();
                        } break;
                    }
                } break;
                case '>': {
                    token = SyntaxToken.MakeToken(TGreaterThan, Line, Column);
                    AdvanceText();
                    if(Current == '=')
                    {
                        token.Extend(TGreaterThanEqual, CurrentLocation);
                        AdvanceText();
                    }
                } break;
                case ':': {
                    token = SyntaxToken.MakeToken(TColon, CurrentLocation);
                    AdvanceText();
                    switch(Current)
                    {
                        case ':': {
                            token.Extend(TDoubleColon, CurrentLocation);
                            AdvanceText();
                        } break;
                    }
                } break;
                case '!': {
                    token = SyntaxToken.MakeToken(TBang, Line, Column);
                    AdvanceText();
                    switch(Current)
                    {
                        case '=': {
                            token.Extend(TBangEqual, CurrentLocation);
                            AdvanceText();
                        } break;
                    }
                } break;
                case '&': {
                    token = SyntaxToken.MakeToken(TAmp, CurrentLocation);
                    AdvanceText();
                    if(Current == '&')
                    {
                        token.Extend(TSeqAND, CurrentLocation);
                        AdvanceText();
                    }
                } break;
                case '|': {
                    token = SyntaxToken.MakeToken(TBar, CurrentLocation);
                    AdvanceText();
                    if(Current == '|')
                    {
                        token.Extend(TSeqOR, CurrentLocation);
                        AdvanceText();
                    }
                } break;
                case '+': {
                    token = SyntaxToken.MakeToken(TPlus, Line, Column);
                    AdvanceText();
                } break;
                case '-':{
                    token = SyntaxToken.MakeToken(TMinus, Line, Column);
                    AdvanceText();
                } break;
                case '*': {
                    token = SyntaxToken.MakeToken(TStar, Line, Column);
                    AdvanceText();
                    if(Current == '*') 
                    {
                        token.Extend(TDoubleStar, CurrentLocation);
                        AdvanceText();
                    }
                } break;
                case '{': {
                    token = SyntaxToken.MakeToken(TCurLeft, Line, Column);
                    AdvanceText();
                } break;
                case '}': {
                    token = SyntaxToken.MakeToken(TCurRight, Line, Column);
                    AdvanceText();
                } break;
                case '(': {
                    token = SyntaxToken.MakeToken(TParLeft, Line, Column);
                    AdvanceText();
                } break;
                case ')': {
                    token = SyntaxToken.MakeToken(TParRight, Line, Column);
                    AdvanceText();
                } break;
                case '[': {
                    token = SyntaxToken.MakeToken(TBracketLeft, Line, Column);
                    AdvanceText();
                } break;
                case ']': {
                    token = SyntaxToken.MakeToken(TBracketRight, Line, Column);
                    AdvanceText();
                } break;
                case '"': {
                    token = LexStringLiteral();
                } break;
                default: {
                    if(char.IsDigit(Current))
                    {
                        token = LexNumericLiteral();
                    } else if(Current is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') 
                              || Current == '_')
                    {
                        token = LexKeywordOrIdentifier();
                    }
                    else 
                    {
                        //I imagine we just throw in BadToken instead of crashing in the lexer - we are free to crash outside the lexer
                        token = SyntaxToken.MakeToken(TBadToken, Line, Column);
                        _diag.ReportInvalidCharacter(Current, Line, Column);
                        AdvanceText();
                    }
                } break;
            }
            tokens.Add(token);
        }
        //End token stream with an EndOfFile
        tokens.Add(SyntaxToken.MakeToken(TEOF, Line, Column));
        return tokens;
    }

    private SyntaxToken LexStringLiteral()
    {
        //Eat first "
        int startLine = Line, startColumn = Column;
        AdvanceText();
        var sb = new StringBuilder();
        var isDone = false;
        while(!IsEndOfFile && !isDone)
        {
            switch(Current)
            {
                case '"':
                    isDone = true;
                    break;
                case '\r':
                case '\n':
                    _diag.ReportNewLineStringLiteral(new TextLocation(startLine, startColumn, Line,Column));
                    isDone = true;
                    break;
                case '\\':
                    AdvanceText();
                    switch(Current)
                    {
                        case 'n':
                            sb.Append('\n');
                            AdvanceText();
                            break;
                        case '"':
                            sb.Append('"');
                            AdvanceText();
                            break;
                        default:
                            sb.Append('\\');
                            break;
                    }
                    break;
                default:
                    sb.Append(Current);
                    AdvanceText();
                    break;
            }
        }
        //Eat last "
        AdvanceText();
        var location = new TextLocation(startLine, startColumn, Line, Column);
        return new SyntaxToken(TStringLiteral, location, sb.ToString(),0);
    }

    private SyntaxToken LexNumericLiteral()
    {
        //TODO: Maybe just work on straight up numbers?
        //collect the full number as a string
        var sb = new StringBuilder();
        var startColumn = Column;
        var startLine = Line;
        for(; !IsEndOfFile && char.IsDigit(Current); AdvanceText())
        {
            sb.Append(Current);
        }
        var number = int.Parse(sb.ToString());
        var location = new TextLocation(startLine, startColumn, Line, Column);
        return SyntaxToken.MakeToken(TConst, location, intValue: number);
    }

    private SyntaxToken LexKeywordOrIdentifier()
    {
        var sb = new StringBuilder();
        var readingKeywordOrIdentifier = true;
        var startColumn = Column;
        var startLine = Line;
        while(readingKeywordOrIdentifier && !IsEndOfFile)
        {
            switch(Current) 
            {
                case '_': //allow variables with names that include _ (underscore)
                case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9'): { //TODO: How do we handle unicode æøå? D:
                    sb.Append(Current);
                    AdvanceText();
                } break;
                case ' ': case '\n': case ';': case '=': case '+': case '*':
                case '{': case '}': case '(': case ')': // We know none of these are valid identifiers.
                default: //What to do in the default case, are we lexing invalid stuff?
                    readingKeywordOrIdentifier = false;
                    break;
            }
        }
        var name = sb.ToString();
        TokenKind kind = name switch 
        {
            "function" => TFunc,
            "if" => TIf,
            //"then" => TThen,
            "else" => TElse,
            "int" => TInt,
            "while" => TWhile,
            "var" => TVar,
            "return" => TReturn,
            "false" => TFalse,
            "true" => TTrue,
            "bool" => TBool,
            "string" => TString,
            "type" => TType,
            "new" => TNew,
            "null" => TNull,
            _ => TIdentifier,
        };
        var location = new TextLocation(startLine, startColumn, Line, Column);
        return SyntaxToken.MakeToken(kind, location, kind == TIdentifier ? name : null);
    }
}