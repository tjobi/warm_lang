using static WarmLangLexerParser.TokenKind;

namespace WarmLangLexerParser;
public class Lexer
{
    public Lexer() { }

    public IList<SyntaxToken> Lex(string filePath)
    {
        StreamReader sr = new(filePath);
        IList<SyntaxToken> tokens = new List<SyntaxToken>();
        
        for(var line = sr.ReadLine(); line != null; line = sr.ReadLine())
        {
            string[] toTokenize = line.Split(" ");

            foreach(var token in toTokenize)
            {
                var syntaxToken = token switch
                {
                    ";" => new SyntaxToken(TSemiColon),
                    "=" => new SyntaxToken(TEqual),
                    "+" => new SyntaxToken(TPlus),
                    "*" => new SyntaxToken(TStar),
                    "int" => new SyntaxToken(TInt),
                    _ when int.TryParse(token, out var number) => new SyntaxToken(TConst, null, number),
                    _ => new SyntaxToken(TIdentifier, token, 0) 
                };
                tokens.Add(syntaxToken);
            }
        }
        tokens.Add(new(TEOF));
        return tokens;
    }
}