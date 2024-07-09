namespace WarmLangCompiler.Binding;

using System.Collections.Immutable;
using WarmLangCompiler.Binding.ControlFlow;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.ErrorReporting;

public sealed class Binder
{
    private readonly ErrorWarrningBag _diag;
    private readonly BoundSymbolScope _scope;

    /// <summary>
    /// Used for binding functions - to allow local functions.
    /// The first pass through of syntax tree only considers function "definitions", their headers.
    /// This means, we don't know anything about local functions when binding function bodies.
    /// </summary>
    private bool _isGlobalScope;

    private readonly Stack<FunctionSymbol> _functionStack;
    public Binder(ErrorWarrningBag bag)
    {
        _diag = bag;
        _scope = new BoundSymbolScope();
        _isGlobalScope = true;
        _functionStack = new();
    }

    public BoundProgram BindProgram(ASTNode root)
    {
        _isGlobalScope = true;
        if(root is BlockStatement statement)
        {
            var bound = Lowerer.LowerProgram(BindBlockStatement(statement));
            _isGlobalScope = false; 
            var functions = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            foreach(var function in _scope.GetFunctions())
            {
                var boundBody = BindFunctionBody(function);
                functions.Add(function, boundBody);
            }
            return new BoundProgram(bound, functions.ToImmutable());
        }
        else 
            throw new NotImplementedException("Binder only allows root to be BlockStatements");
    }

    private BoundStatement BindStatement(StatementNode statement)
    {
        return statement switch 
        {
            BlockStatement st => BindBlockStatement(st),
            VarDeclaration varDecl => BindVarDeclaration(varDecl),
            FuncDeclaration funcDecl => BindFunctionDeclaration(funcDecl),
            IfStatement ifStatement => BindIfStatement(ifStatement),
            WhileStatement wile => BindWhileStatement(wile),
            ReturnStatement ret => BindReturnStatement(ret),
            ExprStatement expr => BindExprStatement(expr),
            _ => throw new NotImplementedException($"Bind statement for {statement}"),
        };
    }

    private BoundBlockStatement BindBlockStatement(BlockStatement st)
    {
        var boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();
        _scope.PushScope();
        foreach(var stmnt in st.Children)
        {
            var bound = BindStatement(stmnt);
            boundStatements.Add(bound);
        }
        _scope.PopScope();
        return new BoundBlockStatement(st, boundStatements.ToImmutable());
    }

    private BoundStatement BindVarDeclaration(VarDeclaration varDecl)
    {
        var type = varDecl.Type.ToTypeSymbol();
        var name = varDecl.Identifier.Name!;
        var rightHandSide = BindTypeConversion(varDecl.RightHandSide, type, allowimplicitListType: true);

        var variable = new VariableSymbol(name, rightHandSide.Type);
        if(!_scope.TryDeclareVariable(variable))
        {
            _diag.ReportNameAlreadyDeclared(varDecl.Identifier);
            return new BoundErrorStatement(varDecl);
        }
        return new BoundVarDeclaration(varDecl, variable, rightHandSide);
    }

    private BoundStatement BindFunctionDeclaration(FuncDeclaration funcDecl)
    {
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
        var uniqueParameterNames = new HashSet<string>();
        foreach(var (type, name) in funcDecl.Params)
        {
            var paramType = type.ToTypeSymbol();
            var paramName = name.Name ?? throw new Exception($"BINDER: NO NAME FOR PARAMETER {name.Location}");
            if(uniqueParameterNames.Contains(paramName))
            {
                _diag.ReportParameterDuplicateName(name);
            } else
            {
                parameters.Add(new ParameterSymbol(paramName, paramType));
            }
        }

        var returnType = funcDecl.ReturnType.ToTypeSymbol();
        var function = _isGlobalScope 
                       ? new FunctionSymbol(funcDecl, parameters.ToImmutable(), returnType)
                       : new LocalFunctionSymbol(funcDecl, parameters.ToImmutable(), returnType);
        if(!_scope.TryDeclareFunction(function))
        {
            _diag.ReportNameAlreadyDeclared(funcDecl.NameToken);
            return new BoundErrorStatement(funcDecl);
        }
        if(!_isGlobalScope && function is LocalFunctionSymbol f)
        {
            var boundBody = BindFunctionBody(function, isGlobalFunc: false);
            f.Body = boundBody;
        }
        return new BoundFunctionDeclaration(funcDecl, function);
    }

