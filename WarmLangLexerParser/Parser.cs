using System.Diagnostics.CodeAnalysis;
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

    private SyntaxToken MatchKinds(params TokenKind[] kinds) => MatchKinds(kinds);

    private SyntaxToken MatchKinds(IEnumerable<TokenKind> kinds)
    {
        if(kinds.Contains(Current.Kind))
        {
            return NextToken();
        }
        _diag.ReportUnexpectedTokenFromMany(kinds.ToArray(), Current.Kind, Current.Location);
        return new SyntaxToken(TBadToken, Current.Location);
    }

    public ASTNode Parse()
    {
        ASTNode root = ParseEntry();
        //Our language should finish parsing with an End Of File, no?
        var _ = MatchKind(TEOF);
        return root;
    }

    private ASTRoot ParseEntry()
    {
        var statements = new List<TopLevelNode>();
        while(NotEndOfFile)
        {
            var statement = ParseTopLevelNode(); 
            statements.Add(statement);
        }
        TextLocation location;
        if(statements.Count > 0)
            location = TextLocation.FromTo(statements[0].Location, statements[^1].Location);
        else
            location = TextLocation.EmptyFile;
        return new ASTRoot(statements, location);
    }

    private TopLevelNode ParseTopLevelNode()
    {
        switch(Current.Kind)
        {
            case TType:
                return ParseTypeDeclaration();
            default:
                var statement = ParseStatement();
                return statement switch
                {
                    VarDeclaration var => new TopLevelVarDeclaration(var),
                    FuncDeclaration var => new TopLevelFuncDeclaration(var),
                    _ => new TopLevelArbitraryStament(statement),
                };
        }
    }

    private TopLevelTypeDeclaration ParseTypeDeclaration()
    {
        var typeToken = NextToken();
        var nameToken = MatchKind(TIdentifier);
        MatchKind(TEqual);

        if(Current.Kind != TCurLeft) throw new NotImplementedException("Parser doesn't yet support alias!"); //TODO: ALIAS
        
        var members = new List<MemberDeclaration>();
        MatchKind(TCurLeft);
        while(NotEndOfFile && Current.Kind != TCurRight)
        {
            var type = ParseType();
            var name = MatchKind(TIdentifier);
            members.Add(new(type, name));
            var semicolon = MatchKind(TSemiColon);  
        }
        MatchKind(TCurRight);
        return new TopLevelTypeDeclaration(nameToken, members);
    }

    private StatementNode ParseStatement()
    {
        return Current.Kind switch 
        {
            TCurLeft => ParseBlockStatement(),
            TIf => ParseIfStatement(),
            TWhile => ParseWhileStatement(),
            TReturn => ParseReturnStatement(),
            TFunc =>  ParseFunctionDeclaration(),
            _ when IsStartOfVariableDeclaration() => ParseVariableDeclaration(),
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
        TypeSyntaxNode? toExtend = null;
        if(TryPeekPossibleType(out var lengthOfType) && Peek(lengthOfType).Kind == TDot)
        {
            toExtend = ParseType();
            var dot = MatchKind(TDot);
        }
        var name = MatchKind(TIdentifier);
        var _ = MatchKind(TParLeft);
        //Params are tuples of:  "function myFunc(int x, int y)" -> (int, x) -> (ParameterType, ParameterName)
        var parameters = ParseParameterList();
        TypeSyntaxNode? returnType = null;
        if (Current.Kind != TCurLeft)
        {
            returnType = ParseType();
        }
        var body = (BlockStatement)ParseBlockStatement();
        return new FuncDeclaration(toExtend, funcKeyword, name, parameters, returnType, body);
    }

    private List<(TypeSyntaxNode, SyntaxToken)> ParseParameterList()
    {
        List<(TypeSyntaxNode, SyntaxToken)> paramNames = new();
        if (Current.Kind == TParRight)
        {
            var parClose = NextToken();
            return paramNames;
        }
        
        var parseParameter = true;
        while (parseParameter
                //&& Current.Kind != TParRight
                && NotEndOfFile)
        {
            var paramType = ParseType();
            var paramName = MatchKind(TIdentifier);
            paramNames.Add((paramType, paramName));
            if (Current.Kind == TComma)
            {
                var comma = MatchKind(TComma);
            }
            else
            {
                parseParameter = false;
            }
        }
        var paramClose = MatchKind(TParRight);
        return paramNames;
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

        //TODO: Precdence check unecessary?
        if(Current.Kind.IsPrefixUnaryOperator())
        {
            var op = NextToken();
            var expr = ParseSubExpression(precedence);
            left = new UnaryExpression(op, expr);
        }
        else 
        {
            left = ParsePrimaryExpression();
        }
        return ParseContinuedExpression(left, parentPrecedence);
    }

    private ExpressionNode ParseContinuedExpression(ExpressionNode left, int parentPrecedence = 0)
    {
        switch(Current.Kind)
        {
            case TEqual:
            {
                var op = NextToken();
                var rhs = ParseSubExpression();
                return new AssignmentExpression(left, op, rhs);
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
            case TConst: 
            {
                res = ParseConstExpression();
            } break;
            case TParLeft:
            {
                res = ParseParenthesesExpression();
            } break;
            case TBracketLeft:
            {
                res = ParseListInitializtionExpression();
            }
            break;
            case TBool or TInt or TString: 
            {
                var typeToken = ParseType();
                res = new AccessExpression(new AccessPredefinedType(typeToken));
            } break;
            case TIdentifier: 
            {
                var nameToken = NextToken();
                res = new AccessExpression(new NameAccess(nameToken));
            } break;
            case TNew:
            {
                res = ParseStructInitializer();
            } break;
            default: 
            {
                var nextToken = Current.Kind == TEOF ? Current : NextToken();
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
                case TParLeft:
                {
                    res = ParseCallExpression(res);  
                } continue;
                case TBracketLeft:
                {
                    var open = MatchKind(TBracketLeft);
                    var expr = ParseExpression();
                    var close = MatchKind(TBracketRight);
                    var subscript = new SubscriptAccess(AccessFromExpression(res), expr);
                    res = new AccessExpression(subscript);
                } continue;
                case TDot:
                {
                    var dot = MatchKind(TDot);
                    var member = MatchKind(TIdentifier);
                    var memberAcess = new MemberAccess(AccessFromExpression(res), member);
                    res = new AccessExpression(memberAcess);
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

    private ExpressionNode ParseCallExpression(ExpressionNode called) => ParseCallExpression(AccessFromExpression(called));
    private ExpressionNode ParseCallExpression(Access called)
    {
        var openPar = MatchKind(TParLeft);
        var args = new List<ExpressionNode>();
        if(Current.Kind != TParRight)
        {
            var isReadingArgs = true;
            while(isReadingArgs && NotEndOfFile) 
                //&& Current.Kind != TParRight) //removed, so we don't allow -> stuff like "myFunc(2,)"
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

    private ExpressionNode ParseStructInitializer()
    {
        MatchKind(TNew);
        var nameToken = MatchKind(TIdentifier);
        var curLeft = MatchKind(TCurLeft);
        var values = new List<(SyntaxToken, ExpressionNode)>();
        var isReading = Current.Kind != TCurRight;
        while(NotEndOfFile && isReading)
        {
            var lhs = MatchKind(TIdentifier);
            MatchKind(TEqual);
            var rhs = ParseExpression();
            values.Add((lhs,rhs));
            isReading = Current.Kind == TComma && NextToken().Kind == TComma; 
        }
        var curRight = MatchKind(TCurRight);
        return new StructInitExpression(nameToken, curLeft, values, curRight);
    }

    private TypeSyntaxNode ParseType()
    {
        var type = MatchKinds(TokenKindExtension.GetPossibleTypeKinds());
        var typ = TypeSyntaxNode.FromSyntaxToken(type);
        while(Current.Kind == TBracketLeft)
        {
            var bracketOpen = NextToken();
            var bracketClose = MatchKind(TBracketRight);
            var location = TextLocation.FromTo(type.Location, bracketClose.Location);
            typ = new TypeSyntaxList(location, typ);
        }
        return typ;
    }

    private bool TryParseType([NotNullWhen(true)] out TypeSyntaxNode? type)
    {
        var checkpoint = currentToken;
        if(!Current.Kind.IsPossibleType())
        {
            type = null;
            return false;
        }
        var typeToken = NextToken();
        type = TypeSyntaxNode.FromSyntaxToken(typeToken);
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
            type = new TypeSyntaxList(location, type);
        }
        return true;
    }

    private bool IsStartOfVariableDeclaration()
    {
        //TODO: User-defined types, what to do?
        // Get a reset point, try to parse identifier into identifier?
        if(Current.Kind != TIdentifier && Current.Kind.IsPossibleType()) return true;

        var checkpoint = currentToken;
        var ident1 = NextToken();
        var ident2 = NextToken();
        currentToken = checkpoint;

        return ident1.Kind == TIdentifier && ident2.Kind == TIdentifier;
    }

    private bool TryPeekPossibleType(out int typeLengthInTokens)
    {
        typeLengthInTokens = 0;
        if(!Current.Kind.IsPossibleType())
            return false;
        
        var checkpoint = currentToken;
        NextToken();
        typeLengthInTokens++;

        while(Current.Kind == TBracketLeft)
        {
            NextToken();
            if(Current.Kind != TBracketRight)
            {
                typeLengthInTokens = 0;
                break;
            }
            NextToken();
            typeLengthInTokens += 2;
        }
        currentToken = checkpoint;
        return true;
    }

    private Access AccessFromExpression(ExpressionNode expr) => expr is AccessExpression ae ? ae.Access : new ExprAccess(expr); 
    
    
}