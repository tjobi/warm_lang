using System.Text;
using WarmLangLexerParser.Exceptions;
using WarmLangLexerParser.Read;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangLexerParser;
public class Lexer
{
    private readonly TextWindow _window;
    public Lexer(TextWindow window)
    { 
        _window = window;
    }
    
    private char Current => _window.Peek();
    private bool IsEndOfFile => _window.IsEndOfFile;
    
    private int Column => _window.Column;
    private int Line => _window.Line; 

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
                default: {
                    if(char.IsDigit(Current))
                    {
                        token = LexNumericLiteral();
                    } else if(char.IsLetter(Current) || Current == '_')
                    {
                        token = LexKeywordOrIdentifier();
                    }
                    else 
                    {
                        throw new LexerException($"Invalid character '{Current}'", Line, Column);
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
        for(; !IsEndOfFile && char.IsDigit(Current); AdvanceText())
        {
            sb.Append(Current);
        }
        var number = int.Parse(sb.ToString());
        return SyntaxToken.MakeToken(TConst, Line, Column, intValue: number);
    }

    private SyntaxToken LexKeywordOrIdentifier()
    {
        var sb = new StringBuilder();
        var readingKeywordOrIdentifier = true;
        while(readingKeywordOrIdentifier && !IsEndOfFile)
        {
            switch(Current) 
            {
                case '_': //allow variables with names that include _ (underscore)
                case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'): { //TODO: How do we handle unicode æøå? D:
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
            "function" => SyntaxToken.MakeToken(TFunc, Line, Column),
            "if" => SyntaxToken.MakeToken(TIf, Line, Column),
            "then" => SyntaxToken.MakeToken(TThen, Line, Column),
            "else" => SyntaxToken.MakeToken(TElse, Line, Column),
            "var" => SyntaxToken.MakeToken(TVar, Line, Column),
            _ => SyntaxToken.MakeToken(TIdentifier, Line, Column, name: name)
        };
    }
}