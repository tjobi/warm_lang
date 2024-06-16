namespace WarmLangCompiler.Binding;

using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.ErrorReporting;

public sealed class Binder
{
    private readonly ErrorWarrningBag _diag;
    public Binder(ErrorWarrningBag bag)
    {
        _diag = bag;
    }

    public BoundProgram BindProgram(ASTNode root)
    {
        if(root is BlockStatement statement)
        {
            var bound = BindBlockStatement(statement);
            return new BoundProgram(bound);
        }
        else 
            throw new Exception("Binder only allow root to be BlockStatements?");
    }

    private BoundStatement BindStatement(StatementNode statement)
    {
        return statement switch 
        {
            BlockStatement st => BindBlockStatement(st),
            ExprStatement expr => BindExprStatement(expr),
            WhileStatement wile => BindWhileStatement(wile),
            IfStatement ifStatement => BindIfStatement(ifStatement),
            VarDeclaration
            _ => throw new NotImplementedException(),
        };
    }


    private BoundBlockStatement BindBlockStatement(BlockStatement st)
    {
        var boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();
        foreach(var stmnt in st.Children)
        {
            var bound = BindStatement(stmnt);
            boundStatements.Add(bound);
        }
        return new BoundBlockStatement(st, boundStatements.ToImmutable());
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
        };
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
