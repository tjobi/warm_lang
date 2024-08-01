namespace WarmLangCompiler.Binding;

using System.Collections.Immutable;
using WarmLangCompiler.Binding.ControlFlow;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Binding.BoundAccessing;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.ErrorReporting;

public sealed class Binder
{
    private readonly ErrorWarrningBag _diag;
    private readonly BoundSymbolScope _scope;
    private readonly BinderTypeHelper _typeHelper;
    private readonly Dictionary<FunctionSymbol, BlockStatement> _unBoundBodyOf;
    private readonly Stack<FunctionSymbol> _functionStack;

    public Binder(ErrorWarrningBag bag)
    {
        _diag = bag;
        _scope = new BoundSymbolScope();
        _functionStack = new();
        _unBoundBodyOf = new();
        _typeHelper = new();
        _scope.PushScope(); //Push scope to contain builtin stuff
        foreach(var func in BuiltInFunctions.GetBuiltInFunctions())
        {
            if(!_scope.TryDeclareFunction(func))
                throw new Exception($"Couldn't push built in {func}");
        }
    }

    public BoundProgram BindProgram(ASTNode node)
    {
        if(node is not ASTRoot root)
            throw new NotImplementedException($"Binder only allows root to be '{nameof(ASTRoot)}'");

        var (bound, globalVariables, hasGlobalNonDeclarationStatements) = BindASTRoot(root);

        var functions = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
        foreach(var function in _scope.GetFunctions())
        {
            if(function.IsBuiltInFunction())
                continue;
            var boundBody = BindFunctionBody(function, _unBoundBodyOf[function]);
            functions.Add(function, boundBody);
        }

        foreach(var (type, memberFuncs) in _typeHelper.GetFunctionMembers())
        {
            foreach(var func in memberFuncs)
            {
                var boundBody = BindFunctionBody(func, _unBoundBodyOf[func]);
                _typeHelper.BindMemberFunc(type, func, boundBody);
            }
        }

        FunctionSymbol? main = null;
        FunctionSymbol? scriptMain = null;
        if(functions.Where(kv => kv.Key.Name == "main").FirstOrDefault() is {Key: FunctionSymbol mainSymbol, Value: BoundBlockStatement mainBody})
        {
            main = mainSymbol;
        }
        
        if(hasGlobalNonDeclarationStatements || main is null)
        {  
            if(main is not null)
                _diag.ReportProgramHasBothMainAndTopLevelStatements(main.Location);
            
            scriptMain = FunctionSymbol.CreateMain("__wl_script_main");
            var scriptMainBody = Lowerer.LowerBody(scriptMain, bound);
            functions[scriptMain] = scriptMainBody;
        }

        return new BoundProgram(main, scriptMain, functions.ToImmutable(), _typeHelper.ToTypeMemberInformation(), globalVariables);
    }

    private (BoundBlockStatement boundRoot, ImmutableArray<BoundVarDeclaration> globals, bool hasGlobalArbitraries) BindASTRoot(ASTRoot root)
    {
        var topLevelstatments = ImmutableArray.CreateBuilder<BoundStatement>();
        var globalVariables = ImmutableArray.CreateBuilder<BoundVarDeclaration>(); //NON-function delcarations
        var hasGlobalArbitraries = false;
        foreach(var topLevelFunc in root.Children)
        {
            if(topLevelFunc is TopLevelFuncDeclaration func)
                BindTopLevelStatement(func);
        }
        foreach(var toplevel in root.Children)
        {
            if(toplevel is TopLevelFuncDeclaration)
                continue;
            
            var bound = BindTopLevelStatement(toplevel);
            topLevelstatments.Add(bound);

            if(toplevel is TopLevelArbitraryStament)
                hasGlobalArbitraries = true;
            
            if(bound is BoundVarDeclaration var)
                globalVariables.Add(var);
        }
        //What if we parsed an empty file?
        var rootNode = topLevelstatments.Count > 0 ? topLevelstatments[0].Node : EmptyStatment.Get;
        var boundChildren = Lowerer.LowerProgram(new BoundBlockStatement(rootNode, topLevelstatments.ToImmutable()));
        
        return (boundChildren, globalVariables.ToImmutable(), hasGlobalArbitraries);
    }

    private BoundStatement BindTopLevelStatement(TopLevelStamentNode statement)
    {
        return BindStatement(statement.Statement, isGlobalScope: true);
    }

