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
                case '=': {
                    token = SyntaxToken.MakeToken(TEqual, Line, Column);
                    AdvanceText();
                    switch(Current)
                    {
                        case '=': { //we've hit a ==
                            token = SyntaxToken.MakeToken(TEqualEqual, Line, Column);
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
                            token = SyntaxToken.MakeToken(TLessThanEqual, Line, Column);
                            AdvanceText();
                        } break;
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
        return SyntaxToken.MakeToken(TConst, startLine, startColumn, intValue: number);
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
        return name switch 
        {
            "function" => SyntaxToken.MakeToken(TFunc, startLine, startColumn),
            "if" => SyntaxToken.MakeToken(TIf, startLine, startColumn),
            "then" => SyntaxToken.MakeToken(TThen, startLine, startColumn),
            "else" => SyntaxToken.MakeToken(TElse, startLine, startColumn),
            "int" => SyntaxToken.MakeToken(TInt, startLine, startColumn),
            //"var" => SyntaxToken.MakeToken(TVar, startLine, startColumn),
            _ => SyntaxToken.MakeToken(TIdentifier, startLine, startColumn, name: name)
        };
    }
}