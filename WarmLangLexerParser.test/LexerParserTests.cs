namespace WarmLangLexerParser.test;

using System.Text;
using static SyntaxToken;
using static TokenKind;


public class LexerParserTests
{ 
    private readonly IFileReader _reader;

    public LexerParserTests()
    {
        _reader = Substitute.For<IFileReader>();
    }

    private Lexer GetLexer(string input)
    {
        byte[] memory = Encoding.UTF8.GetBytes(input);
        MemoryStream memoryStream = new(memory);
        _reader.GetStreamReader().Returns(new StreamReader(memoryStream));
        return new Lexer(_reader);
    }

    [Fact]
    public void TestVarDeclarationShouldSucceed()
    {
        //AAA
        string input = "var x = 25;";
        var expectedRes = new List<SyntaxToken>()
        {
            MakeToken(TVar,0,3),
            MakeToken(TIdentifier,0, 5, "x"),
            MakeToken(TEqual,0,6),
            MakeToken(TConst,0,10, intValue:25),
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().ContainInOrder(expectedRes);
    }

    [Fact]
    public void TestDeclarationShouldFail()
    {
        //TODO: Does it need to tho? Should we really crash when there is no semi colon? :D
        //AAA
        string input = "var x = 25";

        var lexer = GetLexer(input);
        var action = lexer.Lex;

        action.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("5*(4+4);", "{(CstI 5 * (CstI 4 + CstI 4));}")]
    [InlineData("5*4+4;", "{((CstI 5 * CstI 4) + CstI 4);}")]
    [InlineData("(5*(4+4))*5;", "{((CstI 5 * (CstI 4 + CstI 4)) * CstI 5);}")]
    public void TestLexerParserPrecedenceShouldSucceed(string input, string expected)
    {
        //AAA
        var lexer = GetLexer(input);
        var parser = new Parser(lexer.Lex());
        var res = parser.Parse();

        res.ToString().Should().Be(expected);
    }

    [Fact]
    public void TestLexerVariableAssignment()
    {
        string input = "var x = 5; x = 10;";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TVar,0,3),
            MakeToken(TIdentifier,0, 5, "x"),
            MakeToken(TEqual,0,6),
            MakeToken(TConst,0,9, intValue:5),
            MakeToken(TSemiColon,0,9),
            MakeToken(TIdentifier,0,12, "x"),
            MakeToken(TEqual,0,13),
            MakeToken(TConst,0,17, intValue: 10),
            MakeToken(TSemiColon,0,17), //Little weird it doesn't advance for ";" \o/
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().ContainInOrder(expected);
    }

    [Fact]
    public void TestLexerParserVariableAssignment()
    {
        string input = "var x = 5; x = 10;";
        var expected = "{(x:TVar = CstI 5);, (Assign x = CstI 10);}";

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = new Parser(tokens).Parse();

        res.ToString().Should().Be(expected);
    }

    [Fact]
    public void TestLexerParserVariableAssignment2()
    {
        string input = "x = 10;";
        var expectedNameToken = MakeToken(TIdentifier,0,1, "x");
        ExpressionNode expectedExpr = new ConstExpression(10);
        var expected = new BlockStatement(
            new List<StatementNode>()
            {
                new ExprStatement(
                    new VarAssignmentExpression(expectedNameToken, expectedExpr)
                )
            }
        );

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = new Parser(tokens).Parse();

        res.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TestLexerParserVariableAssignmentCursedState()
    {
        string input = "var x = 25; x = (var y = 3) + 4;x + y;";
        var expectedNameToken = MakeToken(TIdentifier,0,1, "x");
        var plusToken = MakeToken(TPlus, 0,0);
        ExpressionNode innerExpr = new VarAssignmentExpression(
            expectedNameToken, new BinaryExpressionNode(
                new VarDeclarationExpression(TVar, "x", new ConstExpression(3)),
                plusToken,
                new ConstExpression(4)
            )
        );
        var expected = new BlockStatement(
            new List<StatementNode>()
            {
                new ExprStatement(
                    new VarDeclarationExpression(TVar, "x", new ConstExpression(25))
                ),
                new ExprStatement(
                    new VarAssignmentExpression(expectedNameToken, innerExpr)
                ),
                new ExprStatement( new BinaryExpressionNode(
                    new VarExpression("x"),
                    plusToken,
                    new VarExpression("y")
                ))
            }
        );

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = new Parser(tokens).Parse();

        res.Should().BeEquivalentTo(expected);
    }
}