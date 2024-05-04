

using WarmLangLexerParser.AST;

namespace WarmLangLexerParser;

public class Parser
{
    private int currentToken;
    public Parser()
    {
        currentToken = 0;
    }

    public void Parse(IList<SyntaxToken> tokens)
    {
        foreach(var token in tokens)
        {
           
        }
    }
}