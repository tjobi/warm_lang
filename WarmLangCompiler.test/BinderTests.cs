namespace WarmLangCompiler.test;

using System.Collections.Immutable;
using WarmLangLexerParser.AST;
using WarmLangLexerParser.AST.TypeSyntax;
using WarmLangLexerParser;
using WarmLangCompiler.Binding.Lower;
using WarmLangCompiler.Binding.BoundAccessing;
using System.Collections.ObjectModel;

public class BinderTests
{
    private readonly ErrorWarrningBag _diag;

    private static readonly TypeSyntaxNode _syntaxInt = new TypeSyntaxInt(new TextLocation(1,1, length:3));
    private static readonly TypeSyntaxNode _syntaxIntList = new TypeSyntaxList(new TextLocation(1,1,length:3+2),_syntaxInt);

    private readonly Binder _binder;
    public BinderTests()
    {
        _diag = new ErrorWarrningBag();
        _binder = new Binder(_diag);
    }

    private static BlockStatement CreateBlockStatement(params StatementNode[] statements) => new(MakeToken(TCurLeft,1,1), statements.ToList(), MakeToken(TCurRight,1,1));
    private static BoundBlockStatement CreateBoundBlockStatement(StatementNode syntax, params BoundStatement[] statements) => new(syntax, statements.ToImmutableArray());
    private static BoundProgram CreateBoundProgram(params BoundStatement[] statements) 
    {
        var globals = statements.Where(s => s is BoundVarDeclaration).Select(s => (BoundVarDeclaration)s).ToImmutableArray();
        var body = statements.Where(s => s is not BoundFunctionDeclaration or BoundVarDeclaration).ToImmutableArray();
        var scriptMain = FunctionSymbol.CreateMain("__wl_script_main");
        var functions = ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Empty
        .Add(
            scriptMain, Lowerer.LowerBody(scriptMain, new BoundBlockStatement(body[0].Node, body))
        );
        
        var typeMembers = new ReadOnlyDictionary<TypeSymbol,IList<MemberSymbol>>(BuiltinMembers.CreateMembersForBuiltins());
        var typeMethods = new ReadOnlyDictionary<TypeSymbol, Dictionary<FunctionSymbol, BoundBlockStatement>>(new Dictionary<TypeSymbol, Dictionary<FunctionSymbol, BoundBlockStatement>>());
        var declaredTypes = new List<TypeSymbol>().AsReadOnly();
        var typeInfo = new TypeMemberInformation(typeMembers, typeMethods, declaredTypes);

        return new BoundProgram(null, scriptMain, functions, typeInfo, globals);
    }
    private static ConstExpression ConstCreater(int val) => new(val, new TextLocation(1,1));
    private static SyntaxToken MakeVariableToken(string name) => MakeToken(TIdentifier, new TextLocation(1,1, length:name.Length), name);

    private static ASTRoot MakeRoot(params StatementNode[] statements)
    {
        var children = statements.Select(s => (TopLevelNode) (s switch {
                                                VarDeclaration var => new TopLevelVarDeclaration(var),
                                                FuncDeclaration func => new TopLevelFuncDeclaration(func),
                                                _ => new TopLevelArbitraryStament(s)
                                                }))
                                  .ToList();
        return new ASTRoot(children);
    }

