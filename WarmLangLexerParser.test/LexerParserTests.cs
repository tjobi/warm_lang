namespace WarmLangLexerParser.test;
using WarmLangLexerParser.Read;
using System.Text;
using static SyntaxToken;
using static TokenKind;
using WarmLangLexerParser.ErrorReporting;
using WarmLangLexerParser.AST.Typs;

public class LexerParserTests
{ 
    private readonly IFileReader _reader;
    private readonly ErrorWarrningBag _diag;

    public LexerParserTests()
    {
        _reader = Substitute.For<IFileReader>();
        _diag = new ErrorWarrningBag();
    }

    private Lexer GetLexer(string input)
    {
        byte[] memory = Encoding.UTF8.GetBytes(input);
        MemoryStream memoryStream = new(memory);
        _reader.GetStreamReader().Returns(new StreamReader(memoryStream));
        FileWindow window = new(_reader);
        return new Lexer(window, _diag);
    }

    private Parser GetParser(Lexer lexer) => new(lexer.Lex(), _diag);
    private Parser GetParser(IList<SyntaxToken> tokens) => new(tokens, _diag);

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
            MakeToken(TConst,2,1, intValue: 2),
            MakeToken(TPlus,2,3),
            MakeToken(TConst,2,5, intValue: 2),
            MakeToken(TSemiColon,2,6),
            MakeToken(TEOF, 4,1)
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TestLexerEmptyLineShouldSucceed()
    {
        string input = //keep the string as is, or screws with the line numbers you'd expect, hehe :)
@"int x = 25;

x;

"; 
                       
        var expectedRes = new List<SyntaxToken>()
        {
            MakeToken(TInt,1,1),
            MakeToken(TIdentifier,1, 5, "x"),
            MakeToken(TEqual,1,7),
            MakeToken(TConst,1,9, intValue:25),
            MakeToken(TSemiColon,1,11),
            MakeToken(TIdentifier,3,1, "x"),
            MakeToken(TSemiColon,3,2),
            MakeToken(TEOF, 5, 1)
        };


        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expectedRes);
    }

    [Fact]
    public void TestVarDeclarationShouldSucceed()
    {
        //AAA
        string input = "int x = 25;";
        var expectedRes = new List<SyntaxToken>()
        {
            MakeToken(TInt,1,1),
            MakeToken(TIdentifier,1,5, "x"),
            MakeToken(TEqual,1,7),
            MakeToken(TConst,1,9, intValue:25),
            MakeToken(TSemiColon,1,11),
            MakeToken(TEOF,2,1),
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expectedRes);
    }

    [Fact(Skip = "Until we decide on whether or not a semicolon is mandatory")]
    public void TestDeclarationShouldFail()
    {
        //TODO: Does it need to tho? Should we really crash when there is no semi colon? :D
        //AAA
        string input = "int x = 25";

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
        var parser = GetParser(lexer);
        var res = parser.Parse();

        res.ToString().Should().Be(expected);
    }

    [Fact]
    public void TestLexerVariableAssignment()
    {
        string input = "int x = 5; x = 10;";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TInt,1,1),
            MakeToken(TIdentifier,1,5,"x"),
            MakeToken(TEqual,1,7),
            MakeToken(TConst,1,9, intValue:5),
            MakeToken(TSemiColon,1,10),
            MakeToken(TIdentifier,1,12, "x"),
            MakeToken(TEqual,1,14),
            MakeToken(TConst,1,16, intValue: 10),
            MakeToken(TSemiColon,1,18),
            MakeToken(TEOF,2,1)
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TestLexerParserVariableAssignment()
    {
        string input = "int x = 5; x = 10;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new VarDeclarationExpression(new TypInt(), "x", new ConstExpression(5))
            ),
            new ExprStatement(
                new VarAssignmentExpression(MakeToken(TIdentifier,0,0, "x"),
                                            new ConstExpression(10)
                                            )
            )
        });

        var lexer = GetLexer(input);
        var res = GetParser(lexer).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
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
        var res = GetParser(lexer).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserVariableAssignmentCursedState()
    {
        string input = "int x = 25; x = (int y = 3) + 4;x + y;";
        var expectedNameToken = MakeToken(TIdentifier,0,1, "x");
        var plusToken = MakeToken(TPlus, 0,0);
        var expected = new BlockStatement(
            new List<StatementNode>()
            {
                new ExprStatement(
                    new VarDeclarationExpression(new TypInt(), "x", new ConstExpression(25))
                ),
                new ExprStatement(
                    new VarAssignmentExpression(
                        MakeToken(TIdentifier, 0,0, "x"), 
                        new BinaryExpression
                        (
                            new VarDeclarationExpression(new TypInt(), "y", new ConstExpression(3)),
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
        var res = GetParser(tokens).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerIfThenElseStatement()
    {
        string input = "if 0 then 2; else 5;";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TIf,1,1),
            MakeToken(TConst,1,4, intValue:0),
            MakeToken(TThen,1,6),
            MakeToken(TConst,1,11,intValue:2),
            MakeToken(TSemiColon,1,12),
            MakeToken(TElse,1,14),
            MakeToken(TConst,1,19,intValue:5),
            MakeToken(TSemiColon,1,20),
            MakeToken(TEOF, 2,1)
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
        var res = GetParser(tokens).Parse();

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
        var res = GetParser(tokens).Parse();

        res.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TestLexerFunctionDeclarationKeyword()
    {
        var input = "function f(){ int x = 10; x; }";
        var expectedTokens = new List<SyntaxToken>()
        {
            MakeToken(TFunc, 1,1),
            MakeToken(TIdentifier,1,10, "f"),
            MakeToken(TParLeft,1,11),
            MakeToken(TParRight,1,12),
            MakeToken(TCurLeft,1,13),
            MakeToken(TInt,1,15),
            MakeToken(TIdentifier,1,19, "x"),
            MakeToken(TEqual,1,21),
            MakeToken(TConst,1,23,intValue:10),
            MakeToken(TSemiColon,1,25),
            MakeToken(TIdentifier,1,27,"x"),
            MakeToken(TSemiColon,1,28),
            MakeToken(TCurRight,1,30),
            MakeToken(TEOF,2,1),
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().ContainInOrder(expectedTokens);
    }

    [Fact]
    public void TestLexerParserFunctionDeclarationKeywordNoParams()
    {
        var input = "function f(){ int x = 10; x; }";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(new FuncDeclaration(
                MakeToken(TIdentifier,0,0,"f"),
                new List<(Typ, string)>(),
                new BlockStatement(new List<StatementNode>()
                {
                    new ExprStatement(new VarDeclarationExpression(
                        new TypInt(),
                        "x",
                        new ConstExpression(10)
                    )),
                    new ExprStatement(new VarExpression("x"))
                })
            ))
        });

        var lexer = GetLexer(input);
        var res = GetParser(lexer.Lex()).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

      [Fact]
    public void TestLexerParserFunctionDeclarationKeyword()
    {
        var input = "function f(int y, int z, int l){ int x = 10; x + y; }";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(new FuncDeclaration(
                MakeToken(TIdentifier,0,0,"f"),
                new List<(Typ, string)>() 
                {
                    (new TypInt(), "y"), 
                    (new TypInt(), "z"), 
                    (new TypInt(), "l")
                },
                new BlockStatement(new List<StatementNode>()
                {
                    new ExprStatement(new VarDeclarationExpression(
                        new TypInt(),
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
        var res = GetParser(lexer.Lex()).Parse();

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
        var res = GetParser(lexer).Parse();

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
        var res = GetParser(lexer).Parse();

        res.Should().BeEquivalentTo(expected, options => 
            options.RespectingRuntimeTypes()
        );
    }

    [Fact]
    public void TestLexerParserUnaryMinus()
    {
        var input = "int x = -1;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new VarDeclarationExpression(new TypInt(), "x",
                    new UnaryExpression(MakeToken(TMinus, 0,0), new ConstExpression(1))
                )
            ),
        });

        var lexer = GetLexer(input);
        var parser = GetParser(lexer);
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserDoubleUnaryMinus()
    {
        var input = "int x = - -1;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new VarDeclarationExpression(new TypInt(), "x",
                    new UnaryExpression(MakeToken(TMinus,0,0), 
                        new UnaryExpression(MakeToken(TMinus,0,0), new ConstExpression(1))
                    )
                )
            ),
        });

        var lexer = GetLexer(input);
        var parser = GetParser(lexer);
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }
    
    [Fact]
    public void TestLexerParserDoubleUnaryPlus()
    {
        var input = "int x = + + 1;";
        var expected = new BlockStatement(new List<StatementNode>()
        {
            new ExprStatement(
                new VarDeclarationExpression(new TypInt(), "x",
                    new UnaryExpression(MakeToken(TPlus,0,0), 
                        new UnaryExpression(MakeToken(TPlus,0,0), new ConstExpression(1))
                    )
                )
            ),
        });

        var lexer = GetLexer(input);
        var parser = GetParser(lexer);
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }
}