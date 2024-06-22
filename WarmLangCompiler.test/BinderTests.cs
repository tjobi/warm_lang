namespace WarmLangCompiler.test;

using System.Collections.Immutable;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;
using WarmLangLexerParser;

public class BinderTests
{
    private readonly ErrorWarrningBag _diag;

    private static readonly ATypeSyntax _syntaxInt = new TypeSyntaxInt(new TextLocation(1,1, length:3));
    private static readonly ATypeSyntax _syntaxIntList = new TypeSyntaxList(new TextLocation(1,1,length:3+2),_syntaxInt);

    private readonly Binder _binder;
    public BinderTests()
    {
        _diag = new ErrorWarrningBag();
        _binder = new Binder(_diag);
        
    }

    private static BlockStatement CreateBlockStatement(params StatementNode[] statements) => new(MakeToken(TCurLeft,1,1), statements.ToList(), MakeToken(TCurRight,1,1));
    private static BoundBlockStatement CreateBoundBlockStatement(StatementNode syntax, params BoundStatement[] statements) => new(syntax, statements.ToImmutableArray());
    private static BoundProgram CreateBoundProgram(StatementNode syntax, params BoundStatement[] statements) => new(CreateBoundBlockStatement(syntax, statements));
    private static ConstExpression ConstCreater(int val) => new(val, new TextLocation(1,1));
    private static SyntaxToken MakeVariableToken(string name) => MakeToken(TIdentifier, new TextLocation(1,1, length:name.Length), name);

    [Fact]
    public void BindVariableDeclarationForConstantInteger()
    {
        var input = CreateBlockStatement(
            new VarDeclaration(_syntaxInt,MakeVariableToken("x"), ConstCreater(5))
        );        
        var expected = new BoundProgram(new BoundBlockStatement(input, new BoundStatement[]
        {
            new BoundVarDeclaration(
                new VarDeclaration(_syntaxInt,MakeVariableToken("x"),ConstCreater(5)),
                "x",
                new BoundTypeConversionExpression(ConstCreater(5), TypeSymbol.Int,
                    new BoundConstantExpression(ConstCreater(5), TypeSymbol.Int)
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
        //int x = [5];
        var rhs = new ListInitExpression(
                    MakeToken(TBracketLeft,1,1),
                    new List<ExpressionNode>()
                    {
                        ConstCreater(5)
                    },
                    MakeToken(TBracketRight,1,1));
        var varDecl = new VarDeclaration(_syntaxInt,MakeVariableToken("x"),rhs);
        var input = CreateBlockStatement(varDecl);
        var expected = new BoundProgram(CreateBoundBlockStatement(
            input,
            new BoundVarDeclaration(varDecl,"x",
                new BoundListExpression(
                    rhs,
                    TypeSymbol.IntList,
                    new BoundExpression[]{new BoundConstantExpression(ConstCreater(5), TypeSymbol.Int)}.ToImmutableArray())
            )
        ));
        var expectedErrorBag = new ErrorWarrningBag();
        expectedErrorBag.ReportCannotConvertToType(rhs.Location, TypeSymbol.Int, TypeSymbol.IntList);

        var boundProgram = _binder.BindProgram(input);

        boundProgram.Should().NotBeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEquivalentTo(expectedErrorBag);
    }

    [Fact]
    public void BindVariableDeclarationOfEmptyList()
    {
        //int[] x = [];
        var rhs = new ListInitExpression(MakeToken(TBracketLeft,1,1),new List<ExpressionNode>(),MakeToken(TBracketRight,1,1));
        var varDecl = new VarDeclaration(_syntaxIntList,MakeVariableToken("x"),rhs);
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
        //x;
        var nameAccess = new NameAccess(MakeToken(TIdentifier,0,0,"x"));
        var accessExpr = new AccessExpression(nameAccess);
        var input = CreateBlockStatement(new ExprStatement(accessExpr));
        var expected = CreateBoundProgram(input,
            new BoundExprStatement(new ExprStatement(accessExpr),new BoundErrorExpression(accessExpr))
        );
        var expectedErrorBag = new ErrorWarrningBag();
        expectedErrorBag.ReportNameDoesNotExist(nameAccess.Location, nameAccess.Name);

        var boundProgram = _binder.BindProgram(input);

        boundProgram.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEquivalentTo(expectedErrorBag);
    }
}