    [Fact]
    public void BindVariableDeclarationForConstantInteger()
    {
        //int x = 5;
        var vardecl = new VarDeclaration(_syntaxInt,MakeVariableToken("x"), ConstCreater(5));
        var input = MakeRoot(vardecl);    
        var expected = CreateBoundProgram(
            new BoundVarDeclaration(vardecl, 
                new GlobalVariableSymbol("x", TypeSymbol.Int), 
                new BoundConstantExpression(ConstCreater(5), TypeSymbol.Int))
        );

        var boundProgram = _binder.BindProgram(input);

        //Some funky fluent assertions happen when using "BeEquivalentTo" on the boundProgram
        //boundProgram.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        boundProgram.GlobalVariables.Should().BeEquivalentTo(expected.GlobalVariables, opt => opt.RespectingRuntimeTypes());
        boundProgram.Entry.Should().BeEquivalentTo(expected.Entry, opt => opt.RespectingRuntimeTypes());
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
        var input = MakeRoot(varDecl);
        var expected = CreateBoundProgram(
            new BoundVarDeclaration(varDecl,new GlobalVariableSymbol("x", TypeSymbol.Int),
                new BoundListExpression(
                    rhs,
                    TypeSymbol.IntList,
                    new BoundExpression[]{new BoundConstantExpression(ConstCreater(5), TypeSymbol.Int)}.ToImmutableArray())
            )
        );
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
        var rhs = new ListInitExpression(MakeToken(TBracketLeft,1,1),MakeToken(TBracketRight,1,1), null);
        var varDecl = new VarDeclaration(_syntaxIntList,MakeVariableToken("x"),rhs);
        var input = MakeRoot(varDecl);
        
        var placeholderType = new PlaceholderTypeSymbol(1);
        placeholderType.Union(TypeSymbol.Int);
        
        var expected = CreateBoundProgram(
            new BoundVarDeclaration(varDecl, new GlobalVariableSymbol("x", TypeSymbol.IntList),
                    new BoundListExpression(
                        rhs,
                        new ListTypeSymbol(placeholderType), 
                        ImmutableArray<BoundExpression>.Empty)
        ));
        
        var boundProgram = _binder.BindProgram(input);

        boundProgram.GlobalVariables.Should().BeEquivalentTo(expected.GlobalVariables, opt => opt.RespectingRuntimeTypes());
        boundProgram.Entry.Should().BeEquivalentTo(expected.Entry, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void BindAccessToUndeclaredVariable()
    {
        //x;
        var nameAccess = new NameAccess(MakeToken(TIdentifier,0,0,"x"));
        var accessExpr = new AccessExpression(nameAccess);
        var exprStmnt = new ExprStatement(accessExpr);
        var input = MakeRoot(exprStmnt);
        var expected = CreateBoundProgram(new BoundErrorStatement(exprStmnt));

        var expectedErrorBag = new ErrorWarrningBag();
        expectedErrorBag.ReportNameDoesNotExist(nameAccess.Location, nameAccess.Name);

        var boundProgram = _binder.BindProgram(input);

        
        boundProgram.GlobalVariables.Should().BeEquivalentTo(expected.GlobalVariables, opt => opt.RespectingRuntimeTypes());
        boundProgram.Entry.Should().BeEquivalentTo(expected.Entry, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEquivalentTo(expectedErrorBag);
    }

    [Fact]
    public void BindListConcatenationWithEmptyListWithExplicitType()
    {
        // [] int + [2];
        var left = new ListInitExpression(MakeToken(TCurLeft,1,1), MakeToken(TCurRight,1,1), _syntaxInt);
        var plus = MakeToken(TPlus,1,1);
        var two = new ConstExpression(2, new TextLocation(1,1));
        var right = new ListInitExpression(
                MakeToken(TCurLeft,1,1), 
                new List<ExpressionNode>() { two },
                MakeToken(TCurRight,1,1));
        var binaryExpression = new BinaryExpression(left,plus,right);
        var exprStatement = new ExprStatement(binaryExpression);
        var input = MakeRoot(exprStatement);

        var expected = CreateBoundProgram(
            new BoundExprStatement(
                exprStatement, 
                new BoundBinaryExpression(
                    binaryExpression,
                    new BoundListExpression(left, TypeSymbol.IntList, ImmutableArray<BoundExpression>.Empty),
                    new BoundBinaryOperator(plus.Kind,BoundBinaryOperatorKind.ListConcat, TypeSymbol.IntList, TypeSymbol.IntList, TypeSymbol.IntList),
                    new BoundListExpression(
                        right, TypeSymbol.IntList, 
                        new List<BoundExpression>
                        {
                            new BoundConstantExpression(two, TypeSymbol.Int),
                        }.ToImmutableArray())
                    ))
        );
        
        var boundProgram = _binder.BindProgram(input);

        boundProgram.GlobalVariables.Should().BeEquivalentTo(expected.GlobalVariables, opt => opt.RespectingRuntimeTypes());
        boundProgram.Entry.Should().BeEquivalentTo(expected.Entry, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void BindListConcatenationWithEmptyListWithNoExplicitTypeSucceeds()
    {
        // [] + [2];
        var left = new ListInitExpression(MakeToken(TCurLeft,1,1), MakeToken(TCurRight,1,1), null);
        var plus = MakeToken(TPlus,1,1);
        var two = new ConstExpression(2, new TextLocation(1,1));
        var right = new ListInitExpression(
                MakeToken(TCurLeft,1,1), 
                new List<ExpressionNode>() { two },
                MakeToken(TCurRight,1,1));
        var binaryExpression = new BinaryExpression(left,plus,right);
        var exprStatement = new ExprStatement(binaryExpression);
        var input = MakeRoot(exprStatement);

        var placeholderType = new PlaceholderTypeSymbol(1);
        placeholderType.Union(TypeSymbol.Int);
        
        var leftType = new ListTypeSymbol(placeholderType);

        var expected = CreateBoundProgram(
            new BoundExprStatement(
                exprStatement,
                new BoundBinaryExpression(binaryExpression,
                new BoundListExpression(left, leftType, ImmutableArray<BoundExpression>.Empty),
                new BoundBinaryOperator(plus.Kind,BoundBinaryOperatorKind.ListConcat, leftType, TypeSymbol.IntList, leftType),
                new BoundListExpression(right, TypeSymbol.IntList, 
                    new List<BoundExpression>{new BoundConstantExpression(two, TypeSymbol.Int)}.ToImmutableArray()
                )
            )));
        
        var boundProgram = _binder.BindProgram(input);
        // var expectedBag = new ErrorWarrningBag();
        // expectedBag.ReportBinaryOperatorCannotBeApplied(new TextLocation(1,1), plus, TypeSymbol.EmptyList, TypeSymbol.IntList);
        // expectedBag.ReportTypeOfEmptyListMustBeExplicit(new TextLocation(1,1));

        boundProgram.GlobalVariables.Should().BeEquivalentTo(expected.GlobalVariables, opt => opt.RespectingRuntimeTypes());
        boundProgram.Entry.Should().BeEquivalentTo(expected.Entry, opt => opt.RespectingRuntimeTypes());
        
        _diag.Count().Should().Be(0);
        // _diag.Count().Should().Be(2);
        // _diag.Should().BeEquivalentTo(
        //     expectedBag, 
        //     opt => opt.Including(we => we.Message)
        //               .Including(we => we.IsError));
    }
}