using WarmLangLexerParser.AST;
using WarmLangLexerParser.Exceptions;
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
        if (elseToken.Kind != TElse) //Could be an EOF if the statement looks like "if <cond> then <stmnt>"
        {
            return new IfStatement(condition, thenStmnt, null);
        }
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
        ExpressionNode left;
        var precedence = Current.Kind.GetUnaryPrecedence(); 
        //If it returns -1, it is certainly not a Unary operator, so go to else-branch and parse a normal BinaryExpression
        if(precedence != -1 && precedence >= parentPrecedence)
        {
            var op = NextToken();
            var expr = ParseBinaryExpression(precedence);
            left = new UnaryExpression(op, expr);
        }
        else 
        {
            left = ParsePrimaryExpression();
        }

        while(true)
        {
            precedence = Current.Kind.GetBinaryPrecedence();
            if(precedence == -1 || precedence <= parentPrecedence) 
            {
                break;
            }
            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpression(left, operatorToken, right);
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
            case TParLeft: {
                return ParseParenthesesExpression();
            }
            case TFunc: {
                return ParseFuncionDeclarationExpression();
            }
            case TIdentifier: //About to use a variable : x + 4
            default: {
                if(Peek(1).Kind == TParLeft)
                {
                    return ParseCallExpression();
                }
                var identToken = NextToken();
                if(identToken.Kind != TIdentifier)
                {
                    throw new ParserException($"Expected expression but got {identToken.Kind}", identToken.Line, identToken.Column);
                }
                return new VarExpression(identToken.Name!);
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

    private ExpressionNode ParseFuncionDeclarationExpression()
    {
        var funcKeyword = MatchKind(TFunc);
        var name = MatchKind(TIdentifier);
        var _ = MatchKind(TParLeft);
        var paramNames = new List<string>();
        if(Current.Kind == TParRight)
        {
            var paramClose = NextToken();
        } else 
        {
            while(Current.Kind != TParRight)
            {
                var next = NextToken();
                if(next.Kind == TIdentifier && next.Name is not null)
                {
                    paramNames.Add(next.Name);
                    if(Current.Kind == TComma && Peek(1).Kind != TParRight)
                    {   //We want to remove commas that separate function params
                        var comma = NextToken();
                    }
                }
                else 
                {
                    throw new ParserException($"Invalid expression in parameter declaration starting with {next.Kind}", next);
                }
            }
            var paramClose = NextToken();
        }
        var body = ParseBlockStatement();
        return new FuncDeclaration(name, paramNames, body);
    }

    private ExpressionNode ParseCallExpression()
    {
        var nameToken = NextToken();
        var openPar = MatchKind(TParLeft);
        var args = new List<ExpressionNode>();
        while(NotEndOfFile && Current.Kind != TParRight)
        {
            var arg = ParseExpression();
            var comma = MatchKind(TComma);
            args.Add(arg);
        }
        var closePar = MatchKind(TParRight);
        return new CallExpression(nameToken, args);
    }
}