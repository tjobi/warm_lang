using static WarmLangLexerParser.Token;

namespace WarmLangLexerParser;
public class Lexer
{
    public Lexer() { }

    public IList<SyntaxToken> ParseTextFile(string filePath)
    {
        StreamReader sr = new(filePath);
        IList<SyntaxToken> tokens = new List<SyntaxToken>();
        
        for(var line = sr.ReadLine(); line != null; line = sr.ReadLine())
        {
            string[] toTokenize = line.Split(" ");

            foreach(var elm in toTokenize)
            {
                switch(elm) 
                {
                    case ";": {
                        tokens.Add(new SyntaxToken(){Kind=TSemiColon});
                    } break;
                    case "=": {
                        tokens.Add(new SyntaxToken(){Kind=TEqual});
                    } break;
                    case "int": {
                        tokens.Add(new SyntaxToken(){Kind=TInt});
                    } break;
                    case "+": {
                        tokens.Add(new SyntaxToken(){Kind=TPlus});
                    } break;
                    default: {
                        if(int.TryParse(elm, out var number))
                        {
                            tokens.Add(new SyntaxToken(){Kind=TConst, IntValue = number});
                        } else
                        {
                            tokens.Add(new SyntaxToken(){Kind=TVariableName, Name = elm});
                        }
                    } break;
                }
            }
        }
        return tokens;
    }
}