    private BoundBlockStatement BindFunctionBody(FunctionSymbol function, bool isGlobalFunc = true)
    {
        _functionStack.Push(function);
        foreach(var @param in function.Parameters)
        {
            _scope.TryDeclareVariable(@param);
        }
        var boundBody = BindBlockStatement(function.Declaration.Body);
        // if(isGlobalFunc) //Little bit of a waste ... but the LowerBody call also inserts a return if it is missing
        boundBody = Lowerer.LowerBody(function, boundBody);
        if(!ControlFlowGraph.AllPathsReturn(boundBody))
            _diag.ReportNotAllCodePathsReturn(function);
        _functionStack.Pop();
        return boundBody;
    }

    private BoundStatement BindIfStatement(IfStatement ifStatement)
    {
        var condition = BindExpression(ifStatement.Condition, TypeSymbol.Bool);
        _scope.PushScope();
        var trueBranch = BindStatement(ifStatement.Then);
        var falseBranch = ifStatement.Else is null ? null : BindStatement(ifStatement.Else);
        _scope.PopScope();
        return new BoundIfStatement(ifStatement, condition, trueBranch, falseBranch);
    }

    private BoundStatement BindWhileStatement(WhileStatement wile)
    {
        var condition = BindExpression(wile.Condition, TypeSymbol.Bool);
        var boundContinue = ImmutableArray.CreateBuilder<BoundExpression>(wile.Continue.Count);
        foreach(var cont in wile.Continue)
        {
            var boundCont = BindExpression(cont);
            boundContinue.Add(boundCont);
        }
        _scope.PushScope();
        var boundBody = BindStatement(wile.Body);
        _scope.PopScope();
        return new BoundWhileStatement(wile, condition, boundBody, boundContinue.MoveToImmutable());
    }

    private BoundStatement BindReturnStatement(ReturnStatement ret)
    {
        if(_functionStack.Count == 0)
        {
            _diag.ReportCannotReturnOutsideFunction(ret.ReturnToken);
            return new BoundErrorStatement(ret);
        }
        var function = _functionStack.Peek();
        BoundExpression? expr = null;
        
        if(function.Type == TypeSymbol.Void)
        {
            if(ret.Expression is not null)
            {
                _diag.ReportReturnWithValueInVoidFunction(ret.ReturnToken, function);
                return new BoundErrorStatement(ret);
            }
        } else
        {
            //we're in a function that returns some value
            if(ret.Expression is null) //if the return does not contain such a value uh oh
            {
                _diag.ReportReturnIsMissingExpression(ret.ReturnToken, function.Type);
                return new BoundErrorStatement(ret);
            }
            expr = BindTypeConversion(ret.Expression, function.Type);
        }
        return new BoundReturnStatement(ret, expr);
    }

    private BoundStatement BindExprStatement(ExprStatement expr)
    {
        var bound = BindExpression(expr.Expression);
        return new BoundExprStatement(expr, bound);
    }

    private BoundExpression BindExpression(ExpressionNode expression, TypeSymbol targetType, bool allowimplicitListType = false) 
    => BindTypeConversion(expression, targetType, allowimplicitListType);

    private BoundExpression BindExpression(ExpressionNode expression, bool allowimplicitListType = false)
    {
        return expression switch
        {
            CallExpression ce => BindCallExpression(ce),
            AccessExpression ae => BindAccessExpression(ae),
            UnaryExpression ue => BindUnaryExpression(ue),
            BinaryExpression be => BindBinaryExpression(be),
            ListInitExpression le => BindListInitExpression(le, allowimplicitListType),
            ConstExpression ce => BindConstantExpression(ce),
            AssignmentExpression assignment => BindAssignmentExpression(assignment),
            ErrorExpressionNode => new BoundErrorExpression(expression),
            _ => throw new NotImplementedException($"Bind expression failed on ({expression.Location})-'{expression}'")
        };
    }

