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
    public void TestLexerCommentShouldSucceed()
    {
        string input = 
@"//This here is my comment 1
2 + 2;
//comment two
";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TConst,1,1, intValue: 2),
            MakeToken(TPlus,1,2),
            MakeToken(TConst,1,5, intValue: 2),
            MakeToken(TSemiColon,1,5),
            MakeToken(TEOF, 3,0)
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TestLexerEmptyLineShouldSucceed()
    {
        string input = //keep the string as is, or screws with the line numbers you'd expect, hehe :)
@"var x = 25;

x;

"; 
                       
        var expectedRes = new List<SyntaxToken>()
        {
            MakeToken(TVar,0,3),
            MakeToken(TIdentifier,0, 5, "x"),
            MakeToken(TEqual,0,6),
            MakeToken(TConst,0,10, intValue:25),
            MakeToken(TSemiColon,0,10),
            MakeToken(TIdentifier,2,1, "x"),
            MakeToken(TSemiColon,2,1),
            MakeToken(TEOF, 4, 0)
        };


        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expectedRes);
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
            MakeToken(TSemiColon,0,10),
            MakeToken(TEOF,1,0),
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expectedRes);
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
            MakeToken(TEOF,1,0)
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expected);
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

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserVariableAssignmentCursedState()
    {
        string input = "var x = 25; x = (var y = 3) + 4;x + y;";
        var expectedNameToken = MakeToken(TIdentifier,0,1, "x");
        var plusToken = MakeToken(TPlus, 0,0);
        var expected = new BlockStatement(
            new List<StatementNode>()
            {
                new ExprStatement(
                    new VarDeclarationExpression(TVar, "x", new ConstExpression(25))
                ),
                new ExprStatement(
                    new VarAssignmentExpression(
                        MakeToken(TIdentifier, 0,0, "x"), 
                        new BinaryExpression
                        (
                            new VarDeclarationExpression(TVar, "y", new ConstExpression(3)),
                            MakeToken(TPlus, 0,0),
                            new ConstExpression(4)
                        )
                    )
                ),
                new ExprStatement( new BinaryExpression(
                    new VarExpression("x"),
                    plusToken,
                    new VarExpression("y")
                ))
            }
        );

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = new Parser(tokens).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerIfThenElseStatement()
    {
        string input = "if 0 then 2; else 5;";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TIf,0,2),
            MakeToken(TConst,0,4, intValue:0),
            MakeToken(TThen,0,9),
            MakeToken(TConst,0,11,intValue:2),
            MakeToken(TSemiColon,0,11),
            MakeToken(TElse,0,17),
            MakeToken(TConst,0,19,intValue:5),
            MakeToken(TSemiColon,0,19),
            MakeToken(TEOF, 1,0)
        };

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();

        tokens.Should().ContainInOrder(expected);
    }

    [Fact]
    public void TestLexerParserIfThenElseStatement()
    {
        string input = "if 0 then 2; else 5;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new IfStatement(
                new ConstExpression(0),
                new ExprStatement(new ConstExpression(2)),
                new ExprStatement(new ConstExpression(5))
            )
        });

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = new Parser(tokens).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserIfThenStatement()
    {
        string input = "if 0 then 2;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new IfStatement(
                new ConstExpression(0),
                new ExprStatement(new ConstExpression(2)),
                null
            )
        });

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = new Parser(tokens).Parse();

        res.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TestLexerFunctionDeclarationKeyword()
    {
        var input = "function f(){ var x = 10; x; }";
        var expectedTokens = new List<SyntaxToken>()
        {
            MakeToken(TFunc, 0,8),
            MakeToken(TIdentifier, 0, 10, "f"),
            MakeToken(TParLeft, 0,10),
            MakeToken(TParRight, 0,11),
            MakeToken(TCurLeft,0,12),
            MakeToken(TVar,0,17),
            MakeToken(TIdentifier,0,19, "x"),
            MakeToken(TEqual,0,20),
            MakeToken(TConst, 0,24,intValue:10),
            MakeToken(TSemiColon,0,24),
            MakeToken(TIdentifier,0,27,"x"),
            MakeToken(TSemiColon,0,27),
            MakeToken(TCurRight,0,29),
            MakeToken(TEOF,1,0),
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().ContainInOrder(expectedTokens);
    }

    [Fact]
    public void TestLexerParserFunctionDeclarationKeywordNoParams()
    {
        var input = "function f(){ var x = 10; x; }";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(new FuncDeclaration(
                MakeToken(TIdentifier,0,0,"f"),
                new List<string>(),
                new BlockStatement(new List<StatementNode>()
                {
                    new ExprStatement(new VarDeclarationExpression(
                        TVar,
                        "x",
                        new ConstExpression(10)
                    )),
                    new ExprStatement(new VarExpression("x"))
                })
            ))
        });

        var lexer = GetLexer(input);
        var res = new Parser(lexer.Lex()).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

      [Fact]
    public void TestLexerParserFunctionDeclarationKeyword()
    {
        var input = "function f(y, z, l){ var x = 10; x + y; }";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(new FuncDeclaration(
                MakeToken(TIdentifier,0,0,"f"),
                new List<string>(){"y", "z", "l"},
                new BlockStatement(new List<StatementNode>()
                {
                    new ExprStatement(new VarDeclarationExpression(
                        TVar,
                        "x",
                        new ConstExpression(10)
                    )),
                    new ExprStatement(new BinaryExpression(
                        new VarExpression("x"),
                        MakeToken(TPlus, 0,0),
                        new VarExpression("y")
                    ))
                })
            ))
        });

        var lexer = GetLexer(input);
        var res = new Parser(lexer.Lex()).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TextLexerParserFunctionCall()
    {
        var input = "f();";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new CallExpression(MakeToken(TIdentifier, 0,0, "f"), new List<ExpressionNode>())
            )
        });

        var lexer = GetLexer(input);
        var res = new Parser(lexer.Lex()).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TextLexerParserFunctionCallWithParams()
    {
        var input = "f(2+5,10);";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new CallExpression(MakeToken(TIdentifier, 0,0, "f"), new List<ExpressionNode>()
                {
                    new BinaryExpression(
                        new ConstExpression(2),
                        MakeToken(TPlus, 0,0),
                        new ConstExpression(5)
                    ),
                    new ConstExpression(10)
                })
            )
        });

        var lexer = GetLexer(input);
        var res = new Parser(lexer.Lex()).Parse();

        res.Should().BeEquivalentTo(expected, options => 
            options.RespectingRuntimeTypes()
        );
    }

    [Fact]
    public void TestLexerParserUnaryMinus()
    {
        var input = "var x = -1;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new VarDeclarationExpression(TVar, "x",
                    new UnaryExpression(MakeToken(TMinus, 0,0), new ConstExpression(1))
                )
            ),
        });

        var lexer = GetLexer(input);
        var parser = new Parser(lexer.Lex());
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserDoubleUnaryMinus()
    {
        var input = "var x = - -1;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new VarDeclarationExpression(TVar, "x",
                    new UnaryExpression(MakeToken(TMinus,0,0), 
                        new UnaryExpression(MakeToken(TMinus,0,0), new ConstExpression(1))
                    )
                )
            ),
        });

        var lexer = GetLexer(input);
        var parser = new Parser(lexer.Lex());
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }
    
    [Fact]
    public void TestLexerParserDoubleUnaryPlus()
    {
        var input = "var x = + + 1;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new VarDeclarationExpression(TVar, "x",
                    new UnaryExpression(MakeToken(TPlus,0,0), 
                        new UnaryExpression(MakeToken(TPlus,0,0), new ConstExpression(1))
                    )
                )
            ),
        });

        var lexer = GetLexer(input);
        var parser = new Parser(lexer.Lex());
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }
}