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
    private readonly SymbolEnvironment _scope;
    private readonly BinderTypeScope _typeScope;
    private readonly Dictionary<FunctionSymbol, BlockStatement> _unBoundBodyOf;

    private readonly Stack<FunctionSymbol> _functionStack;

    private readonly Dictionary<FunctionSymbol, BoundBlockStatement> _functionToBody;

    //TODO: limitation comes from the System.Func delegate definitions in mscorlib, 
    //      apperently it can do at most 9 generic parameters (8 parameters + 1 return type)
    private const int MAX_LOCAL_PARAMETERS = 8;

    public Binder(ErrorWarrningBag bag)
    {
        _diag = bag;
        // _typeHelper = new();
        _typeScope = new(_diag);
        _scope = new(_typeScope);
        _functionStack = new();
        _unBoundBodyOf = [];
        _functionToBody = [];

        _scope.PushScope(); //Push scope to contain builtin stuff
        foreach (var func in BuiltInFunctions.GetBuiltInFunctions())
        {
            if (!_scope.TryDeclareFunction(func))
                throw new Exception($"Couldn't push built in {func}");
        }
    }

    public BoundProgram BindProgram(ASTNode node)
    {
        if (node is not ASTRoot root)
            throw new NotImplementedException($"{nameof(Binder)}.{nameof(BindProgram)} only allows root to be '{nameof(ASTRoot)}'");

        var (bound, globalVariables, hasGlobalNonDeclarationStatements) = BindASTRoot(root);

        // var functions = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
        foreach (var function in _scope.GetFunctions())
        {
            if (function.IsBuiltInFunction())
                continue;
            var boundBody = BindFunctionBody(function, _unBoundBodyOf[function]);
            _functionToBody.Add(function, boundBody);
        }

        foreach (var (type, memberFuncs) in _typeScope.GetFunctionMembers())
        {
            foreach (var func in memberFuncs)
            {
                var boundBody = BindFunctionBody(func, _unBoundBodyOf[func]);
                _typeScope.AddMethodBody(type, func, boundBody);
            }
        }

        FunctionSymbol? main = null;
        FunctionSymbol? scriptMain = null;
        if (_functionToBody.Where(kv => kv.Key.Name == "main").FirstOrDefault() is { Key: FunctionSymbol mainSymbol, Value: BoundBlockStatement mainBody })
        {
            main = mainSymbol;
        }

        if (hasGlobalNonDeclarationStatements || main is null)
        {
            if (main is not null)
                _diag.ReportProgramHasBothMainAndTopLevelStatements(main.Location);

            scriptMain = FunctionFactory.CreateMain("__wl_script_main");
            var scriptMainBody = LowerAndCheckControlFlow(scriptMain, bound);
            _functionToBody[scriptMain] = scriptMainBody;
        }
        return new BoundProgram(main, scriptMain, _functionToBody.ToImmutableDictionary(), _typeScope.ToProgramTypeMemberInformation(), globalVariables);
    }

    private (BoundBlockStatement boundRoot, ImmutableArray<BoundVarDeclaration> globals, bool hasGlobalArbitraries) BindASTRoot(ASTRoot root)
    {
        var topLevelstatments = ImmutableArray.CreateBuilder<BoundStatement>();
        var globalVariables = ImmutableArray.CreateBuilder<BoundVarDeclaration>(); //NON-function delcarations

        BindTypeDeclarations(root);

        foreach (var func in root.GetChildrenOf<TopLevelFuncDeclaration>())
        {
            BindTopLevelStatement(func);
        }
        foreach (var toplevel in root.Children)
        {
            switch (toplevel)
            {
                case TopLevelTypeDeclaration:
                case TopLevelFuncDeclaration:
                case TopLevelError:
                    continue;
                case TopLevelStamentNode top: //ArbitraryStatementNode and VarDeclaration
                    var bound = BindTopLevelStatement(top);
                    if (bound is BoundErrorStatement) continue;

                    topLevelstatments.Add(bound);
                    if (bound is BoundVarDeclaration var)
                        globalVariables.Add(var);
                    break;
                default: throw new NotImplementedException($"{nameof(Binder)} does not yet allow {toplevel.GetType().Name}");
            }
        }
        //What if we parsed an empty file?
        var rootNode = topLevelstatments.Count > 0 ? topLevelstatments[0].Node : EmptyStatment.Get;
        var boundChildren = new BoundBlockStatement(rootNode, topLevelstatments.ToImmutable());

        return (boundChildren, globalVariables.ToImmutable(), topLevelstatments.Count != globalVariables.Count);
    }

    private void BindTypeDeclarations(ASTRoot root)
    {
        var typeDecls = root.GetChildrenOf<TopLevelTypeDeclaration>().ToList();
        var declaredTypes = new List<(TypeSymbol, TopLevelTypeDeclaration)>();
        for (int i = 0; i < typeDecls.Count; i++)
        {
            var declaration = typeDecls[i];
            var typeSyntax = typeDecls[i].Type;
            ImmutableArray<TypeSymbol>? typeParams = null;
            if (declaration.TypeParameters is not null)
            {
                typeParams = _typeScope.CreateTypeParameterSymbols(declaration.TypeParameters);
            }

            if (_typeScope.TryAddType(declaration.Type, out var typeSymbol, typeParameters: typeParams))
            {
                declaredTypes.Add((typeSymbol, typeDecls[i]));
            }
            else
            {
                _diag.ReportTypeAlreadyDeclared(typeSyntax.Location, typeSyntax.Name);
                //Skip the declaring it again
            }
        }
        foreach (var (typeSymbol, decl) in declaredTypes)
        {
            _typeScope.Push();
            if (_typeScope.GetTypeInformation(typeSymbol) is { TypeParameters: not null } info)
            {
                _typeScope.AddTypeParametersToScope(info.TypeParameters);
            }

            foreach (var member in decl.Members)
            {
                var memberType = _typeScope.GetTypeOrThrow(member.Type);
                var memberSymbol = new MemberFieldSymbol(member.Name, memberType);
                _typeScope.AddMember(typeSymbol, memberSymbol);
            }

            _typeScope.Pop();
        }
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
            ErrorStatement error => new BoundErrorStatement(error),
            _ => throw new NotImplementedException($"Bind statement for {statement}"),
        };
    }

    private BoundBlockStatement BindBlockStatement(BlockStatement st, bool pushScope = true)
    {
        var boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();
        if (pushScope)
            _scope.PushScope();
        foreach (var stmnt in st.Children)
        {
            var bound = BindStatement(stmnt);
            boundStatements.Add(bound);
        }
        if (pushScope)
            _scope.PopScope();
        return new BoundBlockStatement(st, boundStatements.ToImmutable());
    }

    private BoundStatement BindVarDeclaration(VarDeclaration varDecl, bool isGlobalScope)
    {
        var name = varDecl.Identifier.Name!;
        var rightHandSide = BindExpression(varDecl.RightHandSide);
        if (varDecl.Type is not null)
        {
            var type = _typeScope.GetTypeOrErrorType(varDecl.Type);
            rightHandSide = BindTypeConversion(rightHandSide, type);
            if(type == TypeSymbol.Void)
            {
                _diag.ReportTypeVoidIsNotValidHere(varDecl.Type.Location);
            }
        }

        if (varDecl.Type is null && _typeScope.GetActualType(rightHandSide.Type) == TypeSymbol.Void)
        {
            _diag.ReportCannotAssignVoidToImplicitlyTypedVariable(varDecl.Identifier, rightHandSide.Location);
        }

        VariableSymbol variable = isGlobalScope
                                ? new GlobalVariableSymbol(name, rightHandSide.Type)
                                : new LocalVariableSymbol(name, rightHandSide.Type, _functionStack.Peek());

        if (!_scope.TryDeclareVariable(variable))
        {
            _diag.ReportNameAlreadyDeclared(varDecl.Identifier);
            return new BoundErrorStatement(varDecl);
        }
        return new BoundVarDeclaration(varDecl, variable, rightHandSide);
    }

    private BoundStatement BindFunctionDeclaration(FuncDeclaration funcDecl, bool isGlobalScope = false)
    {
        if (!isGlobalScope) return BindLocalFunctionDeclaration(funcDecl);
        var nameToken = funcDecl.NameToken;
        var typeParameters = _typeScope.CreateTypeParameterSymbols(funcDecl.TypeParams);

        _typeScope.Push();
        if (_typeScope.AddTypeParametersToScope(typeParameters))
        {
            _typeScope.Pop();
            return new BoundErrorStatement(funcDecl);
        }

        var parameters = _typeScope.CreateParameterSymbols(funcDecl.Params);
        var returnType = _typeScope.GetTypeOrErrorType(funcDecl.ReturnType);

        TypeInformation? ownerTypeInfo = null;
        if (funcDecl.OwnerType is not null)
        {
            if (!_typeScope.TryGetTypeInformation(funcDecl.OwnerType, out ownerTypeInfo))
            {
                _diag.ReportTypeNotFound(funcDecl.OwnerType.ToString(), funcDecl.OwnerType.Location);
                _typeScope.Pop();
                return new BoundErrorStatement(funcDecl);
            }
            var ownerType = ownerTypeInfo.Type;
            if (parameters.Length < 1)
            {
                _diag.ReportMemberFuncNoParameters(funcDecl.Location, funcDecl.NameToken.Name!, ownerType);
            }
            else if (parameters[0].Type is TypeSymbol t && !_typeScope.TypeEquality(t, ownerType) && ownerType != TypeSymbol.Error)
            {
                _diag.ReportMemberFuncFirstParameterMustMatchOwner(funcDecl.Location, funcDecl.NameToken.Name!, ownerType, t);
            }
        }

        var (functionType, _) = _typeScope.CreateFunctionType(parameters, returnType, typeParameters, isMemberFunc: funcDecl.OwnerType is not null);
        var function = new FunctionSymbol(nameToken, typeParameters,
                                          parameters, functionType,
                                          returnType, ownerType: ownerTypeInfo?.Type);
        var boundDeclaration = new BoundFunctionDeclaration(funcDecl, function);

        _typeScope.Pop();

        if (ownerTypeInfo is not null) _typeScope.AddMember(ownerTypeInfo, new MemberFuncSymbol(function));

        if (!function.IsMemberFunc && !_scope.TryDeclareFunction(function))
        {
            _diag.ReportNameAlreadyDeclared(funcDecl.NameToken);
            return new BoundErrorStatement(funcDecl);
        }
        _unBoundBodyOf[function] = funcDecl.Body;
        return boundDeclaration;
    }

    private BoundStatement BindLocalFunctionDeclaration(FuncDeclaration funcDecl)
    {
        var nameToken = funcDecl.NameToken;
        var typeParameters = _typeScope.CreateTypeParameterSymbols(funcDecl.TypeParams);
        _typeScope.Push();
        if (_typeScope.AddTypeParametersToScope(typeParameters))
        {
            _typeScope.Pop();
            return new BoundErrorStatement(funcDecl);
        }
        if (funcDecl.OwnerType is not null)
        {
            var ownerType = _typeScope.GetTypeOrErrorType(funcDecl.OwnerType);
            _diag.ReportLocalMemberFuncDeclaration(nameToken, funcDecl.OwnerType.Location, ownerType);
        }

        var parameters = _typeScope.CreateParameterSymbols(funcDecl.Params);
        var returnType = _typeScope.GetTypeOrErrorType(funcDecl.ReturnType);

        var (functionType, _) = _typeScope.CreateFunctionType(parameters, returnType, typeParameters);

        var symbol = FunctionFactory.CreateLocalFunction(nameToken, typeParameters, parameters, functionType, returnType);

        if (!_scope.TryDeclareFunction(symbol))
        {
            _diag.ReportNameAlreadyDeclared(funcDecl.NameToken);
            _typeScope.Pop();
            return new BoundErrorStatement(funcDecl);
        }

        var boundBody = BindFunctionBody(symbol, funcDecl.Body);
        _functionToBody[symbol] = boundBody;
        _typeScope.Pop();

        PushFreeVariablesUpwards(symbol);

        //TODO: can we get around this?
        if (symbol.Parameters.Length > MAX_LOCAL_PARAMETERS)
        {
            _diag.ReportTooManyParametersInLocal(symbol, funcDecl.Location, MAX_LOCAL_PARAMETERS);
        }

        return new BoundFunctionDeclaration(funcDecl, symbol);
    }

    private BoundBlockStatement BindFunctionBody(FunctionSymbol function, BlockStatement body, bool lower = true)
    {
        _functionStack.Push(function);
        _scope.PushScope();
        _typeScope.Push();
        foreach (var typeParam in function.TypeParameters)
        {
            //no need to check the return - this has already run on function declaration
            // but we have since lost the scope, so define it again!
            _typeScope.TryAddType(typeParam, out var _);
        }

        BoundBlockStatement boundBody;
        {   /* scope of function body */
            foreach (var @param in function.Parameters)
            {
                _scope.TryDeclareVariable(@param, allowShadowing: true);
            }
            boundBody = BindBlockStatement(body, pushScope: false);
            if (lower) boundBody = LowerAndCheckControlFlow(function, boundBody);
        }

        _typeScope.Pop();
        _scope.PopScope();
        _functionStack.Pop();
        return boundBody;
    }

    private BoundStatement BindIfStatement(IfStatement ifStatement)
    {
        var condition = BindExpression(ifStatement.Condition, TypeSymbol.Bool);
        //any scope push happens in bindStatement :)
        var trueBranch = BindStatement(ifStatement.Then);
        var falseBranch = ifStatement.Else is null ? null : BindStatement(ifStatement.Else);
        return new BoundIfStatement(ifStatement, condition, trueBranch, falseBranch);
    }

    private BoundStatement BindWhileStatement(WhileStatement wile)
    {
        var condition = BindExpression(wile.Condition, TypeSymbol.Bool);
        var boundContinue = ImmutableArray.CreateBuilder<BoundExpression>(wile.Continue.Count);
        foreach (var cont in wile.Continue)
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
        if (_functionStack.Count == 0)
        {
            _diag.ReportCannotReturnOutsideFunction(ret.ReturnToken);
            return new BoundErrorStatement(ret);
        }
        var function = _functionStack.Peek();
        BoundExpression? expr = null;

        if (function.Type == TypeSymbol.Void)
        {
            if (ret.Expression is not null)
            {
                _diag.ReportReturnWithValueInVoidFunction(ret.ReturnToken, function);
                return new BoundErrorStatement(ret);
            }
        }
        else
        {
            //we're in a function that returns some value
            if (ret.Expression is null) //if the return does not contain such a value uh oh
            {
                _diag.ReportReturnIsMissingExpression(ret.ReturnToken, function.ReturnType);
                return new BoundErrorStatement(ret);
            }
            expr = BindTypeConversion(ret.Expression, function.ReturnType);
        }
        return new BoundReturnStatement(ret, expr);
    }

    private BoundStatement BindExprStatement(ExprStatement expr)
    {
        var bound = BindExpression(expr.Expression);
        return new BoundExprStatement(expr, bound);
    }

    private BoundExpression BindExpression(ExpressionNode expression, TypeSymbol targetType)
    => BindTypeConversion(expression, targetType);

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
            ObjectInitExpression se => BindObjectInitExpression(se),
            NullExpression @null => BindNullExpression(@null),
            FuncTypeApplication application => BindFuncTypeApplication(application),
            LambdaExpression expr => BindLambdaExpression(expr),
            ErrorExpression => new BoundErrorExpression(expression),
            _ => throw new NotImplementedException($"{nameof(BindExpression)} failed on ({expression.Location})-'{expression}'")
        };
    }

    private BoundExpression BindFuncTypeApplication(FuncTypeApplication application)
    {
        var funcTypeInfo = BindAccessCallExpression(application.AppliedOn, out var access, out var funcSymbol);

        if (funcTypeInfo is null || funcSymbol is null) return new BoundErrorExpression(application);

        if (!funcTypeInfo.HasTypeParameters
            || funcTypeInfo.HasTypeParameters && funcTypeInfo.TypeParameters.Value.Length != application.TypeParams.Count)
        {
            var received = !funcTypeInfo.HasTypeParameters ? 0 : funcTypeInfo.TypeParameters.Value.Length;
            _diag.ReportFunctionMismatchingTypeParameters(application.Location,
                                                          application.TypeParams.Count,
                                                          received);
            return new BoundErrorExpression(application);
        }
        // TypeParameters of the original function can be retrieved from the BoundTypeApplication
        List<TypeSymbol> typeArgs = [];
        foreach (var typeParam in application.TypeParams)
        {
            var typeArg = _typeScope.GetTypeOrErrorType(typeParam);
            typeArgs.Add(typeArg);
            if (typeArg == TypeSymbol.Void)
            {
                _diag.ReportFunctionIllegalVoidTypeArgument(funcSymbol, typeParam.Location);
                return new BoundErrorExpression(application);
            }
        }
        var specailized = CreateSpecializedFunction(funcSymbol, application.Location, typeArgs);

        return new BoundTypeApplication(application, access, specailized);
    }

    //Nullable typeArguments because you may want to call a generic function "id<T>(T t)" by doing just "id(1)"
    //  which is then inferred to id<int>(1);
    private SpecializedFunctionSymbol CreateSpecializedFunction(
        FunctionSymbol func,
        WarmLangLexerParser.TextLocation location,
        List<TypeSymbol>? typeArguments = null
    )
    {
        if (typeArguments is not null && func.TypeParameters.Length != typeArguments.Count)
            throw new Exception($"Compiler bug - do not allow inferring parameters when any are explicitly defined");

        var instantiatedParameters = ImmutableArray.CreateBuilder<ParameterSymbol>(func.Parameters.Length);
        var instantiatedTypeParameters = new List<TypeSymbol>();
        //FIXME: Honestly, a rewrite from typesymbol to a typeId is due...
        //       => pretty nasty using strings - but type parameters are unique within a function so should be fine... 
        var typeParametersMap = new Dictionary<string, TypeSymbol>();
        // We have to create a concrete version of the definition. Any TypeParameterSymbol is replaced by its concrete...
        for (int i = 0; i < func.TypeParameters.Length; i++)
        {
            var typeParam = func.TypeParameters[i];
            var concrete = typeArguments?[i] ?? _typeScope.CreatePlacerHolderType();
            instantiatedTypeParameters.Add(concrete);
            typeParametersMap[typeParam.Name] = concrete;
        }

        //Let's also concretize the actual parameters + return type
        var concreteReturn = _typeScope.MakeConcrete(func.ReturnType, typeParametersMap, location);
        for (int i = 0; i < func.Parameters.Length; i++)
        {
            var param = func.Parameters[i];
            var concrete = _typeScope.MakeConcrete(param.Type, typeParametersMap, location);
            instantiatedParameters.Add(new ParameterSymbol(param.Name, concrete, param.Placement));
        }

        var concreteParameters = instantiatedParameters.MoveToImmutable();
        var (specializedFunctionType, _) = _typeScope.CreateFunctionType(concreteParameters, concreteReturn, isMemberFunc: func.IsMemberFunc);
        var specailized = new SpecializedFunctionSymbol(func, instantiatedTypeParameters,
                                                        concreteParameters, specializedFunctionType,
                                                        concreteReturn, location);
        return specailized;
    }

    private BoundExpression BindAssignmentExpression(AssignmentExpression assignment)
    {
        var boundAccess = BindAccess(assignment.Access, expectWriteable: true);
        if (boundAccess is BoundExprAccess)
        {
            _diag.ReportInvalidLeftSideOfAssignment(assignment.Location);
            return new BoundErrorExpression(assignment);
        }
        if (boundAccess is BoundSubscriptAccess sa)
        {
            var targetType = sa.Target.Type;
            if (targetType == TypeSymbol.String)
                _diag.ReportSubscriptTargetIsReadOnly(targetType, assignment.Access.Location);
        }
        if (boundAccess is BoundMemberAccess bma && bma.Member.IsReadOnly)
        {
            //TODO: Should we create error state instead?
            _diag.ReportCannotAssignToReadonlyMember(bma.Target.Type, bma.Member.Name, assignment.Access.Location);
        }
        var boundRightHandSide = BindTypeConversion(assignment.RightHandSide, boundAccess.Type);
        return new BoundAssignmentExpression(assignment, boundAccess, boundRightHandSide);
    }

    private BoundExpression BindCallExpression(CallExpression ce)
    {
        //We have reached a cast 'bool(25)' or 'int(true)' or 'string(2555)'
        if (ce.Arguments.Count == 1 && ce.Called is AccessPredefinedType predefined)
        {
            var predefinedType = _typeScope.GetTypeOrErrorType(predefined.Syntax);
            return BindTypeConversion(ce.Arguments[0], predefinedType, allowExplicit: true);
        }
        var funcTypeInfo = BindAccessCallExpression(ce.Called, out var accessToCall, out var calledFuncSymbol);

        if (funcTypeInfo is null) return new BoundErrorExpression(ce);

        var typeArgEnv = ImmutableDictionary<TypeSymbol, TypeSymbol>.Empty;
        if (funcTypeInfo.HasTypeParameters && funcTypeInfo.TypeParameters.Value.Length > 0)
        {
            if (calledFuncSymbol is null)
            {
                _diag.ReportFeatureNotImplemented(ce.Location, "Cannot call generic functions that are not applied at call");
                return new BoundErrorExpression(ce);
            }
            if (calledFuncSymbol is not SpecializedFunctionSymbol specialized)
            {
                calledFuncSymbol = specialized = CreateSpecializedFunction(calledFuncSymbol, ce.Location);
            }
            typeArgEnv = specialized.TypeParameters.Zip(specialized.TypeArguments).ToImmutableDictionary(kv => kv.First, kv => kv.Second);
            funcTypeInfo = (FunctionTypeInformation)_typeScope.GetTypeInformationOrThrow(specialized.Type);
            accessToCall = new BoundExprAccess(new BoundTypeApplication(ce, accessToCall, specialized));
        }

        var argsSize = ce.Arguments.Count + (funcTypeInfo.IsMemberFunc ? 1 : 0);
        if (argsSize != funcTypeInfo.Parameters.Count)
        {
            _diag.ReportFunctionCallMismatchArguments(ce.Called.Location, ce.Called.ToString(), funcTypeInfo.Parameters.Count, argsSize);
            return new BoundErrorExpression(ce);
        }

        var arguments = ImmutableArray.CreateBuilder<BoundExpression>(argsSize);
        if (funcTypeInfo.IsMemberFunc && accessToCall is not BoundFuncAccess)
        {
            BoundMemberAccess? bma = accessToCall switch
            {
                BoundMemberAccess b => b,
                BoundExprAccess { Expression: BoundTypeApplication { Access: BoundMemberAccess b } } => b,
                _ => null
            };
            if (bma is null)
            {
                /*TODO: We need to synthesize a closure inside "BindAccessCallExpression"
                        So, if a MemberFuncSymbol is directly used, do as normal...
                        but if it is as a value, then we need to create a closure that requires the "target"
                        on which we are calling it as a method...
                */
                _diag.ReportFeatureNotImplemented(ce.Location, "Cannot use methods indirectly");
                return new BoundErrorExpression(ce);
            }
            if (bma is { Target: not BoundTypeAccess, Member: MemberFuncSymbol })
            {
                arguments.Add(new BoundAccessExpression(ce, bma.Target));
            }
        }

        arguments.AddRange(ce.Arguments.Select(BindExpression));
        for (int i = 0; i < argsSize; i++)
        {
            arguments[i] = BindTypeConversion(arguments[i], funcTypeInfo.Parameters[i]);
        }

        //After all unifies have happend
        if (calledFuncSymbol is SpecializedFunctionSymbol sfs)
        {
            var voidTypeArg = sfs.TypeArguments.FirstOrDefault(t => _typeScope.GetActualType(t) == TypeSymbol.Void);
            if(voidTypeArg is not null)
            {
                _diag.ReportFunctionIllegalVoidTypeArgument(calledFuncSymbol, ce.Location);
                return new BoundErrorExpression(ce);
            }
        }


        return new BoundCallExpression(ce, accessToCall, arguments.MoveToImmutable(), funcTypeInfo.ReturnType, typeArgEnv);
    }

    private BoundExpression BindAccessExpression(AccessExpression ae)
    {
        var boundAccess = BindAccess(ae.Access);
        if (boundAccess is BoundInvalidAccess) return new BoundErrorExpression(ae);
        return new BoundAccessExpression(ae, boundAccess.Type, boundAccess);
    }

    private BoundAccess BindAccess(Access access, bool expectFunc = false, bool expectWriteable = false)
    {
        switch (access)
        {
            case NameAccess na:
                {
                    if (_scope.TryLookup(na.Name, out var symbol))
                    {
                        if (symbol is FunctionSymbol func) return new BoundFuncAccess(func);
                        if (symbol is VariableSymbol variable)
                        {
                            //Have we reached a free variable?
                            var currentFunction = _functionStack.Peek();
                            if (variable is ScopedVariableSymbol scoped && scoped.BelongsToOrThrow != currentFunction)
                            {
                                var fv = currentFunction.FreeVariables;
                                //Create a new mapping for this free variable
                                if (!fv.TryGetValue(scoped, out var local))
                                {
                                    fv[scoped] = local = new LocalVariableSymbol(scoped.Name, scoped.Type, currentFunction);
                                }
                                variable = local;

                                if (expectWriteable)
                                {
                                    _diag.ReportVariablesCapturedByClosureAreLocal(na.Location, na.Name);
                                }
                            }
                            return new BoundNameAccess(variable);
                        }
                    }
                    if (_typeScope.TryGetType(na.Name, out var type))
                        return new BoundTypeAccess(type);

                    _diag.ReportNameDoesNotExist(na.Location, na.Name);
                    return new BoundInvalidAccess();
                }
            case AccessPredefinedType predefined:
                {
                    var type = _typeScope.GetTypeOrErrorType(predefined.Syntax);
                    return new BoundPredefinedTypeAccess(type);
                }
            case MemberAccess ma:
                {
                    var boundTarget = BindAccess(ma.Target);
                    var boundTargetType = _typeScope.GetActualType(boundTarget.Type);
                    if (boundTargetType == TypeSymbol.Error || ma.MemberToken.Name is null)
                        return new BoundInvalidAccess();

                    if (!_typeScope.TryFindMember(boundTargetType, ma.MemberToken.Name!, out var boundMember))
                    {
                        _diag.ReportCouldNotFindMemberForType(ma.Location, boundTargetType, ma.MemberToken.Name);
                        return new BoundInvalidAccess();
                    }
                    if (boundMember is MemberFuncSymbol fs && !expectFunc)
                    {
                        if (boundTargetType.IsValueType)
                        {
                            //TODO: could be cool to just lock in the '2' in 'var twoStr = 2.toString();'
                            _diag.ReportFeatureNotImplemented(ma.Location, "Cannot create closures for methods on value types");
                            return new BoundInvalidAccess();
                        }
                        if (!_typeScope.TryGetTypeInformation(fs.Function.Type, out var memberFuncInfo)
                           || memberFuncInfo.HasTypeParameters)
                        {
                            _diag.ReportFeatureNotImplemented(ma.Location, "Cannot reference generic methods (yet)");
                            return new BoundInvalidAccess();
                        }
                        //TODO: Please, put the "target" into the free variables, so we can reuse closure logic!
                        //TODO: Use the type information instead of function symbol
                        var (funcType, _) = _typeScope.CreateFunctionType(
                            fs.Function.Parameters.Skip(1).Select(p => p.Type).ToImmutableArray(),
                            fs.Function.ReturnType,
                            fs.Function.TypeParameters,
                            isMemberFunc: false
                        );
                        return new BoundFuncAccess(boundTarget, fs.Function, funcType);
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
                    if (_typeScope.IsSubscriptable(boundTarget.Type, out var resultType))
                    {
                        var boundIndexExpr = BindExpression(sa.Index);
                        return new BoundSubscriptAccess(boundTarget, boundIndexExpr, resultType);
                    }
                    _diag.ReportCannotSubscriptIntoType(sa.Location, boundTarget.Type);
                    return new BoundInvalidAccess();
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
        if (bound.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(ue);
        }
        var boundOperator = BoundUnaryOperator.Bind(_typeScope, ue.Operator.Kind, bound);
        if (boundOperator is null)
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
        if (boundLeft.Type == TypeSymbol.Error || boundRight.Type == TypeSymbol.Error)
        {
            //this mutes any errors that follow as a result of left/right being errors
            return new BoundErrorExpression(binaryExpr);
        }
        _typeScope.Unify(boundLeft.Type, boundRight.Type);
        var boundOperator = BoundBinaryOperator.Bind(_typeScope, binaryExpr.Operator.Kind, boundLeft, boundRight);
        if (boundOperator is null)
        {
            _diag.ReportBinaryOperatorCannotBeApplied(binaryExpr.Location, binaryExpr.Operator, boundLeft.Type, boundRight.Type);
            return new BoundErrorExpression(binaryExpr);
        }
        return new BoundBinaryExpression(binaryExpr, boundLeft, boundOperator, boundRight);
    }

    private BoundExpression BindListInitExpression(ListInitExpression le)
    {
        if (le.IsEmptyList)
        {
            TypeSymbol inner;
            //It was an implicitly typed empty list []
            if (le.ElementType is null) inner = _typeScope.CreatePlacerHolderType();
            else inner = _typeScope.GetTypeOrErrorType(le.ElementType);

            var type = _typeScope.GetOrCreateListType(inner);
            return new BoundListExpression(le, type, ImmutableArray<BoundExpression>.Empty);
        }

        var elements = ImmutableArray.CreateBuilder<BoundExpression>(le.Elements.Count);
        TypeSymbol? innerType = null;
        foreach (var elm in le.Elements)
        {
            var bound = BindExpression(elm);
            innerType ??= bound.Type;
            var conversion = BindTypeConversion(bound, innerType);
            elements.Add(conversion);
        }
        var initListType = _typeScope.GetOrCreateListType(innerType!);
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

    private BoundExpression BindObjectInitExpression(ObjectInitExpression se)
    {
        TypeSymbol type;
        if (!_typeScope.TryGetTypeInformation(se.ObjectType, out var typeInfo))
        {
            _diag.ReportTypeNotFound(se.Name, se.ObjectType.Location);
            type = TypeSymbol.Error;
        }
        else type = typeInfo.Type;

        if (type == TypeSymbol.Int || type == TypeSymbol.Bool || type == TypeSymbol.String
           || typeInfo is TypeParamaterInformation || typeInfo is FunctionTypeInformation)
        {
            //The "name" part of struct init is not an identifier, for example: "new int {...};"
            //TODO: Do we want to allow "int x = new int{5};"?
            _diag.ReportCannotInstantiateTypeWithNew(type, se.Location);
            return new BoundErrorExpression(se);
        }
        var members = ImmutableArray.CreateBuilder<(MemberSymbol, BoundExpression)>(se.Members.Count);
        foreach (var (mNameToken, mExpr) in se.Members)
        {
            var mName = mNameToken.Name!;
            BoundExpression bound;
            if (_typeScope.TryFindMember(type, mName, out var memberSymbol))
            {
                bound = BindExpression(mExpr);
                bound = BindTypeConversion(bound, memberSymbol.Type);
            }
            else
            {
                bound = new BoundErrorExpression(mExpr);
                memberSymbol = ErrorMemberSymbol.Instance;
                _diag.ReportTypeHasNoSuchMember(type, mNameToken);
            }
            ;
            members.Add((memberSymbol, bound));
        }
        return new BoundObjectInitExpression(se, type, members.MoveToImmutable());
    }

    private BoundExpression BindLambdaExpression(LambdaExpression expr)
    {
        var parameters = _typeScope.CreateParameterSymbols(expr.Parameters!, treatNullAsPlaceholder: true);
        var returnType = _typeScope.CreatePlacerHolderType();
        var (lambdaType, _) = _typeScope.CreateFunctionType(parameters, returnType);
        var lambdaSymbol = FunctionFactory.CreateLambda(expr.Location, parameters, lambdaType, returnType);

        var lambdaBody = BindFunctionBody(lambdaSymbol, expr.Body, lower: false);
        if (_typeScope.GetTypeInformation(returnType) is PlaceHolderInformation)
        {
            //We weren't able to infer the return type - make it void
            _typeScope.Unify(returnType, TypeSymbol.Void);
        }
        _functionToBody[lambdaSymbol] = LowerAndCheckControlFlow(lambdaSymbol, lambdaBody);
        
        PushFreeVariablesUpwards(lambdaSymbol);

        //TODO: can we get around this?
        if (lambdaSymbol.Parameters.Length > MAX_LOCAL_PARAMETERS)
        {
            _diag.ReportTooManyParametersInLocal(lambdaSymbol, expr.Location, MAX_LOCAL_PARAMETERS);
        }

        return new BoundLambdaExpression(expr, lambdaSymbol);
    }

    private static BoundExpression BindNullExpression(NullExpression @null)
        => new BoundNullExpression(@null);

    private BoundExpression BindTypeConversion(ExpressionNode expr, TypeSymbol to, bool allowExplicit = false)
    {
        var boundExpression = BindExpression(expr);
        return BindTypeConversion(boundExpression, to, allowExplicit);
    }

    private BoundExpression BindTypeConversion(BoundExpression expr, TypeSymbol to, bool allowExplicit = false)
    {
        _typeScope.Unify(expr.Type, to);

        var conversion = Conversion.GetConversion(expr.Type, to, _typeScope.TypeEquality);
        if (!conversion.Exists)
        {
            if (to != TypeSymbol.Error && expr.Type != TypeSymbol.Error)
            {
                var actualTo = _typeScope.GetActualType(to);
                var actualFrom = _typeScope.GetActualType(expr.Type);
                _diag.ReportCannotConvertToType(expr.Location, actualTo, actualFrom);
            }
            return new BoundErrorExpression(expr.Node);
        }
        if (conversion.IsExplicit && !allowExplicit)
        {
            _diag.ReportCannotImplicitlyConvertToType(expr.Location, to, expr.Type);
            return new BoundErrorExpression(expr.Node);
        }
        if (conversion.IsIdentity)
            return expr;
        return new BoundTypeConversionExpression(expr.Node, to, expr);
    }

    private FunctionTypeInformation? BindAccessCallExpression(Access access, out BoundAccess accessSymbol,
                                                              out FunctionSymbol? symbol)
    {
        symbol = null;
        accessSymbol = BindAccess(access, expectFunc: true);
        if (accessSymbol is BoundInvalidAccess) return null;
        var typeInfo = _typeScope.GetTypeInformation(accessSymbol.Type);
        if (typeInfo is null || typeInfo.Type == TypeSymbol.Error) return null;

        if (typeInfo is not FunctionTypeInformation funcInfo)
        {
            _diag.ReportExpectedFunctionName(access.Location);
            return null;
        }
        symbol = accessSymbol switch
        {
            BoundFuncAccess acc => acc.Func,
            BoundMemberAccess { Member: MemberFuncSymbol acc } => acc.Function,
            BoundExprAccess { Expression: BoundTypeApplication app } => app.Specialized,
            _ => null
        };
        return funcInfo;
    }

    private void PushFreeVariablesUpwards(FunctionSymbol target)
    {
        //Create a copy - so we can remove from the original ... oof
        if (target.FreeVariables.Count == 0) return;

        var fvMap = target.FreeVariables.ToDictionary();
        var missingLinkUpwards = target.FreeVariables.Keys.ToHashSet();
        foreach (var func in _functionStack)
        {
            if (func.IsGlobal) continue;
            foreach (var (v, l) in fvMap)
            {
                if (v.BelongsTo != func && !func.FreeVariables.ContainsKey(v))
                    func.FreeVariables[v] = new LocalVariableSymbol(l.Name, l.Type, func);

                if (func.FreeVariables.TryGetValue(v, out var upperLocal) && missingLinkUpwards.Remove(v))
                {
                    target.FreeVariables.Remove(v);
                    target.FreeVariables[upperLocal] = l;
                }
            }
        }
    }

    private BoundBlockStatement LowerAndCheckControlFlow(FunctionSymbol func, BoundBlockStatement blockStatement)
    {
        blockStatement = Lowerer.LowerBody(func, blockStatement, _typeScope);

        if (!ControlFlowGraph.AllPathsReturn(blockStatement))
            _diag.ReportNotAllCodePathsReturn(func);
        return blockStatement;
    }    
}