    private BoundExpression BindAssignmentExpression(AssignmentExpression assignment)
    {
        var boundAccess = BindAccess(assignment.Access);
        if(boundAccess is BoundExprAccess)
        {
            _diag.ReportInvalidLeftSideOfAssignment(assignment.Location);
            return new BoundErrorExpression(assignment);
        }
        var boundRightHandSide = BindTypeConversion(assignment.RightHandSide, boundAccess.Type);
        return new BoundAssignmentExpression(assignment, boundAccess, boundRightHandSide);
    }

    private BoundExpression BindCallExpression(CallExpression ce)
    {   
        //We have reached a cast 'bool(25)' or 'int(true)'
        if(ce.Arguments.Count == 1 && ce.Called.Kind.ToTypeSymbol() is TypeSymbol to)
            return BindTypeConversion(ce.Arguments[0], to, allowExplicit: true);

        var arguments = ImmutableArray.CreateBuilder<BoundExpression>(ce.Arguments.Count);
        foreach(var arg in ce.Arguments)
        {
            var bound = BindExpression(arg);
            arguments.Add(bound);
        }
        var functionName = ce.Called.Name!;
        if(!_scope.TryLookup(functionName, out var symbol))
        {
            _diag.ReportNameDoesNotExist(ce.Called);
            return new BoundErrorExpression(ce);    
        }

        if (symbol is not FunctionSymbol function)
        {
            _diag.ReportNameIsNotAFunction(ce.Called);
            return new BoundErrorExpression(ce);
        }
        if(arguments.Count != function.Parameters.Length)
        {
            _diag.ReportFunctionCallMismatchArguments(ce.Called, function.Parameters.Length, arguments.Count);
            return new BoundErrorExpression(ce);
        }

        for (int i = 0; i < arguments.Count; i++)
        {
            var boundArg = arguments[i];
            var functionParameter = function.Parameters[i];
            arguments[i] = BindTypeConversion(boundArg, functionParameter.Type);
        }
        return new BoundCallExpression(ce, function, arguments.ToImmutable());
    }

    private BoundExpression BindAccessExpression(AccessExpression ae)
    {
        var boundAccess = BindAccess(ae.Access);
        if(boundAccess is BoundInvalidAccess)
        {
            return new BoundErrorExpression(ae);
        }
        return new BoundAccessExpression(ae, boundAccess.Type, boundAccess);
    }

    private BoundAccess BindAccess(Access access)
    {
        switch(access)
        {
            case NameAccess na:
            {
                if(_scope.TryLookup(na.Name, out var symbol) && symbol is not null)
                {
                    return new BoundNameAccess((VariableSymbol)symbol);
                }
                _diag.ReportNameDoesNotExist(na.Location, na.Name);
                return new BoundInvalidAccess();
            }
            case ExprAccess exprAccess: 
            {
                var expr = BindExpression(exprAccess.Expression);
                return new BoundExprAccess(expr);
            }
            case SubscriptAccess sa:
            {
                var boundTarget = BindAccess(sa.Target);
                if(boundTarget.Type is not ListTypeSymbol)
                {
                    _diag.ReportCannotSubscriptIntoType(sa.Location, boundTarget.Type);
                    return new BoundInvalidAccess();
                }
                var boundIndexExpr = BindExpression(sa.Index);
                return new BoundSubscriptAccess(boundTarget, boundIndexExpr);
            }
            case InvalidAccess:
                return new BoundInvalidAccess();
            default:
                throw new NotImplementedException($"Binder doesn't allow: BindAccess of '{access}' yet");
        }
    }

