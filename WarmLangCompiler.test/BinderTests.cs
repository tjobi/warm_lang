namespace WarmLangCompiler.test;

using System.Collections.Immutable;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;


public class BinderTests
{
    private readonly ErrorWarrningBag _diag;

    private static readonly ATypeSyntax _syntaxInt = new TypeSyntaxInt();
    private static readonly ATypeSyntax _syntaxIntList = new TypeSyntaxList(_syntaxInt);

    private readonly Binder _binder;
    public BinderTests()
    {
        _diag = new ErrorWarrningBag();
        _binder = new Binder(_diag);
        
    }

    private BlockStatement CreateBlockStatement(params StatementNode[] statements) => new(statements.ToList());
    private BoundBlockStatement CreateBoundBlockStatement(StatementNode syntax, params BoundStatement[] statements) => new(syntax, statements.ToImmutableArray());
    private BoundProgram CreateBoundProgram(StatementNode syntax, params BoundStatement[] statements) => new(CreateBoundBlockStatement(syntax, statements));

    [Fact]
    public void BindVariableDeclarationForConstantInteger()
    {
        var input = CreateBlockStatement(
            new VarDeclaration(_syntaxInt,"x",new ConstExpression(5))
        );        
        var expected = new BoundProgram(new BoundBlockStatement(input, new BoundStatement[]
        {
            new BoundVarDeclaration(new VarDeclaration(_syntaxInt,"x",new ConstExpression(5)),"x",
                new BoundTypeConversionExpression(new ConstExpression(5), TypeSymbol.Int,
                    new BoundConstantExpression(new ConstExpression(5), TypeSymbol.Int)
                )
            ),
        }.ToImmutableArray())
        );

        var boundProgram = _binder.BindProgram(input);

        boundProgram.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void BindVariableDeclarationOfTypeIntToList()
    {
        var rhs = new ListInitExpression(new List<ExpressionNode>()
                    {
                        new ConstExpression(5)
                    });
        var varDecl = new VarDeclaration(_syntaxInt,"x",rhs);
        var input = CreateBlockStatement(varDecl);
        var expected = new BoundProgram(CreateBoundBlockStatement(
            input,
            new BoundVarDeclaration(varDecl,"x",
                new BoundListExpression(
                    rhs,
                    TypeSymbol.IntList,
                    new BoundExpression[]{new BoundConstantExpression(new ConstExpression(5), TypeSymbol.Int)}.ToImmutableArray())
            )
        ));
        var expectedErrorBag = new ErrorWarrningBag();
        expectedErrorBag.ReportCannotConvertToType(TypeSymbol.Int, TypeSymbol.IntList);

        var boundProgram = _binder.BindProgram(input);

        boundProgram.Should().NotBeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEquivalentTo(expectedErrorBag);
    }

    [Fact]
    public void BindVariableDeclarationOfEmptyList()
    {
        var rhs = new ListInitExpression(new List<ExpressionNode>());
        var varDecl = new VarDeclaration(_syntaxIntList,"x",rhs);
        var input = CreateBlockStatement(varDecl);
        var expected = new BoundProgram(
            CreateBoundBlockStatement(input,
                new BoundVarDeclaration(varDecl,"x",
                    new BoundTypeConversionExpression(rhs, TypeSymbol.IntList,
                        new BoundListExpression(
                            rhs,
                            TypeSymbol.EmptyList, new ImmutableArray<BoundExpression>())
                    )
                )
            ));

        var boundProgram = _binder.BindProgram(input);

        boundProgram.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void BindAccessToUndeclaredVariable()
    {
        var nameAccess = new NameAccess(MakeToken(TIdentifier,0,0,"x"));
        var accessExpr = new AccessExpression(nameAccess);
        var input = CreateBlockStatement(new ExprStatement(accessExpr));
        var expected = CreateBoundProgram(input,
            new BoundExprStatement(new ExprStatement(accessExpr),new BoundErrorExpression(accessExpr))
        );
        var expectedErrorBag = new ErrorWarrningBag();
        expectedErrorBag.ReportNameDoesNotExist("x");

        var boundProgram = _binder.BindProgram(input);

        boundProgram.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEquivalentTo(expectedErrorBag);
    }
}