namespace WarmLangCompiler.Binding;

using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.ErrorReporting;

public sealed class Binder
{
    private readonly ErrorWarrningBag _diag;
    private readonly BoundSymbolScope _scope;
    public Binder(ErrorWarrningBag bag)
    {
        _diag = bag;
        _scope = new BoundSymbolScope();
    }

    public BoundProgram BindProgram(ASTNode root)
    {
        if(root is BlockStatement statement)
        {
            var bound = BindBlockStatement(statement);
            return new BoundProgram(bound);
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
        var name = varDecl.Name;
        var rightHandSide = BindTypeConversion(varDecl.RightHandSide, type);
       
        var variable = new VariableSymbol(name, rightHandSide.Type);
        if(!_scope.TryDeclareVariable(variable))
        {
            _diag.ReportVariableAlreadyDeclared(name);
            return new BoundErrorStatement(varDecl);
        }
        return new BoundVarDeclaration(varDecl, name, rightHandSide);
    }

    private BoundStatement BindFunctionDeclaration(FuncDeclaration funcDecl)
    {
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
        var uniqueParameterNames = new HashSet<string>();
        foreach(var @param in funcDecl.Params)
        {
            var paramType = @param.type.ToTypeSymbol();
            var paramName = @param.name;
            if(uniqueParameterNames.Contains(paramName))
            {
                _diag.ReportParameterDuplicateName(paramName);
            } else
            {
                parameters.Add(new ParameterSymbol(paramName, paramType));
            }
        }
        
        foreach(var @param in parameters)
        {
            var var = new VariableSymbol(@param.Name, @param.Type);
            _scope.TryDeclareVariable(var);
        }
        var body = BindBlockStatement(funcDecl.Body);
    
        var returnType = funcDecl.ReturnType.ToTypeSymbol();
        var function = new FunctionSymbol(funcDecl.Name, parameters.ToImmutable(), returnType, body);
        if(!_scope.TryDeclareFunction(function))
        {
            _diag.ReportFunctionAlreadyDeclared(function.Name);
            return new BoundErrorStatement(funcDecl);
        }
        return new BoundFunctionDeclaration(funcDecl, function);
    }

    private BoundStatement BindIfStatement(IfStatement ifStatement)
    {
        var condition = BindExpression(ifStatement.Condition);
        if(condition.Type != TypeSymbol.Bool)
        {
            _diag.ReportCannotImplicitlyConvertToType(TypeSymbol.Bool, condition.Type);
            return new BoundErrorStatement(ifStatement);
        }
        _scope.PushScope();
        var trueBranch = BindStatement(ifStatement.Then);
        var falseBranch = ifStatement.Else is null ? null : BindStatement(ifStatement.Else);
        _scope.PopScope();
        return new BoundIfStatement(ifStatement, condition, trueBranch, falseBranch);
    }

    private BoundStatement BindWhileStatement(WhileStatement wile)
    {
        var condition = BindExpression(wile.Condition);
        if(condition.Type != TypeSymbol.Bool)
        {
            _diag.ReportCannotImplicitlyConvertToType(TypeSymbol.Bool, condition.Type);
            return new BoundErrorStatement(wile);
        }
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

    private BoundStatement BindExprStatement(ExprStatement expr)
    {
        var bound = BindExpression(expr.Expression);
        return new BoundExprStatement(expr, bound);
    }

    private BoundExpression BindExpression(ExpressionNode expression)
    {
        return expression switch
        {
            CallExpression ce => BindCallExpression(ce),
            AccessExpression ae => BindAccessExpression(ae),
            UnaryExpression ue => BindUnaryExpression(ue),
            BinaryExpression be => BindBinaryExpression(be),
            ListInitExpression le => BindListInitExpression(le),
            ConstExpression ce => BindConstantExpression(ce),
            AssignmentExpression assignment => BindAssignmentExpression(assignment),
            ErrorExpressionNode => new BoundErrorExpression(expression),
            _ => throw new NotImplementedException($"Bind expression failed on {expression.Kind}")
        };
    }

    private BoundExpression BindAssignmentExpression(AssignmentExpression assignment)
    {
        var boundAccess = BindAccess(assignment.Access);
        var boundRightHandSide = BindExpression(assignment.RightHandSide);
        if(boundAccess.Type != boundRightHandSide.Type)
        {
            _diag.ReportCannotImplicitlyConvertToType(boundAccess.Type, boundRightHandSide.Type);
            return new BoundErrorExpression(assignment);
        }
        return new BoundAssignmentExpression(assignment, boundAccess, boundRightHandSide);
    }

    private BoundExpression BindCallExpression(CallExpression ce)
    {
        var arguments = ImmutableArray.CreateBuilder<BoundExpression>(ce.Arguments.Count);
        foreach(var arg in ce.Arguments)
        {
            var bound = BindExpression(arg);
            arguments.Add(bound);
        }
        var functionName = ce.Called.Name!;
        if(!_scope.TryLookup(functionName, out var symbol))
        {
            _diag.ReportNameDoesNotExist(functionName);
            return new BoundErrorExpression(ce);    
        }

        if (symbol is not FunctionSymbol function)
        {
            _diag.ReportNameIsNotAFunction(functionName);
            return new BoundErrorExpression(ce);
        }
        if(arguments.Count != function.Parameters.Length)
        {
            _diag.ReportFunctionCalMissingArguments(functionName, function.Parameters.Length, arguments.Count);
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
                _diag.ReportNameDoesNotExist(na.Name);
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
                    _diag.ReportCannotSubscriptIntoType(boundTarget.Type);
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
        var boundOperator = BoundUnaryOperator.Bind(ue.Kind, bound);
        if(boundOperator is null)
        {
            _diag.ReportUnaryOperatorCannotBeApplied(ue.Operator, bound.Type);
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

        var boundOperator = BoundBinaryOperator.Bind(binaryExpr.Kind, boundLeft, boundRight);
        if(boundOperator is null)
        {
            _diag.ReportBinaryOperatorCannotBeApplied(binaryExpr.Operator, boundLeft.Type, boundRight.Type);
            return new BoundErrorExpression(binaryExpr);
        }
        return new BoundBinaryExpression(binaryExpr,boundLeft, boundOperator, boundRight);
    }

    private BoundExpression BindListInitExpression(ListInitExpression le)
    {
        var elements = ImmutableArray.CreateBuilder<BoundExpression>(le.Elements.Count);
        foreach(var elm in le.Elements)
        {
            var bound = BindExpression(elm);
            elements.Add(bound);
        }
        if(elements.Count > 0)
        {
            var fstType = elements[0].Type;
            var listType = new ListTypeSymbol($"list<{fstType}>", fstType);
            return new BoundListExpression(le, listType, elements.MoveToImmutable());
        }
        return new BoundListExpression(le, TypeSymbol.EmptyList, elements.MoveToImmutable());
    }

    private BoundExpression BindConstantExpression(ConstExpression ce)
    {
        var type = TypeSymbol.Int; //TODO: more constants?
        return new BoundConstantExpression(ce, type);
    }

    private BoundExpression BindTypeConversion(ExpressionNode expr, TypeSymbol to)
    {
        var boundExpression = BindExpression(expr);
        return BindTypeConversion(boundExpression, to);
    }

    private BoundExpression BindTypeConversion(BoundExpression expr, TypeSymbol to)
    {
        var conversion = Conversion.GetConversion(expr.Type, to);
        if(!conversion.Exists)
        {
            if(to != TypeSymbol.Error && expr.Type != TypeSymbol.Error)
            {
                _diag.ReportCannotConvertToType(to,expr.Type);
            }
            return new BoundErrorExpression(expr.Node);
        }
        if(to != expr.Type && conversion.IsExplicit)
        {
            _diag.ReportCannotImplicitlyConvertToType(to, expr.Type);
            return new BoundErrorExpression(expr.Node);
        }
        return new BoundTypeConversionExpression(expr.Node, to, expr);
    }
}