    private BoundExpression BindUnaryExpression(UnaryExpression ue)
    {
        var bound = BindExpression(ue.Expression);
        if(bound.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(ue);
        }
        var boundOperator = BoundUnaryOperator.Bind(ue.Operator.Kind, bound);
        if(boundOperator is null)
        {
            _diag.ReportUnaryOperatorCannotBeApplied(ue.Location, ue.Operator, bound.Type);
            return new BoundErrorExpression(ue);
        }
        return new BoundUnaryExpression(ue, boundOperator, bound);
    }

    private BoundExpression BindBinaryExpression(BinaryExpression binaryExpr)
    {
        var boundLeft = BindExpression(binaryExpr.Left);
        var boundRight = BindExpression(binaryExpr.Right);
        if(boundLeft.Type == TypeSymbol.Error || boundRight.Type == TypeSymbol.Error)
        {
            //this mutes any errors that follow as a result of left/right being errors
            return new BoundErrorExpression(binaryExpr);
        }

        var boundOperator = BoundBinaryOperator.Bind(binaryExpr.Operator.Kind, boundLeft, boundRight);
        if(boundOperator is null)
        {
            _diag.ReportBinaryOperatorCannotBeApplied(binaryExpr.Location, binaryExpr.Operator, boundLeft.Type, boundRight.Type);
            return new BoundErrorExpression(binaryExpr);
        }
        return new BoundBinaryExpression(binaryExpr,boundLeft, boundOperator, boundRight);
    }

    private BoundExpression BindListInitExpression(ListInitExpression le, bool allowimplicitListType)
    {
        if(le.IsEmptyList)
        {
            var type = TypeSymbol.EmptyList;
            if(le.ElementType is null)
            {
                if(!allowimplicitListType)
                    _diag.ReportTypeOfEmptyListMustBeExplicit(le.Location);
            }
            else
                type = new ListTypeSymbol(le.ElementType.ToTypeSymbol());
            return new BoundListExpression(le, type, ImmutableArray<BoundExpression>.Empty);
        }

        var elements = ImmutableArray.CreateBuilder<BoundExpression>(le.Elements.Count);
        TypeSymbol? listType = null;
        foreach(var elm in le.Elements)
        {
            var bound = BindExpression(elm);
            listType ??= bound.Type;
            var conversion = BindTypeConversion(bound, listType);
            elements.Add(conversion);
        }
        var initListType = new ListTypeSymbol(listType!);
        return new BoundListExpression(le, initListType, elements.MoveToImmutable());
    }

    private static BoundExpression BindConstantExpression(ConstExpression ce)
    {
        var type = ce.Value switch 
        {
            int => TypeSymbol.Int,
            bool => TypeSymbol.Bool,
            _ => throw new NotImplementedException($"'{nameof(BindConstantExpression)}' doesn't know about '{ce.Value}'"),
        };
        return new BoundConstantExpression(ce, type);
    }

    private BoundExpression BindTypeConversion(ExpressionNode expr, TypeSymbol to, bool allowExplicit = false, bool allowimplicitListType = false)
    {
        var boundExpression = BindExpression(expr, allowimplicitListType);
        return BindTypeConversion(boundExpression, to, allowExplicit);
    }

    private BoundExpression BindTypeConversion(BoundExpression expr, TypeSymbol to, bool allowExplicit = false)
    {
        var conversion = Conversion.GetConversion(expr.Type, to);
        if(!conversion.Exists)
        {
            if(to != TypeSymbol.Error && expr.Type != TypeSymbol.Error)
            {
                _diag.ReportCannotConvertToType(expr.Location, to,expr.Type);
            }
            return new BoundErrorExpression(expr.Node);
        }
        if(conversion.IsExplicit && !allowExplicit)
        {
            _diag.ReportCannotImplicitlyConvertToType(expr. Location, to, expr.Type);
            return new BoundErrorExpression(expr.Node);
        }
        if(conversion.IsIdentity)
            return expr;
        return new BoundTypeConversionExpression(expr.Node, to, expr);
    }
}
