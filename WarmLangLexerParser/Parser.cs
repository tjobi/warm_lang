using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.Typs;
using WarmLangLexerParser.ErrorReporting;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangLexerParser;

public class Parser
{
    private readonly IList<SyntaxToken> tokens;
    private readonly ErrorWarrningBag _diag;
    private int currentToken;
    
    public Parser(IList<SyntaxToken> tokens, ErrorWarrningBag diag)
    {
        this.tokens = tokens;
        currentToken = 0;
        _diag = diag;
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
        int line = Current.Line;
        int col = Current.Column;
        _diag.ReportUnexpectedToken(kind, Current.Kind, line, col);
        return new SyntaxToken(TBadToken, line, col);
    }

    private SyntaxToken MatchKinds(params TokenKind[] kinds)
    {
        if(kinds.Contains(Current.Kind))
        {
            return NextToken();
        }
        int line = Current.Line;
        int col = Current.Column;
        _diag.ReportUnexpectedTokenFromMany(kinds, Current.Kind, line, col);
        return new SyntaxToken(TBadToken, line, col);
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
            //TODO: May be a problem when we introduce more types? -- what to do?
            TInt => ParseVariableDeclaration(), 
            TFunc =>  ParseFunctionDeclaration(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementNode ParseIfStatement()
    {
        var ifToken   = MatchKind(TIf);
        var condition = ParseExpression();
        var thenToken = MatchKind(TThen);
        var thenStmnt = ParseStatement();
        if (Current.Kind != TElse) //Could be an EOF if the statement looks like "if <cond> then <stmnt>"
        {
            return new IfStatement(condition, thenStmnt, null);
        }
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

    private StatementNode ParseVariableDeclaration()
    {
        var type = ParseType();
        var name = MatchKind(TIdentifier);
        var _ = NextToken(); // throw away the '='
        var rhs = ParseExpression(); //Parse the right hand side of a "int x = rhs"
        var semicolon = MatchKind(TSemiColon);
        return new VarDeclarationExpression(type, name.Name!, rhs);
    }


    private StatementNode ParseFunctionDeclaration()
    {
        var funcKeyword = MatchKind(TFunc);
        var name = MatchKind(TIdentifier);
        var _ = MatchKind(TParLeft);
        //Params are tuples of:  "function myFunc(int x, int y)" -> (int, x) -> (ParameterType, ParameterName)
        List<(Typ,string)> paramNames = new(); 
        if(Current.Kind == TParRight)
        {
            var parClose = NextToken();
        } else 
        {
            var parseParameter = true;
            while(parseParameter 
                  //&& Current.Kind != TParRight
                  && NotEndOfFile)
            {
                var paramType = ParseType(); 
                var paramName = MatchKind(TIdentifier);
                paramNames.Add( (paramType, paramName.Name!) );
                if(Current.Kind == TComma)
                {
                    var comma = MatchKind(TComma);
                } else 
                {
                    parseParameter = false;
                }
            }
            var paramClose = MatchKind(TParRight);
        }
        var body = ParseBlockStatement();
        return new FuncDeclaration(name, paramNames, body);
    }

    private StatementNode ParseExpressionStatement()
    {
        //Lifts an expression to a statement, x + 5;
        // the semicolon makes it a statement.
        var expr = ParseExpression();
        var semicolon = MatchKind(TSemiColon);
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
            case TParLeft: {
                return ParseParenthesesExpression();
            }
            case TBracketLeft: {
                //Allow list initialization like [] or [1,2,3,4]?
                var bracketOpen = MatchKind(TBracketLeft);
                var staticElements = new List<ExpressionNode>();
                var isReading = true;
                while(isReading && NotEndOfFile)
                {
                    var next = ParseExpression();
                    staticElements.Add(next);
                    if(Current.Kind != TComma)
                    {
                        isReading = false;
                    } else
                    {
                        var comma = NextToken();
                    }
                }
                var bracketClose = MatchKind(TBracketRight);
                return new ArrayInitExpression(staticElements);
            }
            case TIdentifier: {
                //About to use a variable : x + 4 or call a function x()
                if(Peek(1).Kind == TParLeft)
                {
                    return ParseCallExpression();
                } else if (Peek(1).Kind == TBracketLeft)
                {
                    return ParseSubscriptExpression();
                }
                var identToken = MatchKind(TIdentifier);
                return new VarExpression(identToken.Name!);
            }
            default: {
                var nextToken = Current.Kind == TEOF ? Current : NextToken();
                //var nextToken = Current;
                _diag.ReportInvalidExpression(nextToken);
                return new ErrorExpressionNode(nextToken.Line, nextToken.Column);
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

    private ExpressionNode ParseConstExpression()
    {
        var token = NextToken();
        return new ConstExpression(token.IntValue!.Value);
    }

    private Typ ParseType()
    {
        var type = MatchKinds(TInt); //MatchKinds can take many arguments, so MatchKinds(TInt, TBool, TStr) would match those 3 :D
        Typ typ = type.Kind switch 
        {
            TInt => new TypInt(),
            _ => new TypInvalid() //TODO: user-defined types
        };
        if(Current.Kind == TBracketLeft)
        {
            var bracketOpen = NextToken();
            var bracketClose = MatchKind(TBracketRight);
            return new TypArray(typ);
        }
        return typ;
    }

    private ExpressionNode ParseCallExpression()
    {
        var nameToken = NextToken();
        var openPar = MatchKind(TParLeft);
        var args = new List<ExpressionNode>();
        if(Current.Kind != TParRight)
        {
            var isReadingArgs = true;
            while(isReadingArgs && NotEndOfFile) 
                //&& Current.Kind != TParRight) //TODO: removed this, so we don't allow 
                                                //stuff like myFunc(2,), those trailing commas D:
            {
                var arg = ParseExpression();
                args.Add(arg);
                if(Current.Kind == TComma)
                {
                    var comma = MatchKind(TComma);
                } else 
                {
                    isReadingArgs = false;
                }
            }
        }

        var closePar = MatchKind(TParRight);
        return new CallExpression(nameToken, args);
    }

    private ExpressionNode ParseSubscriptExpression()
    {
        var nameToken = MatchKind(TIdentifier);
        var bracketOpen = MatchKind(TBracketLeft);
        var expr = ParseExpression();
        var bracketClose = MatchKind(TBracketRight);
        return new SubscriptExpression(nameToken, expr);
    }
}