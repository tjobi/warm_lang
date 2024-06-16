namespace WarmLangCompiler.Binding;

using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;
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
            ExprStatement expr => BindExprStatement(expr),
            WhileStatement wile => BindWhileStatement(wile),
            IfStatement ifStatement => BindIfStatement(ifStatement),
            VarDeclaration varDecl => BindVarDeclaration(varDecl),
            FuncDeclaration funcDecl => BindFunctionDeclaration(funcDecl),
            _ => throw new NotImplementedException($"Bind statement for {statement}"),
        };
    }


    private BoundStatement BindVarDeclaration(VarDeclaration varDecl)
    {
        var type = varDecl.Type switch 
        {
            TypeSyntaxInt => TypeSymbol.Int,
            TypeSyntaxList => TypeSymbol.List,
            _ => throw new NotImplementedException($"BindVarDeclaration doesn't know {varDecl.Type}"),
        };
        var name = varDecl.Name;
        var rightHandSide = BindExpression(varDecl.RightHandSide);
        if(type != rightHandSide.Type)
        {
            _diag.ReportCannotImplicitlyConvertToType(type, rightHandSide.Type);
            return new BoundErrorStatement(varDecl);
        }
        
        var variable = new VariableSymbol(name, rightHandSide.Type);
        if(!_scope.TryDeclareVariable(variable))
        {
            _diag.ReportVariableAlreadyDeclared(name);
            return new BoundErrorStatement(varDecl);
        }
        return new BoundVarDeclaration(varDecl, name, rightHandSide);
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
    
    private BoundStatement BindFunctionDeclaration(FuncDeclaration funcDecl)
    {
        throw new NotImplementedException("BIND FUNCTION DECLARATION!");
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
            ConstExpression ce => BindConstantExpression(ce),
            BinaryExpression be => BindBinaryExpression(be),
            UnaryExpression ue => BindUnaryExpression(ue),
            ListInitExpression le => BindListInitExpression(le),
            CallExpression ce => BindCallExpression(ce),
            AccessExpression ae => BindAccessExpression(ae),
            _ => throw new NotImplementedException($"Bind expression failed on {expression}")
        };
    }
    
    private BoundExpression BindCallExpression(CallExpression ce)
    {
        throw new NotImplementedException("BIND CALL EXPRESSION!");
    }

    private BoundExpression BindAccessExpression(AccessExpression ae)
    {
        //TODO: this is a big mess!
        if(ae.Access is NameAccess na)
        {
            if(_scope.TryLookup(na.Name, out var symbol))
            {
                return new BoundAccessExpression(ae, (VariableSymbol) (symbol ?? new VariableSymbol("?", TypeSymbol.Error)));
            }
            throw new Exception($"TryLookup returned a null for {ae}");
        } else
            throw new NotImplementedException("Binder doesn't allow arbitrary access yet");
    }


    private BoundExpression BindListInitExpression(ListInitExpression le)
    {
        var elements = ImmutableArray.CreateBuilder<BoundExpression>(le.Elements.Count);
        foreach(var elm in le.Elements)
        {
            var bound = BindExpression(elm);
            elements.Add(bound);
        }
        return new BoundListExpression(le, TypeSymbol.List, elements.MoveToImmutable());
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

    private BoundExpression BindConstantExpression(ConstExpression ce)
    {
        var type = TypeSymbol.Int; //TODO: more constants?
        return new BoundConstantExpression(ce, type);
    }
}
