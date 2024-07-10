using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;
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
        _diag.ReportUnexpectedToken(kind, Current.Kind, Current.Location);
        return new SyntaxToken(TBadToken, Current.Location);
    }

    private SyntaxToken MatchKinds(params TokenKind[] kinds)
    {
        if(kinds.Contains(Current.Kind))
        {
            return NextToken();
        }
        _diag.ReportUnexpectedTokenFromMany(kinds, Current.Kind, Current.Location);
        return new SyntaxToken(TBadToken, Current.Location);
    }

    public ASTNode Parse()
    {
        ASTNode left = ParseEntry();
        //Our language should finish parsing with an End Of File, no?
        var _ = MatchKind(TEOF);
        return left;
    }

    private StatementNode ParseEntry()
    {
        var statements = new List<StatementNode>();
        while( NotEndOfFile && Current.Kind != TCurRight)
        {
            var statement = ParseStatement(); 
            statements.Add(statement);
        }
        return new BlockStatement(tokens[0], statements, tokens[^1]);
    }

    private StatementNode ParseStatement()
    {
        return Current.Kind switch 
        {
            TCurLeft => ParseBlockStatement(),
            TIf => ParseIfStatement(),
            TWhile => ParseWhileStatement(),
            TReturn => ParseReturnStatement(),
            //TODO: May be a problem when we introduce more types? -- what to do?
            TInt or TBool or TString => ParseVariableDeclaration(), 
            TFunc =>  ParseFunctionDeclaration(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementNode ParseReturnStatement()
    {
        var returnToken = MatchKind(TReturn);
        ExpressionNode? expr = null;
        if(Current.Kind != TSemiColon)
        {
            expr = ParseExpression();
        }
        var semicolon = MatchKind(TSemiColon);
        return new ReturnStatement(returnToken, expr);
    }

    private StatementNode ParseWhileStatement()
    {
        var whileToken = MatchKind(TWhile);
        var condition = ParseExpression();
        var exprList = new List<ExpressionNode>();
        if(Current.Kind == TColon) //Then it looks like "while condition : cont(,cont)*" 
        {
            var colon = MatchKind(TColon);
            var parseCont = true;
            do{
                var expr = ParseExpression();
                exprList.Add(expr);
                if(Current.Kind == TComma)
                {
                    var comma = NextToken();
                } else
                {
                    parseCont = false;
                }
            } while(NotEndOfFile && parseCont);
        }
        var tokenBeforeBody = Current;
        var body = ParseBlockStatement();
        if(tokenBeforeBody.Kind != TCurLeft)
        {
            _diag.ReportWhileExpectedBlockStatement(tokenBeforeBody);
        }
        return new WhileStatement(whileToken, condition, body, exprList);
    }

    private StatementNode ParseIfStatement()
    {
        var ifToken   = MatchKind(TIf);
        var condition = ParseExpression();
        var thenStmnt = ParseBlockStatement();
        if (Current.Kind != TElse) //Could be an EOF if the statement looks like "if <cond> then <stmnt>"
        {
            return new IfStatement(ifToken, condition, thenStmnt, null);
        }
        var elseToken = MatchKind(TElse);
        StatementNode elseStmnt;
        if(Current.Kind != TCurLeft)
        {
            if(Current.Kind != TIf)
                _diag.ReportExpectedIfStatement(Current);
            elseStmnt = ParseIfStatement();
        } else 
        {
            elseStmnt = ParseBlockStatement();
        }
        return new IfStatement(ifToken, condition, thenStmnt, elseStmnt);
    }

    private StatementNode ParseBlockStatement()
    {
        var statements = new List<StatementNode>();
        var open = MatchKind(TCurLeft);
        while( NotEndOfFile && Current.Kind != TCurRight)
        {
            var statement = ParseStatement();
            statements.Add(statement);
        }
        var close = MatchKind(TCurRight);
        return new BlockStatement(open, statements, close);
    }

    private StatementNode ParseVariableDeclaration()
    {
        var type = ParseType();
        var name = MatchKind(TIdentifier);
        var equal = NextToken(); // throw away the '='
        var rhs = ParseExpression(); //Parse the right hand side of a "int x = rhs"
        var semicolon = MatchKind(TSemiColon);
        return new VarDeclaration(type, name, equal, rhs);
    }


    private StatementNode ParseFunctionDeclaration()
    {
        var funcKeyword = MatchKind(TFunc);
        var name = MatchKind(TIdentifier);
        var _ = MatchKind(TParLeft);
        //Params are tuples of:  "function myFunc(int x, int y)" -> (int, x) -> (ParameterType, ParameterName)
        List<(TypeSyntaxNode,SyntaxToken)> paramNames = new(); 
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
                paramNames.Add( (paramType, paramName) );
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
        TypeSyntaxNode? returnType = null;
        if(Current.Kind != TCurLeft)
        {
            returnType = ParseType();
        }
        var body = (BlockStatement) ParseBlockStatement();
        return new FuncDeclaration(funcKeyword, name, paramNames, returnType, body);
    }

    private StatementNode ParseExpressionStatement()
    {
        //Lifts an expression to a statement, x + 5;
        // the semicolon makes it a statement.
        var expr = ParseExpression();
        var semicolon = MatchKind(TSemiColon);
        return new ExprStatement(expr);
    }

    private ExpressionNode ParseExpression() => ParseSubExpression();

    private ExpressionNode ParseSubExpression(int parentPrecedence = 0)
    {
        ExpressionNode left;
        var precedence = Current.Kind.GetUnaryPrecedence(); 
        //If it returns -1, it is certainly not a Unary operator, so go to else-branch and parse a normal BinaryExpression
        if(Current.Kind.IsPrefixUnaryExpression() 
            && precedence != -1 && precedence >= parentPrecedence)
        {
            var op = NextToken();
            var expr = ParseSubExpression(precedence);
            left = new UnaryExpression(op, expr);
        }
        else 
        {
            left = ParsePrimaryExpression();
            precedence = parentPrecedence;
        }
        return ParseContinuedExpression(left, precedence);
    }

    private ExpressionNode ParseContinuedExpression(ExpressionNode left, int parentPrecedence = 0)
    {
        switch(Current.Kind)
        {
            case TEqual:
            {
                var op = NextToken();
                var rhs = ParseSubExpression();
                if(left is AccessExpression ae)
                {
                    return new AssignmentExpression(ae.Access, op, rhs);
                }
                return new AssignmentExpression(new ExprAccess(left), op, rhs);
            }
            default:
                return ParseBinaryExpression(left,parentPrecedence);
        }
    }

    private ExpressionNode ParseBinaryExpression(ExpressionNode left, int parentPrecedence = 0)
    {
        while(true)
        {
            var precedence = Current.Kind.GetBinaryPrecedence();
            if(precedence == -1 || precedence <= parentPrecedence) 
            {
                break;
            }
            var operatorToken = NextToken();
            var right = ParseSubExpression(precedence);
            left = new BinaryExpression(left, operatorToken, right);
        }
        return left;
    }

    private ExpressionNode ParsePrimaryExpression()
    {
        ExpressionNode res;
        switch(Current.Kind)
        {
            case TStringLiteral:
            case TTrue:
            case TFalse:
            case TConst: {
                res = ParseConstExpression();
            } break;
            case TParLeft: {
                res = ParseParenthesesExpression();
            } break;
            case TBracketLeft:
            {
                res = ParseListInitializtionExpression();
            }
            break;
            case TBool or TInt or TString: {
                var nameToken = NextToken();
                nameToken = new SyntaxToken(nameToken.Kind,nameToken.Location, name: nameToken.Kind.AsString(), 0);
                res = ParseCallExpression(nameToken); 
            } break;
            case TIdentifier: {
                var nameToken = NextToken();
                if(Current.Kind == TParLeft)
                {
                    res = ParseCallExpression(nameToken);
                }
                else 
                {
                    res = new AccessExpression(new NameAccess(nameToken));
                }
            } break;
            default: {
                var nextToken = Current.Kind == TEOF ? Current : NextToken();
                //var nextToken = Current;
                _diag.ReportInvalidExpression(nextToken);
                return new ErrorExpressionNode(nextToken);
            }
        }
        return ParsePostfixExpression(res);
    }

    private ExpressionNode ParsePostfixExpression(ExpressionNode res)
    {
        //TODO: how do we account for operator precedence? What if I want a postfix unary operator with some precedence?
        while(true)
        {
            switch(Current.Kind)
            {
                //TODO: Enable again, once we allow functions as types
                    //So we can do something like myFuncList[2](parameter1, parameter2);
                // case TParLeft:
                // {
                //     res = ParseCallExpression(res);  
                // } continue;
                case TBracketLeft:
                {
                    var open = MatchKind(TBracketLeft);
                    var expr = ParsePrimaryExpression();
                    var close = MatchKind(TBracketRight);
                    if(res is AccessExpression ae)
                    {
                        res = new AccessExpression(new SubscriptAccess(ae.Access, expr));
                    }
                    else 
                    {
                        res = new AccessExpression(new SubscriptAccess(new ExprAccess(res), expr));
                    }
                } continue;
                default:
                    return res;
            }
        }
    }

    private ExpressionNode ParseParenthesesExpression()
    {
        var openPar = MatchKind(TParLeft);
        var expr = ParseSubExpression();
        var closePar = MatchKind(TParRight);
        return expr;
    }

    private ExpressionNode ParseConstExpression()
    {
        var token = NextToken();
        return new ConstExpression(token);
    }

    private ExpressionNode ParseListInitializtionExpression()
    {
        //Allow list initialization like [] or [1,2,3,4]?
        if(Current.Kind == TBracketLeft && Peek(1).Kind == TBracketRight)
            return ParseEmptyListInitializationExpression();
        
        var bracketOpen = MatchKind(TBracketLeft);
        var staticElements = new List<ExpressionNode>();
        var isReading = true;
        while (isReading && NotEndOfFile)
        {
            var next = ParseExpression();
            staticElements.Add(next);
            if (Current.Kind != TComma)
            {
                isReading = false;
            }
            else
            {
                var comma = NextToken();
            }
        }
        var bracketClose = MatchKind(TBracketRight);
        return new ListInitExpression(bracketOpen, staticElements, bracketClose);
    }

    private ExpressionNode ParseEmptyListInitializationExpression()
    {
        var open = MatchKind(TBracketLeft);
        var close = MatchKind(TBracketRight);
        TryParseType(out TypeSyntaxNode? type);
        return new ListInitExpression(open, close, type);
    }

    private ExpressionNode ParseCallExpression(SyntaxToken called)
    {
        //var nameToken = called;
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
        return new CallExpression(called, openPar, args, closePar);
    }

    private TypeSyntaxNode ParseType()
    {
        var type = MatchKinds(TInt, TIdentifier, TBool, TString); //MatchKinds can take many arguments, so MatchKinds(TInt, TBool, TStr) would match those 3 :D
        TypeSyntaxNode typ = type.Kind switch 
        {
            TInt => new TypeSyntaxInt(type.Location),
            TBool => new TypeSyntaxBool(type.Location),
            TString => new TypeSyntaxString(type.Location),
            TIdentifier => new TypeSyntaxUserDefined(type), //user-defined types
            _ => new BadTypeSyntax(type.Location)
        };
        while(Current.Kind == TBracketLeft)
        {
            var bracketOpen = NextToken();
            var bracketClose = MatchKind(TBracketRight);
            var location = TextLocation.FromTo(type.Location, bracketClose.Location);
            typ = new TypeSyntaxList(location, typ);
        }
        return typ;
    }

    private bool TryParseType(out TypeSyntaxNode? type)
    {
        var checkpoint = currentToken;
        type = null;
        if(Current.Kind is not TInt or TIdentifier)
            return false;
        var typeToken = NextToken();
        TypeSyntaxNode typ = typeToken.Kind switch 
        {
            TInt => new TypeSyntaxInt(typeToken.Location),
            TIdentifier => new TypeSyntaxUserDefined(typeToken), //user-defined types
            _ => new BadTypeSyntax(typeToken.Location)
        };
        while(Current.Kind == TBracketLeft)
        {
            var bracketOpen = NextToken();
            if(Current.Kind != TBracketRight)
            {
                currentToken = checkpoint;
                return false;
            }
            var bracketClose = NextToken();
            var location = TextLocation.FromTo(typeToken.Location, bracketClose.Location);
            typ = new TypeSyntaxList(location, typ);
        }
        type = typ;
        return true;
    }

    
}