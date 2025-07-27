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
        if (index >= tokens.Count)
            return tokens[^1];
        return tokens[index];
    }

    private SyntaxToken NextToken()
    {
        return tokens[currentToken++];
    }

    private SyntaxToken MatchKind(TokenKind kind)
    {
        if (Current.Kind == kind)
        {
            return NextToken();
        }
        _diag.ReportUnexpectedToken(kind, Current.Kind, Current.Location);
        return new SyntaxToken(TBadToken, Current.Location);
    }

    private SyntaxToken MatchKinds(params TokenKind[] kinds) => MatchKinds(kinds);

    private SyntaxToken MatchKinds(IEnumerable<TokenKind> kinds)
    {
        if (kinds.Contains(Current.Kind))
        {
            return NextToken();
        }
        _diag.ReportUnexpectedTokenFromMany(kinds.ToArray(), Current.Kind, Current.Location);
        return new SyntaxToken(TBadToken, Current.Location);
    }

    private int GetCheckpoint() => currentToken;
    private void RestoreCheckpoint(int checkpoint) => currentToken = checkpoint;

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
        while (NotEndOfFile)
        {
            var statement = ParseTopLevelNode();
            statements.Add(statement);
        }
        TextLocation location;
        if (statements.Count > 0)
            location = TextLocation.FromTo(statements[0].Location, statements[^1].Location);
        else
            location = TextLocation.EmptyFile;
        return new ASTRoot(statements, location);
    }

    private TopLevelNode ParseTopLevelNode()
    {
        switch (Current.Kind)
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

    private TopLevelNode ParseTypeDeclaration()
    {
        var typeToken = NextToken();
        var nameToken = MatchKind(TIdentifier);

        List<TypeSyntaxParameterType>? typeParams = null;
        if (Current.Kind == TLessThan) typeParams = ParseTypeParameters();

        MatchKind(TEqual);

        if (Current.Kind != TCurLeft)
        {
            ParseType(); //Eat whatever is being aliased
            MatchKind(TSemiColon); //Eat semicolon?
            var loc = TextLocation.FromTo(typeToken.Location, Current.Location);
            _diag.Report("Parser doesn't support alias (yet)", isError: true, loc);
            return new TopLevelError(loc);
        }

        var fields = new List<FieldDeclaration>();
        var curlOpen = MatchKind(TCurLeft);
        while (NotEndOfFile && Current.Kind != TCurRight)
        {
            var type = ParseType();
            var name = MatchKind(TIdentifier);
            fields.Add(new(type, name));
            var semicolon = MatchKind(TSemiColon);
        }
        var curlClose = MatchKind(TCurRight);
        return new TopLevelTypeDeclaration(typeToken, nameToken, typeParams, curlOpen, fields, curlClose);
    }

    private StatementNode ParseStatement()
    {
        return Current.Kind switch
        {
            TCurLeft => ParseBlockStatement(),
            TIf => ParseIfStatement(),
            TWhile => ParseWhileStatement(),
            TReturn => ParseReturnStatement(),
            TFunc => ParseFunctionDeclaration(),
            TVar => ParseVariableDeclaration(null),
            _ when IsStartOfVariableDeclaration(out var type) => ParseVariableDeclaration(type),
            TType => ParseStatementError(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementNode ParseReturnStatement()
    {
        var returnToken = MatchKind(TReturn);
        ExpressionNode? expr = null;
        if (Current.Kind != TSemiColon)
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
        if (Current.Kind == TColon) //Then it looks like "while condition : cont(,cont)*" 
        {
            var colon = MatchKind(TColon);
            var parseCont = true;
            do
            {
                var expr = ParseExpression();
                exprList.Add(expr);
                if (Current.Kind == TComma)
                {
                    var comma = NextToken();
                }
                else
                {
                    parseCont = false;
                }
            } while (NotEndOfFile && parseCont);
        }
        var tokenBeforeBody = Current;
        var body = ParseBlockStatement();
        if (tokenBeforeBody.Kind != TCurLeft)
        {
            _diag.ReportWhileExpectedBlockStatement(tokenBeforeBody);
        }
        return new WhileStatement(whileToken, condition, body, exprList);
    }

    private StatementNode ParseIfStatement()
    {
        var ifToken = MatchKind(TIf);
        var condition = ParseExpression();
        var thenStmnt = ParseBlockStatement();
        if (Current.Kind != TElse) //Could be an EOF if the statement looks like "if <cond> then <stmnt>"
        {
            return new IfStatement(ifToken, condition, thenStmnt, null);
        }
        var elseToken = MatchKind(TElse);
        StatementNode elseStmnt;
        if (Current.Kind != TCurLeft)
        {
            if (Current.Kind != TIf)
                _diag.ReportExpectedIfStatement(Current);
            elseStmnt = ParseIfStatement();
        }
        else
        {
            elseStmnt = ParseBlockStatement();
        }
        return new IfStatement(ifToken, condition, thenStmnt, elseStmnt);
    }

    private StatementNode ParseBlockStatement()
    {
        var statements = new List<StatementNode>();
        var open = MatchKind(TCurLeft);
        while (NotEndOfFile && Current.Kind != TCurRight)
        {
            var statement = ParseStatement();
            statements.Add(statement);
        }
        var close = MatchKind(TCurRight);
        return new BlockStatement(open, statements, close);
    }

    private StatementNode ParseVariableDeclaration(TypeSyntaxNode? type)
    {
        TextLocation loc;
        if (type is null)
        {
            var varToken = NextToken();
            loc = varToken.Location;
        }
        else
        {
            loc = type.Location;
        }
        var name = MatchKind(TIdentifier);
        var equal = NextToken(); // throw away the '='
        var rhs = ParseExpression(); //Parse the right hand side of a "int x = rhs"
        var semicolon = MatchKind(TSemiColon);
        return new VarDeclaration(loc, type, name, equal, rhs);
    }


    private StatementNode ParseFunctionDeclaration()
    {
        var funcKeyword = MatchKind(TFunc);

        if (TryParseType(out var toExtend, out var consumedTokens) && Current.Kind == TDot)
        {
            MatchKind(TDot);
        }
        else
        {
            toExtend = null;
            currentToken -= consumedTokens;
        }

        var name = MatchKind(TIdentifier);

        //case id<T>()
        var typeParams = ParseTypeParameters();

        var _ = MatchKind(TParLeft);
        //Params are tuples of:  "function myFunc(int x, int y)" -> (int, x) -> (ParameterType, ParameterName)
        var parameters = ParseParameterList();
        TypeSyntaxNode? returnType = null;
        if (Current.Kind != TCurLeft)
        {
            returnType = ParseType();
        }
        var body = (BlockStatement)ParseBlockStatement();
        return new FuncDeclaration(toExtend, funcKeyword, name, typeParams, parameters, returnType, body);
    }

    private List<TypeSyntaxParameterType> ParseTypeParameters()
    {
        var typeParams = new List<TypeSyntaxParameterType>();
        if (Current.Kind == TLessThan)
        {
            var openAngle = NextToken();
            var parseTypeParameters = true;
            while (parseTypeParameters && NotEndOfFile)
            {
                var typeParamName = MatchKind(TIdentifier);
                typeParams.Add(new(typeParamName));
                if (Current.Kind == TComma) { NextToken(); }
                else parseTypeParameters = false;
            }
            var closeAngle = MatchKind(TGreaterThan);
        }
        return typeParams;
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

    private StatementNode ParseStatementError()
    {
        //TODO: Other types of statement errors?
        //TODO: Is this really the way we want to deal with this? Is there a more elegant route of letting the binder?
        var errorToken = Current;
        var typeDecl = ParseTypeDeclaration();
        _diag.ReportKeywordOnlyAllowedInTopScope(errorToken.Kind, typeDecl.Location);
        return new ErrorStatement(typeDecl.Location);
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
        if (Current.Kind.IsPrefixUnaryOperator())
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
        switch (Current.Kind)
        {
            case TEqual:
                {
                    var op = NextToken();
                    var rhs = ParseSubExpression();
                    return new AssignmentExpression(left, op, rhs);
                }
            default:
                return ParseBinaryExpression(left, parentPrecedence);
        }
    }

    private ExpressionNode ParseBinaryExpression(ExpressionNode left, int parentPrecedence = 0)
    {
        while (true)
        {
            var precedence = Current.Kind.GetBinaryPrecedence();
            if (precedence == -1 || precedence <= parentPrecedence)
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
        switch (Current.Kind)
        {
            case TStringLiteral:
            case TTrue:
            case TFalse:
            case TConst:
                {
                    res = ParseConstExpression();
                }
                break;
            case TNull:
                {
                    var nullToken = NextToken();
                    res = new NullExpression(nullToken);
                }
                break;
            case TParLeft:
                {
                    //Check if it is a lambda (A a) => ...;
                    //Likely to be some sort of precendence needed here because
                    // (A a) => a + 2 is NOT ((A a) => a) + 2;
                    if (IsPossibleLambdaExpression())
                    {
                        res = ParseLambdaExpression();
                    }
                    else
                    {
                        res = ParseParenthesesExpression();
                    }
                }
                break;
            case TBracketLeft:
                {
                    res = ParseListInitializtionExpression();
                }
                break;
            case TBool or TInt or TString:
                {
                    var typeToken = ParseType();
                    res = new AccessExpression(new AccessPredefinedType(typeToken));
                }
                break;
            case TIdentifier:
                {
                    var nameToken = NextToken();
                    res = new AccessExpression(new NameAccess(nameToken));
                }
                break;
            case TNew:
                {
                    res = ParseObjectInitializer();
                }
                break;
            default:
                {
                    var nextToken = Current.Kind == TEOF ? Current : NextToken();
                    _diag.ReportInvalidExpression(nextToken);
                    return new ErrorExpression(nextToken);
                }
        }
        return ParsePostfixExpression(res);
    }

    private ExpressionNode ParsePostfixExpression(ExpressionNode res)
    {
        //TODO: how do we account for operator precedence? What if I want a postfix unary operator with some precedence?
        while (true)
        {
            switch (Current.Kind)
            {
                case TParLeft:
                    {
                        res = ParseCallExpression(res);
                    }
                    continue;
                case TLessThan:
                    {
                        if (TryParseTypeArguments(out var typeArgs, end: TParLeft))
                        {
                            var (openAngle, args, closeAngle) = typeArgs.Value;
                            var resAccess = AccessFromExpression(res);
                            res = new TypeApplication(resAccess, openAngle, args, closeAngle);
                        }
                        else return res;
                    }
                    continue;
                case TBracketLeft:
                    {
                        var open = MatchKind(TBracketLeft);
                        var expr = ParseExpression();
                        var close = MatchKind(TBracketRight);
                        var subscript = new SubscriptAccess(AccessFromExpression(res), expr);
                        res = new AccessExpression(subscript);
                    }
                    continue;
                case TDot:
                    {
                        var dot = MatchKind(TDot);
                        var member = MatchKind(TIdentifier);
                        var memberAcess = new MemberAccess(AccessFromExpression(res), member);
                        res = new AccessExpression(memberAcess);
                    }
                    continue;
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
        if (Current.Kind == TBracketLeft && Peek(1).Kind == TBracketRight)
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
        if (Current.Kind != TParRight)
        {
            var isReadingArgs = true;
            while (isReadingArgs && NotEndOfFile)
            //&& Current.Kind != TParRight) //removed, so we don't allow -> stuff like "myFunc(2,)"
            {
                var arg = ParseExpression();
                args.Add(arg);
                if (Current.Kind == TComma)
                {
                    var comma = MatchKind(TComma);
                }
                else
                {
                    isReadingArgs = false;
                }
            }
        }

        var closePar = MatchKind(TParRight);
        return new CallExpression(called, openPar, args, closePar);
    }

    private ExpressionNode ParseObjectInitializer()
    {
        MatchKind(TNew);
        var nameToken = ParseType(); //MatchKind(TIdentifier);
        var curLeft = MatchKind(TCurLeft);
        var values = new List<(SyntaxToken, ExpressionNode)>();
        var isReading = Current.Kind != TCurRight;
        while (NotEndOfFile && isReading)
        {
            var lhs = MatchKind(TIdentifier);
            MatchKind(TEqual);
            var rhs = ParseExpression();
            values.Add((lhs, rhs));
            isReading = Current.Kind == TComma && NextToken().Kind == TComma;
        }
        var curRight = MatchKind(TCurRight);
        return new ObjectInitExpression(nameToken, curLeft, values, curRight);
    }

    private TypeSyntaxNode ParseType()
    {
        if (!TryParseType(out var type, out _, force: true))
            throw new Exception($"{nameof(ParseType)} - failed to parse a type. Something is wrong with the parser");
        return type!;
    }

    private bool TryParseType([NotNullWhen(true)] out TypeSyntaxNode? type) => TryParseType(out type, out var _);
    private bool TryParseType([NotNullWhen(true)] out TypeSyntaxNode? type, out int consumedTokens, bool force = false)
    {
        var checkpoint = currentToken;
        consumedTokens = 0;
        if (!force && !Current.Kind.IsPossibleType())
        {
            type = null;
            return false;
        }
        var typeToken = MatchKinds(TokenKindExtension.GetPossibleTypeKinds());
        type = TypeSyntaxNode.FromSyntaxToken(typeToken);

        if (Current.Kind == TLessThan) //Parse: type<
        {
            if (TryParseTypeArguments(out var typeArgs))
            {
                var (angleOpen, typeArguments, angleClose) = typeArgs.Value;
                type = new TypeSyntaxTypeApplication(type, angleOpen, typeArguments, angleClose);
            }
        }

        while (NotEndOfFile && Current.Kind == TBracketLeft)
        {
            var bracketOpen = NextToken();
            SyntaxToken bracketClose;
            if (force)
            {
                bracketClose = MatchKind(TBracketRight);
            }
            else
            {
                if (Current.Kind != TBracketRight)
                {
                    currentToken = checkpoint;
                    return false;
                }
                bracketClose = NextToken();
            }
            var location = TextLocation.FromTo(typeToken.Location, bracketClose.Location);
            type = new TypeSyntaxList(location, type);
        }
        consumedTokens = currentToken - checkpoint;
        return true;
    }

    private bool IsStartOfVariableDeclaration([NotNullWhen(true)] out TypeSyntaxNode? type)
    {
        type = null;
        var checkpoint = currentToken;
        if (!TryParseType(out var typ)) return false;

        if (Current.Kind != TIdentifier && Peek(1).Kind != TEqual)
        {
            currentToken = checkpoint;
            return false;
        }
        type = typ;
        return true;
    }

    private static Access AccessFromExpression(ExpressionNode expr) => expr is AccessExpression ae ? ae.Access : new ExprAccess(expr);

    private bool TryParseTypeArguments(
        [NotNullWhen(true)] out (SyntaxToken AngleOpen, List<TypeSyntaxNode> TypeArguments, SyntaxToken AngleClose)? res,
        TokenKind? end = null
    )
    {
        var checkpoint = currentToken;
        res = null;
        if (Current.Kind != TLessThan) return false;

        var openAngle = NextToken();
        var typeParams = new List<TypeSyntaxNode>();
        var ateSome = false;
        while (TryParseType(out TypeSyntaxNode? type) && NotEndOfFile)
        {
            typeParams.Add(type);
            ateSome = true;
            if (Current.Kind != TComma) break;
            MatchKind(TComma);
        }
        if (ateSome && Current.Kind == TGreaterThan
           && (end is null || (end is not null && Peek(1).Kind == end)))
        {
            var closeAngle = NextToken();
            res = (openAngle, typeParams, closeAngle);
            return true;
        }
        currentToken = checkpoint;
        return false;
    }

    private bool IsPossibleLambdaExpression()
    {
        var checkpoint = GetCheckpoint();
        if (Current.Kind != TParLeft) return false;
        
        _ = NextToken();
        int depth = 1;
        //Consume any token even if (a) or (A a, A a) or ((a) => a) ...
        while (depth > 0 && NotEndOfFile)
        {
            if (Current.Kind is TParLeft) depth++;
            if (Current.Kind is TParRight) depth--;
            _ = NextToken();
        }

        if (Current.Kind is not TArrow)
        {
            RestoreCheckpoint(checkpoint);
            return false;
        }
        RestoreCheckpoint(checkpoint);
        return true;
    }

    private ExpressionNode ParseLambdaExpression()
    {
        //We could probably split here into specified vs. not, but let's just use null for that ?
        var parameters = new List<(TypeSyntaxNode?, SyntaxToken)>();
        var openPar = MatchKind(TParLeft);
        var depth = 1;
        var isReading = true;
        while (isReading && NotEndOfFile)
        {
            if (Current.Kind is TParLeft) depth++;
            if (Current.Kind is TParRight && --depth == 0) break;

            //Provide a prettier error message here - if a user puts (a,b,c,) => ... 
            TypeSyntaxNode? type = null;
            if (Peek(1).Kind is not TComma and not TParRight && TryParseType(out type)) { }
            var name = MatchKind(TIdentifier);
            if(name.Kind is not TBadToken) parameters.Add((type, name));
            if (Current.Kind is not TComma) isReading = false;
            else
            {
                NextToken(); //Consume the comma
                if (Current.Kind is TParRight) _diag.ReportTrailingCommaInParameterList(Current.Location);
            }
        }

        _ = MatchKind(TParRight);
        _ = MatchKind(TArrow);

        var lambdaBody = ParseExpression();
        var location = TextLocation.FromTo(openPar.Location, lambdaBody.Location);
        return new LambdaExpression(location, parameters, lambdaBody);
    }
}