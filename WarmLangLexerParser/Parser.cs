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
    private bool NotEndOfFile => Current.Kind != TEOF;
    private SyntaxToken Peek(int offset)
    {
        var index = currentToken + offset;
        if(index >= tokens.Count)
            return tokens[^1];
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
        ASTNode left = ParseEntry();
        //Our language should finish parsing with an End Of File, no?
        var _ = MatchKind(TEOF);
        return left;
    }

    public StatementNode ParseEntry()
    {
        var statements = new List<StatementNode>();
        while( NotEndOfFile && Current.Kind != TCurRight)
        {
            var statement = ParseStatement(); 
            statements.Add(statement);
        }
        return new BlockStatement(statements);
    }

    private StatementNode ParseStatement()
    {
        return Current.Kind switch 
        {
            TCurLeft => ParseBlockStatement(),
            TIf => ParseIfStatement(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementNode ParseIfStatement()
    {
        var ifToken   = MatchKind(TIf);
        var condition = ParseExpression();
        var thenToken = MatchKind(TThen);
        var thenStmnt = ParseStatement();
        var elseToken = MatchKind(TElse);
        var elseStmnt = ParseStatement();
        return new IfStatement(condition, thenStmnt, elseStmnt);
    }

    private StatementNode ParseBlockStatement()
    {
        var statements = new List<StatementNode>();
        var _ = MatchKind(TCurLeft);
        while( NotEndOfFile && Current.Kind != TCurRight)
        {
            var statement = ParseStatement();
            statements.Add(statement);
        }
        var __ = MatchKind(TCurRight);
        return new BlockStatement(statements);
    }
    private StatementNode ParseExpressionStatement()
    {
        //Lifts an expression to a statement, x + 5;
        // the semicolon makes it a statement.
        var expr = ParseExpression();
        var semiColonToken = MatchKind(TSemiColon);
        return new ExprStatement(expr);
    }

    private ExpressionNode ParseExpression() => ParseVariableAssignmentExpression();

    private ExpressionNode ParseVariableAssignmentExpression()
    {
        if(Current.Kind == TIdentifier)
        {
            switch(Peek(1).Kind) //Look ahead to next token, x = (<--) 5, look for a '=' 
            {
                case TEqual: {
                    var nameToken = NextToken();
                    var equalToken = NextToken();
                    var rightHandSide = ParseBinaryExpression();
                    return new VarAssignmentExpression(nameToken, rightHandSide);
                }
            }
        }

        return ParseBinaryExpression();
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
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpressionNode(left, operatorToken, right);
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
            case TVar: { //Variable binding : var x = 2;
                return ParseVariableDeclarationExpression();
            }
            case TIdentifier: { //About to use a variable : x + 4
                var identToken = NextToken();
                return new VarExpression(identToken.Name!);
            }
            case TParLeft: {
                return ParseParenthesesExpression();
            }
            default: {
                return null!;
            }
        }
    }

    private ExpressionNode ParseParenthesesExpression()
    {
        var openPar = MatchKind(TParLeft);
        var expr = ParseBinaryExpression();
        var closePar = MatchKind(TParRight);
        return expr;
    }

    private ExpressionNode ParseVariableDeclarationExpression()
    {
        var type = MatchKind(TVar);
        var name = MatchKind(TIdentifier);
        var _ = NextToken(); // throw away the '='
        var rhs = ParseBinaryExpression(); //Parse the right hand side of a "int x = rhs"
        return new VarDeclarationExpression(type.Kind, name.Name!, rhs);
    }

    private ExpressionNode ParseConstExpression()
    {
        var token = NextToken();
        return new ConstExpression(token.IntValue!.Value);
    }
}