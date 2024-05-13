using System.Text;
using WarmLangLexerParser.Exceptions;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangLexerParser;
public class Lexer
{
    private int col, row;
    private readonly StreamReader reader;
    private string curLine;
    public Lexer(IFileReader fileReader)
    { 
        col = row = 0;
        reader = fileReader.GetStreamReader();
        curLine = reader.ReadLine() ?? "";
    }
    
    public Lexer(string filePath)
    {
        col = row = 0;
        reader = new StreamReader(filePath);
        curLine = reader.ReadLine() ?? "";
    }

    private char Current => Peek();
    private bool IsEndOfFile => reader.EndOfStream && curLine == "" ;

    private char Peek()
    {
        try 
        {
            return curLine[col];
        } catch (Exception)
        {
            Console.WriteLine($"LEXER Failed: on line: {row+1}, column: {col+1}");
            throw;
        }
    }

    private void AdvanceText()
    {
        col++;
        if(col >= curLine.Length)
        {
            string? line = "";
            while(!reader.EndOfStream && string.IsNullOrWhiteSpace(line = reader.ReadLine())) 
            {
                row++;
            }
            curLine = line ?? "";
            col = 0;
            row++;
        }
    }

    private void AdvanceLine()
    {
        string? line = null;
        while(!reader.EndOfStream && string.IsNullOrWhiteSpace(line = reader.ReadLine())) 
        {
            row++;
        }
        curLine = line ?? "";
        col = 0;
        row++;
    }

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
                    token = SyntaxToken.MakeToken(TSemiColon, row, col);
                    AdvanceText();
                } break;
                case '/': {
                    token = SyntaxToken.MakeToken(TSlash, row, col);
                    AdvanceText();
                    if(Current == '/')
                    {
                        //We've reached a comment
                        AdvanceLine();
                        continue;
                    }
                } break;
                case ',': {
                    token = SyntaxToken.MakeToken(TComma, row, col);
                    AdvanceText();
                } break;
                case '=': {
                    token = SyntaxToken.MakeToken(TEqual, row, col);
                    AdvanceText();
                    switch(Current)
                    {
                        case '=': { //we've hit a ==
                            token = SyntaxToken.MakeToken(TEqualEqual, row, col);
                            AdvanceText();
                        } break;
                    }
                } break;
                case '<': {
                    token = SyntaxToken.MakeToken(TLessThan, row, col);
                    AdvanceText();
                    switch(Current)
                    {
                        case '=': {
                            token = SyntaxToken.MakeToken(TLessThanEqual, row, col);
                            AdvanceText();
                        } break;
                    }
                } break;
                case '+': {
                    token = SyntaxToken.MakeToken(TPlus, row, col);
                    AdvanceText();
                } break;
                case '-':{
                    token = SyntaxToken.MakeToken(TMinus, row, col);
                    AdvanceText();
                } break;
                case '*': {
                    token = SyntaxToken.MakeToken(TStar, row, col);
                    AdvanceText();
                } break;
                case '{': {
                    token = SyntaxToken.MakeToken(TCurLeft, row, col);
                    AdvanceText();
                } break;
                case '}': {
                    token = SyntaxToken.MakeToken(TCurRight, row, col);
                    AdvanceText();
                } break;
                case '(': {
                    token = SyntaxToken.MakeToken(TParLeft, row, col);
                    AdvanceText();
                } break;
                case ')': {
                    token = SyntaxToken.MakeToken(TParRight, row, col);
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
                        throw new LexerException($"Invalid character '{Current}'", row, col);
                    }
                } break;
            }
            tokens.Add(token);
        }
        //End token stream with an EndOfFile
        tokens.Add(SyntaxToken.MakeToken(TEOF, row, col));
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
        return SyntaxToken.MakeToken(TConst, row, col, intValue: number);
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
            "function" => SyntaxToken.MakeToken(TFunc, row, col),
            "if" => SyntaxToken.MakeToken(TIf, row, col),
            "then" => SyntaxToken.MakeToken(TThen, row, col),
            "else" => SyntaxToken.MakeToken(TElse, row, col),
            "var" => SyntaxToken.MakeToken(TVar, row, col),
            _ => SyntaxToken.MakeToken(TIdentifier, row, col, name: name)
        };
    }
}