    private BoundStatement BindStatement(StatementNode statement, bool isGlobalScope = false)
    {
        return statement switch 
        {
            BlockStatement st => BindBlockStatement(st),
            VarDeclaration varDecl => BindVarDeclaration(varDecl, isGlobalScope),
            FuncDeclaration funcDecl => BindFunctionDeclaration(funcDecl, isGlobalScope),
            IfStatement ifStatement => BindIfStatement(ifStatement),
            WhileStatement wile => BindWhileStatement(wile),
            ReturnStatement ret => BindReturnStatement(ret),
            ExprStatement expr => BindExprStatement(expr),
            _ => throw new NotImplementedException($"Bind statement for {statement}"),
        };
    }

    private BoundBlockStatement BindBlockStatement(BlockStatement st, bool pushScope = true)
    {
        var boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();
        if(pushScope)
            _scope.PushScope();
        foreach(var stmnt in st.Children)
        {
            var bound = BindStatement(stmnt);
            boundStatements.Add(bound);
        }
        if(pushScope)
            _scope.PopScope();
        return new BoundBlockStatement(st, boundStatements.ToImmutable());
    }

    private BoundStatement BindVarDeclaration(VarDeclaration varDecl, bool isGlobalScope)
    {
        var type = varDecl.Type.ToTypeSymbol();
        var name = varDecl.Identifier.Name!;
        var rightHandSide = BindTypeConversion(varDecl.RightHandSide, type, allowimplicitListType: true);

        var variable = !isGlobalScope ? new VariableSymbol(name, rightHandSide.Type) : new GlobalVariableSymbol(name, rightHandSide.Type);
        if(!_scope.TryDeclareVariable(variable))
        {
            _diag.ReportNameAlreadyDeclared(varDecl.Identifier);
            return new BoundErrorStatement(varDecl);
        }
        return new BoundVarDeclaration(varDecl, variable, rightHandSide);
    }

    private BoundStatement BindFunctionDeclaration(FuncDeclaration funcDecl, bool isGlobalScope = false)
    {
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
        var uniqueParameterNames = new HashSet<string>();
        for (int i = 0; i < funcDecl.Params.Count; i++)
        {
            var (type, name) = funcDecl.Params[i];
            var paramType = type.ToTypeSymbol();
            //missing names have been reported by the parser
            var paramName = name.Name ?? "NO_NAME"; 
            if(uniqueParameterNames.Contains(paramName))
            {
                _diag.ReportParameterDuplicateName(name);
            } else
            {
                parameters.Add(new ParameterSymbol(paramName, paramType, i));
            }
        }

        var returnType = funcDecl.ReturnType.ToTypeSymbol();
        var nameToken = funcDecl.NameToken;
        var function = isGlobalScope 
                       ? new FunctionSymbol(nameToken, parameters.ToImmutable(), returnType)
                       : new LocalFunctionSymbol(nameToken, parameters.ToImmutable(), returnType);
        
        if(funcDecl.OwnerType is not null)
        {
            var ownerType = funcDecl.OwnerType.ToTypeSymbol();
            if(isGlobalScope)
            {
                var symbol = new MemberFuncSymbol(function);
                _typeHelper.AddMember(ownerType, symbol);
                function.SetOwnerType(ownerType); //TODO: this is quite ugly
                var funcParams = function.Parameters;
                if(funcParams.Length < 1)
                {
                    _diag.ReportMemberFuncNoParameters(function.Location, function.Name, ownerType);
                }
                else if(funcParams[0].Type != ownerType)
                {
                    _diag.ReportMemberFuncFirstParameterMustMatchOwner(function.Location, function.Name, ownerType, funcParams[0].Type);
                }
            } 
            else 
                _diag.ReportLocalMemberFuncDeclaration(funcDecl.NameToken, funcDecl.OwnerType.Location, ownerType);
        }
        if(!function.IsMemberFunc && !_scope.TryDeclareFunction(function))
        {
            _diag.ReportNameAlreadyDeclared(funcDecl.NameToken);
            return new BoundErrorStatement(funcDecl);
        }
        if(!isGlobalScope && function is LocalFunctionSymbol f)
        {
            var boundBody = BindFunctionBody(function, funcDecl.Body);
            f.Body = boundBody;
        } else 
        {
            _unBoundBodyOf[function] = funcDecl.Body;
        }
        return new BoundFunctionDeclaration(funcDecl, function);
    }

    private BoundBlockStatement BindFunctionBody(FunctionSymbol function, BlockStatement body)
    {
        _functionStack.Push(function);
        _scope.PushScope();
        foreach(var @param in function.Parameters)
        {
            _scope.TryDeclareVariable(@param);
        }
        var boundBody = BindBlockStatement(body, pushScope: false);
        boundBody = Lowerer.LowerBody(function, boundBody);
        if(!ControlFlowGraph.AllPathsReturn(boundBody))
            _diag.ReportNotAllCodePathsReturn(function);
        _scope.PopScope();
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
        if(boundAccess is BoundSubscriptAccess sa)
        {
            var targetType = sa.Target.Type;
            if(targetType == TypeSymbol.String)
                _diag.ReportSubscriptTargetIsReadOnly(targetType, assignment.Access.Location);
        }
        var boundRightHandSide = BindTypeConversion(assignment.RightHandSide, boundAccess.Type);
        return new BoundAssignmentExpression(assignment, boundAccess, boundRightHandSide);
    }

