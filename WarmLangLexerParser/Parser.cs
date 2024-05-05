using WarmLangLexerParser.AST;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangLexerParser;

public class Parser
{
    private readonly IList<SyntaxToken> tokens;
    private int currentToken;
    
    private SyntaxToken Current => Peek();
    public Parser(IList<SyntaxToken> tokens)
    {
        this.tokens = tokens;
        currentToken = 0;
    }

    private SyntaxToken Peek()
    {
        return tokens[currentToken];
    }

    private SyntaxToken NextToken()
    {
        return tokens[currentToken++];
    }

    private SyntaxToken MatchKind(TokenKind kind)
    {
        if(Current.Kind == kind)
        {
            return NextToken();
        }
        throw new Exception("What happens here?");
    }

    public ExpressionNode Parse()
    {
        ExpressionNode left = ParsePrimaryExpression();
        while(Current.Kind != TEOF)
        {
            var next = NextToken();
            switch(next.Kind)
            {
                case TPlus: {
                    //Meaning next is a TPlus -> "+"
                    var right = ParseConstExpression();

                    left = new BinaryExpressionNode(left, "+", right);
                } break;
                default: {
                    throw new NotImplementedException($"not yet implemented {next}");
                }
            }
            
        }
        //Our language should finish parsing with an End Of File, no?
        var _ = MatchKind(TEOF);
        return left;
    }

    private ExpressionNode ParsePrimaryExpression()
    {
        switch(Current.Kind)
        {
            case TConst: {
                return ParseConstExpression();
            }
            default: {
                return null!;
            }
        }
    }

    private ExpressionNode ParseConstExpression()
    {
        var token = NextToken();
        return new ConstExpression(token.IntValue!.Value);
    }
}