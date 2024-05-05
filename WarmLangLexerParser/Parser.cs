using WarmLangLexerParser.AST;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangLexerParser;

public class Parser
{
    private readonly IList<SyntaxToken> tokens;
    private int currentToken;
    
    public Parser(IList<SyntaxToken> tokens)
    {
        this.tokens = tokens;
        currentToken = 0;
    }

    private SyntaxToken Current => Peek(0);
    private SyntaxToken Peek(int offset)
    {
        var index = currentToken + offset;
        if(index >= tokens.Count)
            return tokens[index];
        return tokens[index];
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
        return tokens[tokens.Count-1];
    }

    public ASTNode Parse()
    {
        var children = new List<ASTNode>();
        ASTNode left = new BlockStatement(children);
        while(Current.Kind != TEOF)
        {
            switch(Current.Kind)
            {
                case TSemiColon: {
                    NextToken();
                } break;
                default: {
                    children.Add(ParseExpression());
                } break;
            }
        }
        //Our language should finish parsing with an End Of File, no?
        var _ = MatchKind(TEOF);
        return left;
    }

    private ExpressionNode ParseExpression()
    {
        ExpressionNode left = ParsePrimaryExpression();
        while(Current.Kind != TEOF)
        {
            switch(Current.Kind)
            {
                case TStar:     //Meaning next token is a "*"
                case TPlus: {   //Meaning next token is a "+" 
                    var operatorToken = NextToken();
                    var precedence = operatorToken.Kind.GetBinaryPrecedence();
                    var right = ParseBinaryExpression(precedence);
                    var binOp = operatorToken.Kind switch {
                        TPlus => "+",
                        TStar => "*",
                        _ => throw new NotImplementedException($"{Current.Kind} is not yet supported!")
                    };
                    left = new BinaryExpressionNode(left, binOp, right);
                } break;
                case TSemiColon: { // Verryyyy hacky... ;(
                    return left;
                }
                default: {
                    throw new NotImplementedException($"not yet implemented {Current.Kind}");
                }
            }
            
        }
        return left;
    }

    private ExpressionNode ParsePrimaryExpression()
    {
        switch(Current.Kind)
        {
            case TConst: {
                return ParseConstExpression();
            }
            case TInt: { //Variable binding : int x = 2;
                return ParseVariableBindingExpression();
            }
            case TIdentifier: { //About to use a variable : x + 4
                var identToken = NextToken();
                return new VarExpression(identToken.Name!);
            }
            default: {
                return null!;
            }
        }
    }

    private ExpressionNode ParseVariableBindingExpression()
    {
        var type = MatchKind(TInt);
        var name = MatchKind(TIdentifier);
        var _ = NextToken(); // throw away the '='
        var rhs = ParseBinaryExpression(); //Parse the right hand side of a "int x = rhs"
        return new VarBindingExpression(type.Kind, name.Name!, rhs);
    }

    private ExpressionNode ParseConstExpression()
    {
        var token = NextToken();
        return new ConstExpression(token.IntValue!.Value);
    }

    private ExpressionNode ParseBinaryExpression(int parentPrecedence = 0)
    {
        ExpressionNode left = ParsePrimaryExpression();
        while(true)
        {
            var precedence = Current.Kind.GetBinaryPrecedence();
            if(precedence == -1 || precedence <= parentPrecedence) 
            {
                break;
            }
            var operatorToken = NextToken();
            var binOp = operatorToken.Kind switch {
                TPlus => "+",
                TStar => "*",
                _ => throw new NotImplementedException($"{Current.Kind} is not yet supported!")
            };
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpressionNode(left, binOp, right);
        }
        return left;
    }
}