    private BoundExpression BindCallExpression(CallExpression ce)
    {
        //We have reached a cast 'bool(25)' or 'int(true)' or 'string(2555)'
        if (ce.Arguments.Count == 1 && ce.Called is AccessPredefinedType predefined)
            return BindTypeConversion(ce.Arguments[0], predefined.Syntax.ToTypeSymbol(), allowExplicit: true);
        
        var function = BindAccessCallExpression(ce, out var accessToCall);
        if(function is null)
            return new BoundErrorExpression(ce);
        
        var arguments = ImmutableArray.CreateBuilder<BoundExpression>(function.Parameters.Length);
        if(function.IsMemberFunc)
        {
            if(accessToCall is not BoundMemberAccess bma)
                throw new Exception($"{nameof(Binder)} - has reached an exception state. Trying to call member function '{function}' on non-member-access '{accessToCall}");
            
            if(bma.Target is not BoundPredefinedTypeAccess && bma.Member is MemberFuncSymbol)
            {
                arguments.Add(new BoundAccessExpression(ce, bma.Target));
            }   
        }

        foreach (var arg in ce.Arguments)
        {
            var bound = BindExpression(arg);
            arguments.Add(bound);
        }

        if(arguments.Count != function.Parameters.Length)
        {
            _diag.ReportFunctionCallMismatchArguments(ce.Called.Location, function.Name, function.Parameters.Length, arguments.Count);
        } 
        else
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                var boundArg = arguments[i];
                var functionParameter = function.Parameters[i];
                arguments[i] = BindTypeConversion(boundArg, functionParameter.Type);
            }
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

    private BoundAccess BindAccess(Access access, bool expectFunc = false)
    {
        switch(access)
        {
            case NameAccess na:
            {
                if(_scope.TryLookup(na.Name, out var symbol) && symbol is not null)
                {
                    if(symbol is VariableSymbol variable)
                        return new BoundNameAccess(variable);
                    if(symbol is FunctionSymbol func)
                    {
                        if(!expectFunc)
                        {
                            _diag.ReportExpectedVariableName(na.Location, na.Name);
                            return new BoundInvalidAccess();
                        }
                        return new BoundFuncAccess(func);
                    }
                }
                _diag.ReportNameDoesNotExist(na.Location, na.Name);
                return new BoundInvalidAccess();
            }
            case AccessPredefinedType predefined: {
                var type = predefined.Syntax.ToTypeSymbol();
                return new BoundPredefinedTypeAccess(type);
            }
            case MemberAccess ma:
            {
                var boundTarget = BindAccess(ma.Target);
                if(boundTarget.Type == TypeSymbol.Error)
                    return new BoundInvalidAccess();
                var boundMember = _typeHelper.FindMember(boundTarget.Type, ma.MemberToken.Name!);
                if(boundMember is null)
                {
                    _diag.ReportCouldNotFindMemberForType(ma.Location, boundTarget.Type, ma.MemberToken.Name);
                    return new BoundInvalidAccess();
                }
                return new BoundMemberAccess(boundTarget, boundMember);
            }
            case ExprAccess exprAccess: 
            {
                var expr = BindExpression(exprAccess.Expression);
                return new BoundExprAccess(expr);
            }
            case SubscriptAccess sa:
            {
                var boundTarget = BindAccess(sa.Target);
                if(boundTarget.Type is not ListTypeSymbol && boundTarget.Type != TypeSymbol.String)
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
            string => TypeSymbol.String,
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

    private FunctionSymbol? BindAccessCallExpression(CallExpression ce, out BoundAccess accessSymbol)
    {
        accessSymbol = BindAccess(ce.Called, expectFunc: true);
        switch(accessSymbol)
        {
            case BoundFuncAccess acc:
                return acc.Func;
            case BoundMemberAccess { Member: MemberFuncSymbol acc}:
                return acc.Function;

            case BoundNameAccess acc:
                _diag.ReportNameIsNotAFunction(ce.Called.Location, acc.Symbol.Name);
                return null;
            case BoundMemberAccess bma:
                _diag.ReportNameIsNotAFunction(ce.Called.Location, bma.Member.Name);
                return null;
            case BoundInvalidAccess:
                return null;
            
            default: 
                //TODO: comeback for higher-order functions
                _diag.ReportExpectedFunctionName(ce.Called.Location);
                return null;
        }
    }
}
