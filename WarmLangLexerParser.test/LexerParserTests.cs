namespace WarmLangLexerParser.test;
using WarmLangLexerParser.Read;
using System.Text;
using static SyntaxToken;
using static TokenKind;
using WarmLangLexerParser.ErrorReporting;
using WarmLangLexerParser.AST.TypeSyntax;
using Xunit.Sdk;

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

    private BlockStatement MakeEntryBlock(TextLocation start, TextLocation end, params StatementNode[] statements)
    {
        var open = MakeToken(TBadToken, start);
        var close = MakeToken(TBadToken, end);
        return new BlockStatement(open, statements.ToList(), close);
    }

    private BlockStatement MakeEntryBlock(string input, params StatementNode[] statements)
    => MakeEntryBlock(new TextLocation(1,1), new TextLocation(1,input.Length+1), statements);

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
            MakeToken(TEOF, 3,14)
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
            MakeToken(TInt,new TextLocation(1,1,length:3)),
            MakeToken(TIdentifier,1, 5, "x"),
            MakeToken(TEqual,1,7),
            MakeToken(TConst,new TextLocation(1,9,length:2), intValue:25),
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
            MakeToken(TInt,new TextLocation(1,1, length:3)),
            MakeToken(TIdentifier,1,5, "x"),
            MakeToken(TEqual,1,7),
            MakeToken(TConst,new TextLocation(1,9,length:2),intValue:25),
            MakeToken(TSemiColon,1,11),
            MakeToken(TEOF,1,12),
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expectedRes);
    }

    [Fact]
    public void TestLexerForNumericFollowedByEOF()
    {
        //AAA
        string input = "int x = 25";
        var expectedTokens = new List<SyntaxToken>()
        {
            MakeToken(TInt,new TextLocation(1,1,length:3)),
            MakeToken(TIdentifier,1,5, "x"),
            MakeToken(TEqual,1,7),
            MakeToken(TConst,new TextLocation(1,9,length:2), intValue:25)
        };

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();

        tokens.Should().ContainInConsecutiveOrder(expectedTokens);
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerForIdentifierFollowedByEOF()
    {
        //AAA
        string input = "int x = yyyyyy";
        var expectedTokens = new List<SyntaxToken>()
        {
            MakeToken(TInt,new TextLocation(1,1,length:3)),
            MakeToken(TIdentifier,1,5, "x"),
            MakeToken(TEqual,1,7),
            MakeToken(TIdentifier,new TextLocation(1,9,length:6), "yyyyyy"),
            MakeToken(TEOF, 1,15)
        };

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();

        tokens.Should().ContainInConsecutiveOrder(expectedTokens);
        _diag.Should().BeEmpty();
    }

    [Theory]
    [InlineData("5*(4+4);", "{(Cst 5 * (Cst 4 + Cst 4));}")]
    [InlineData("5*4+4;", "{((Cst 5 * Cst 4) + Cst 4);}")]
    [InlineData("(5*(4+4))*5;", "{((Cst 5 * (Cst 4 + Cst 4)) * Cst 5);}")]
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
            MakeToken(TInt,new TextLocation(1,1,length:3)),
            MakeToken(TIdentifier,1,5,"x"),
            MakeToken(TEqual,1,7),
            MakeToken(TConst,1,9, intValue:5),
            MakeToken(TSemiColon,1,10),
            MakeToken(TIdentifier,1,12, "x"),
            MakeToken(TEqual,1,14),
            MakeToken(TConst,new TextLocation(1,16, length:2), intValue: 10),
            MakeToken(TSemiColon,1,18),
            MakeToken(TEOF,1,19)
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TestLexerParserVariableAssignment()
    {
        string input = "int x = 5; x = 10;";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxInt(new TextLocation(1,1,length:3)),
                MakeToken(TIdentifier,new TextLocation(1,5), "x"),
                new ConstExpression(MakeToken(TConst, new TextLocation(1,9), intValue:5))),
            new ExprStatement(
                new AssignmentExpression(
                    new NameAccess(MakeToken(TIdentifier,1,12, "x")),
                    MakeToken(TEqual, 1,14),
                    new ConstExpression(MakeToken(TConst, new TextLocation(1,16, length:2), intValue: 10))
                    )
            )
        );

        var lexer = GetLexer(input);
        var res = GetParser(lexer).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserVariableAssignment2()
    {
        string input = "x = 10;";
        var expectedNameToken = MakeToken(TIdentifier,1,1, "x");
        ExpressionNode expectedExpr = new ConstExpression(MakeToken(TConst, new TextLocation(1,5,length:2), intValue:10));
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new AssignmentExpression(
                    new NameAccess(expectedNameToken),
                    MakeToken(TEqual, new TextLocation(1,3)),
                    expectedExpr)
            )
        );

        var lexer = GetLexer(input);
        var res = GetParser(lexer).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerIfElseStatement()
    {
        string input = "if 0 { 2; } else { 5; }";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TIf,new TextLocation(1,1, length:2)),
            MakeToken(TConst,1,4, intValue:0),
            MakeToken(TCurLeft,1,6),
            MakeToken(TConst,1,8,intValue:2),
            MakeToken(TSemiColon,1,9),
            MakeToken(TCurRight,1,11),
            MakeToken(TElse,new TextLocation(1,13,length:4)),
            MakeToken(TCurLeft,1,18),
            MakeToken(TConst,1,20,intValue:5),
            MakeToken(TSemiColon,1,21),
            MakeToken(TCurRight,1,23),
            MakeToken(TEOF, 1,24)
        };

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();

        tokens.Should().ContainInOrder(expected);
    }

    [Fact]
    public void TestLexerParserIfThenElseStatement()
    {
        string input = "if 0 {2;} else {5;}";
        var expected = MakeEntryBlock(input,
            new IfStatement(
                MakeToken(TIf,1,1,1,3),
                new ConstExpression(0, new TextLocation(1,4)),
                new BlockStatement(
                    MakeToken(TCurLeft,1,6),
                    new List<StatementNode>()
                    {
                        new ExprStatement(new ConstExpression(2, new TextLocation(1,7)))
                    },
                    MakeToken(TCurLeft,1,9)
                ),
                new BlockStatement(
                    MakeToken(TCurLeft,1,16),
                    new List<StatementNode>()
                    {
                        new ExprStatement(new ConstExpression(5, new TextLocation(1,17)))
                    },
                    MakeToken(TCurLeft,1,19)
                )
            )
        );

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = GetParser(tokens).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserIfThenStatement()
    {
        string input = "if 0 { 2; }";
        var expected = MakeEntryBlock(input,
            new IfStatement(
                MakeToken(TIf,1,1),
                new ConstExpression(MakeToken(TConst,1,4,intValue:0)),
                new BlockStatement(
                    MakeToken(TCurLeft,1,6),
                    new List<StatementNode>()
                    {
                        new ExprStatement(new ConstExpression(MakeToken(TConst,1,8, intValue:2))),
                    },
                    MakeToken(TCurRight,1,11)
                ),
                null
            )
        );

        var lexer = GetLexer(input);
        var tokens = lexer.Lex();
        var res = GetParser(tokens).Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerFunctionDeclarationKeyword()
    {
        var input = "function f(){ int x = 10; x; }";
        var expectedTokens = new List<SyntaxToken>()
        {
            MakeToken(TFunc, new TextLocation(1,1, length:8)),
            MakeToken(TIdentifier,1,10, "f"),
            MakeToken(TParLeft,1,11),
            MakeToken(TParRight,1,12),
            MakeToken(TCurLeft,1,13),
            MakeToken(TInt,new TextLocation(1,15, length:3)),
            MakeToken(TIdentifier,1,19, "x"),
            MakeToken(TEqual,1,21),
            MakeToken(TConst,new TextLocation(1,23, length:2),intValue:10),
            MakeToken(TSemiColon,1,25),
            MakeToken(TIdentifier,1,27,"x"),
            MakeToken(TSemiColon,1,28),
            MakeToken(TCurRight,1,30),
            MakeToken(TEOF,1,31),
        };

        var lexer = GetLexer(input);
        var res = lexer.Lex();

        res.Should().ContainInOrder(expectedTokens);
    }

    [Fact]
    public void TestLexerParserFunctionDeclarationKeywordNoParams()
    {
        var input = "function f(){ int x = 10; x; }";
        var expected = MakeEntryBlock(input,
            new FuncDeclaration(
                MakeToken(TFunc,new TextLocation(1,1,length:8)),
                MakeToken(TIdentifier,1,10,"f"),
                new List<(TypeSyntaxNode, SyntaxToken)>(),
                new BlockStatement(MakeToken(TCurLeft,1,13),
                new List<StatementNode>()
                {
                    new VarDeclaration(
                        new TypeSyntaxInt(new TextLocation(1,15,length:3)),
                        MakeToken(TIdentifier,1,19, "x"),
                        new ConstExpression(MakeToken(TConst,new TextLocation(1,23, length:2), intValue:10))
                    ),
                    new ExprStatement(new AccessExpression(new NameAccess(MakeToken(TIdentifier,1,27,"x"))))
                },
                MakeToken(TCurRight,1,30))
            )
        );

        var lexer = GetLexer(input);
        var res = GetParser(lexer.Lex()).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

      [Fact]
    public void TestLexerParserFunctionDeclarationKeyword()
    {
        var input = "function f(int y, int z, int l){ int x = 10; x + y; }";
        var expected = MakeEntryBlock(input,
            new FuncDeclaration(
                MakeToken(TFunc,1,1,1,9),
                MakeToken(TIdentifier,1,10,"f"),
                new List<(TypeSyntaxNode, SyntaxToken)>() 
                {
                    (new TypeSyntaxInt(new TextLocation(1,12,length:3)), MakeToken(TIdentifier,1,16,"y")), 
                    (new TypeSyntaxInt(new TextLocation(1,19,length:3)), MakeToken(TIdentifier,1,23,"z")), 
                    (new TypeSyntaxInt(new TextLocation(1,26,length:3)), MakeToken(TIdentifier,1,30,"l"))
                },
                new BlockStatement(
                    MakeToken(TCurLeft,1,32),
                    new List<StatementNode>()
                    {
                        new VarDeclaration(
                            new TypeSyntaxInt(new TextLocation(1,34, length:3)),
                            MakeToken(TIdentifier,1,38, "x"),
                            new ConstExpression(10, new TextLocation(1,42,1,44))
                        ),
                        new ExprStatement(new BinaryExpression(
                            new AccessExpression(new NameAccess(MakeToken(TIdentifier,1,46,"x"))),
                            MakeToken(TPlus, 1,48),
                            new AccessExpression(new NameAccess(MakeToken(TIdentifier,1,50,"y")))
                        ))
                    },
                    MakeToken(TCurRight,1,53))
            )
        );

        var lexer = GetLexer(input);
        var res = GetParser(lexer.Lex()).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TextLexerParserFunctionCall()
    {
        var input = "f();";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new CallExpression(
                    MakeToken(TIdentifier, 1,1, "f"),
                    MakeToken(TParLeft,1,2),
                    new List<ExpressionNode>(),
                    MakeToken(TParRight,1,3))
            )
        );

        var lexer = GetLexer(input);
        var res = GetParser(lexer).Parse();

        res.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void TextLexerParserFunctionCallWithParams()
    {
        var input = "f(2+5,10);";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new CallExpression(
                    MakeToken(TIdentifier, 1,1, "f"), 
                    MakeToken(TParLeft,1,2),
                    new List<ExpressionNode>()
                    {
                        new BinaryExpression(
                            new ConstExpression(2, new TextLocation(1,3)),
                            MakeToken(TPlus, 1,4),
                            new ConstExpression(5, new TextLocation(1,5))
                        ),
                        new ConstExpression(10, new TextLocation(1,7,1,9))
                    },
                    MakeToken(TParRight,1,9))
            )
        );

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
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxInt(new TextLocation(1,1,length:3)),
                MakeToken(TIdentifier,1,5,"x"),
                new UnaryExpression(
                    MakeToken(TMinus, 1,9),
                    new ConstExpression(1, new TextLocation(1,10)))
            )
        );

        var lexer = GetLexer(input);
        var parser = GetParser(lexer);
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserDoubleUnaryMinus()
    {
        var input = "int x = - -1;";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxInt(new TextLocation(1,1,length:3)),
                MakeToken(TIdentifier,1,5, "x"),
                new UnaryExpression(MakeToken(TMinus,1,9), 
                    new UnaryExpression(MakeToken(TMinus,1,11), new ConstExpression(1, new TextLocation(1,12)))
                )
            )
            );

        var lexer = GetLexer(input);
        var parser = GetParser(lexer);
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }
    
    [Fact]
    public void TestLexerParserDoubleUnaryPlus()
    {
        var input = "int x = + + 1;";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxInt(new TextLocation(1,1, length:3)),
                MakeToken(TIdentifier,1,5,"x"),
                new UnaryExpression(MakeToken(TPlus,1,9), 
                    new UnaryExpression(MakeToken(TPlus,1,11), new ConstExpression(1, new TextLocation(1,13)))
                )
            )
        );
        
        var lexer = GetLexer(input);
        var parser = GetParser(lexer);
        var res = parser.Parse();

        res.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserForListInitialization()
    {
        var input = "int[] xs = [1,2,3+5];";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxList(new TextLocation(1,1,length:5),
                    new TypeSyntaxInt(new TextLocation(1,1,length:3))),
                MakeToken(TIdentifier,new TextLocation(1,7,length:2), "xs"),
                new ListInitExpression(
                    MakeToken(TBracketLeft,1,12),
                    new List<ExpressionNode>()
                    {
                        new ConstExpression(1, new TextLocation(1,13)),
                        new ConstExpression(2, new TextLocation(1,15)),
                        new BinaryExpression(
                            new ConstExpression(3, new TextLocation(1,17)),
                            MakeToken(TPlus, 1,18),
                            new ConstExpression(5, new TextLocation(1,19))
                        )
                    },
                    MakeToken(TBracketRight,1,20)))
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserForListSubscript()
    {
        var input = "xs[2]";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new AccessExpression(
                    new SubscriptAccess(
                        new NameAccess(MakeToken(TIdentifier,new TextLocation(1,1,length:2),"xs")),
                        new ConstExpression(2, new TextLocation(1,4))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }

    [Fact]
    public void TestLexerParserForListElementAssignment()
    {
        var input = "xs[2] = 25;";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new AssignmentExpression(
                    new SubscriptAccess(
                        new NameAccess(MakeToken(TIdentifier,new TextLocation(1,1, length:2),"xs")),
                        new ConstExpression(2, new TextLocation(1,4))
                    ),
                    MakeToken(TEqual,1,7),
                    new ConstExpression(25, new TextLocation(1,9,length:2))
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserListElementAssignmentEqualsListElement()
    {
        var input = "xs[2] = xs[10];";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new AssignmentExpression(
                    new SubscriptAccess(
                        new NameAccess(MakeToken(TIdentifier,new TextLocation(1,1,length:2),"xs")),
                        new ConstExpression(2, new TextLocation(1,4))
                    ),
                    MakeToken(TEqual,1,7),
                    new AccessExpression(
                        new SubscriptAccess(
                            new NameAccess(MakeToken(TIdentifier, new TextLocation(1,9,length:2),"xs")),
                            new ConstExpression(10, new TextLocation(1,12,length:2)))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserForEmptyList()
    {
        var input = "int[] xs = [];";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxList(
                    new TextLocation(1,1,length:3+2), new TypeSyntaxInt(new TextLocation(1,1, length:3))),
                MakeToken(TIdentifier,new TextLocation(1,7,length:2), "xs"),
                new ListInitExpression(
                    MakeToken(TBracketLeft,1,12),
                    new List<ExpressionNode>(),
                    MakeToken(TBracketRight,1,13))
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserAddElementToList()
    {
        var input = "int[] xs = []; xs :: 5;";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxList(
                    new TextLocation(1,1, length:3+2), 
                    new TypeSyntaxInt(new TextLocation(1,1,length:3))),
                MakeToken(TIdentifier,new TextLocation(1,7,length:2), "xs"),
                new ListInitExpression(
                    MakeToken(TBracketLeft,1,12),
                    new List<ExpressionNode>(),
                    MakeToken(TBracketRight,1,13))
            ),
            new ExprStatement(
                new BinaryExpression(
                    new AccessExpression(new NameAccess(MakeToken(TIdentifier,new TextLocation(1,16,length:2),"xs"))),
                    MakeToken(TDoubleColon,new TextLocation(1,19, length:2)),
                    new ConstExpression(5, new TextLocation(1,22))
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserRemoveElementFromList()
    {
        var input = "<-xs;";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TLeftArrow,new TextLocation(1,1, length:2)),
                    new AccessExpression(
                        new NameAccess(MakeToken(TIdentifier,new TextLocation(1,3,length:2), "xs")))
            ))
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserRemoveAddPrecedence()
    {
        var input = "<- xs :: 20;";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TLeftArrow,new TextLocation(1,1,length:2)),
                    new BinaryExpression(
                        new AccessExpression(new NameAccess(MakeToken(TIdentifier,new TextLocation(1,4,length:2), "xs"))),
                        MakeToken(TDoubleColon,new TextLocation(1,7, length:2)),
                        new ConstExpression(20, new TextLocation(1,10, length:2))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserSuffixUnaryPrecedence()
    {
        var input = "<- <- xs;";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TLeftArrow,new TextLocation(1,1, length:2)),
                    new UnaryExpression(
                        MakeToken(TLeftArrow,new TextLocation(1,4, length:2)),
                        new AccessExpression(new NameAccess(MakeToken(TIdentifier,new TextLocation(1,7, length:2), "xs")))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParsePrefixAndSuffixPrecedence()
    {
        var input = "<- -xs;";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TLeftArrow,new TextLocation(1,1, length:2)),
                    new UnaryExpression(
                        MakeToken(TMinus,1,4),
                        new AccessExpression(new NameAccess(MakeToken(TIdentifier,new TextLocation(1,5, length:2), "xs")))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParseParanthesisAndUnaryOperator()
    {
        var input = "-(<- xs);";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TMinus,1,1),
                    new UnaryExpression(
                        MakeToken(TLeftArrow,new TextLocation(1,3, length:2)),
                        new AccessExpression(
                            new NameAccess(MakeToken(TIdentifier,new TextLocation(1,6, length:2), "xs")))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParseParanthesisAndFunctionCall()
    {
        var input = "-(func(2));";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TMinus,1,1),
                    new CallExpression(
                        MakeToken(TIdentifier,new TextLocation(1,3,length:4), "func"),
                        MakeToken(TParLeft,1,7),
                        new List<ExpressionNode>(){new ConstExpression(2, new TextLocation(1,8))},
                        MakeToken(TParRight,1,9)
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParseParanthesisAndVariableAssignment()
    {
        var input = "<-(xs = []);";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TLeftArrow, new TextLocation(1,1,length:2)),
                    new AssignmentExpression(
                        new NameAccess(MakeToken(TIdentifier,new TextLocation(1,4,length:2), "xs")),
                        MakeToken(TEqual,1,7),
                        new ListInitExpression(
                            MakeToken(TBracketLeft,1,9),
                            new List<ExpressionNode>(),
                            MakeToken(TBracketLeft,1,10))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParseDoubleSubscript()
    {
        var input = "xs[1][1];";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new AccessExpression(
                    new SubscriptAccess(
                        new SubscriptAccess(
                            new NameAccess(
                                MakeToken(TIdentifier,new TextLocation(1,1, length:2), "xs")),
                                new ConstExpression(1, new TextLocation(1,4))),
                        new ConstExpression(1, new TextLocation(1,7))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParseSubscriptIntoCallExpression()
    {
        var input = "returnsList()[1];";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new AccessExpression(
                    new SubscriptAccess(
                        new ExprAccess(
                            new CallExpression(
                                MakeToken(TIdentifier,new TextLocation(1,1,1,12),"returnsList"),
                                MakeToken(TParLeft,1,12),
                                new List<ExpressionNode>(),
                                MakeToken(TParLeft,1,13)
                                )),
                        new ConstExpression(1, new TextLocation(1,15))
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerParserLeftArrowFunctionCall()
    {
        var input = "<- f();";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new UnaryExpression(
                    MakeToken(TLeftArrow,new TextLocation(1,1, length:2)),
                    new CallExpression(
                        MakeToken(TIdentifier,1,4,"f"),
                        MakeToken(TParLeft,1,5),
                        new List<ExpressionNode>(),
                        MakeToken(TParRight,1,6)
                    )
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerParserMissingSemicolon()
    {
        var input = "x+2";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new BinaryExpression(
                    new AccessExpression(new NameAccess(MakeToken(TIdentifier,1,1,"x"))),
                    MakeToken(TPlus,1,2),
                    new ConstExpression(2, new TextLocation(1,3))
                )
            )
        );
        var expectedDiag = new ErrorWarrningBag();
        expectedDiag.ReportUnexpectedToken(TSemiColon,TEOF, new TextLocation(1,4));

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().ContainSingle();
        _diag.Should().BeEquivalentTo(expectedDiag);
    }

    [Fact]
    public void LexerParserMissingExpressionAndSemicolon()
    {
        var input = "x+;";
        var expected = MakeEntryBlock(input,
            new ExprStatement(
                new BinaryExpression(
                    new AccessExpression(new NameAccess(MakeToken(TIdentifier,1,1,"x"))),
                    MakeToken(TPlus,1,2),
                    new ErrorExpressionNode(MakeToken(TSemiColon,1,3))
                )
            )
        );
    
        var expectedDiag = new ErrorWarrningBag();
        expectedDiag.ReportInvalidExpression(MakeToken(TSemiColon,1,3));
        expectedDiag.ReportUnexpectedToken(TSemiColon, TEOF, new TextLocation(1,4));

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().HaveCount(2);
        _diag.Should().BeEquivalentTo(expectedDiag);
    }

    [Fact]
    public void LexerParserWhileStatement()
    {
        var input = "while x {}";
        var expected = MakeEntryBlock(input,
            new WhileStatement(
                MakeToken(TWhile,1,1, 1,6),
                new AccessExpression(new NameAccess(MakeToken(TIdentifier,new TextLocation(1,7),"x"))),
                new BlockStatement(
                    MakeToken(TCurLeft,1,9),
                    new List<StatementNode>(),
                    MakeToken(TCurRight,1,10))
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerParserFaultyWhileStatement()
    {
        var input = "while x f();";
        var expected = MakeEntryBlock(input,
            new WhileStatement(
                MakeToken(TWhile,new TextLocation(1,1, length:6)),
                new AccessExpression(new NameAccess(MakeToken(TIdentifier,1,7,"x"))),
                new BlockStatement(
                    MakeToken(TBadToken,1,9),
                    new List<StatementNode>()
                    {
                        new ExprStatement(
                            new CallExpression(
                                MakeToken(TIdentifier,1,9,"f"),
                                MakeToken(TParLeft,1,10),
                                new List<ExpressionNode>(),
                                MakeToken(TParRight,1,11)
                            )
                        )
                    },
                    MakeToken(TBadToken,1,13))
            )
        );
        var expectedErrorBag = new ErrorWarrningBag();
        expectedErrorBag.ReportWhileExpectedBlockStatement(MakeToken(TIdentifier,1,9));
        expectedErrorBag.ReportUnexpectedToken(TCurLeft, TIdentifier, new TextLocation(1,9));
        expectedErrorBag.ReportUnexpectedToken(TCurRight, TEOF, new TextLocation(1,13));


        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEquivalentTo(expectedErrorBag);
        _diag.Should().HaveCount(3);
    }

    [Fact]
    public void LexerParserWhileStatementWithCont()
    {
        var input = "while x : 2+2 {}";
        var expected = MakeEntryBlock(input,
            new WhileStatement(
                MakeToken(TWhile,1,1,1,6),
                new AccessExpression(new NameAccess(MakeToken(TIdentifier,1,7,"x"))),
                new BlockStatement(
                    MakeToken(TCurLeft,1,15),
                    new List<StatementNode>(),
                    MakeToken(TCurRight,1,16)),
                new List<ExpressionNode>()
                {
                    new BinaryExpression(
                        new ConstExpression(2, new TextLocation(1,11)),
                        MakeToken(TPlus,1,12),
                        new ConstExpression(2, new TextLocation(1,13))
                    )
                }
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerParserWhileStatementWithMoreThanOnecont()
    {
        var input = "while x : 2+2,5+5 {}";
        var expected = MakeEntryBlock(input,
            new WhileStatement(
                MakeToken(TWhile, new TextLocation(1,1,1,6)),
                new AccessExpression(new NameAccess(MakeToken(TIdentifier,new TextLocation(1,7),"x"))),
                new BlockStatement(
                    MakeToken(TCurLeft,1,19),
                    new List<StatementNode>(),
                    MakeToken(TCurRight,1,20)),
                new List<ExpressionNode>()
                {
                    new BinaryExpression(
                        new ConstExpression(2, new TextLocation(1,11)),
                        MakeToken(TPlus,1,12),
                        new ConstExpression(2, new TextLocation(1,13))
                    ),
                    new BinaryExpression(
                        new ConstExpression(5,  new TextLocation(1,15)),
                        MakeToken(TPlus,1,16),
                        new ConstExpression(5,  new TextLocation(1,17))
                    ),
                }
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerParserFunctionWithReturnType()
    {
        var input = "function f() int {2;}";
        var expected = MakeEntryBlock(input,
            new FuncDeclaration(
                MakeToken(TFunc,new TextLocation(1,1,length:8)),
                MakeToken(TIdentifier,1,10, "f"),
                new List<(TypeSyntaxNode, SyntaxToken)>(),
                new TypeSyntaxInt(new TextLocation(1,14,1,17)),
                new BlockStatement(MakeToken(TCurLeft,1,18),
                new List<StatementNode>()
                {
                    new ExprStatement(new ConstExpression(2, new TextLocation(1,19))),
                },
                MakeToken(TCurRight,1,21))
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }


    [Fact]
    public void TestReturnStatementNoExpression()
    {
        var input = "return;";
        var expected = MakeEntryBlock(input,
            new ReturnStatement(MakeToken(TReturn, new TextLocation(1,1,length:6)), null)
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestReturnStatementWithExpression()
    {
        var input = "return 2+2;";
        var expected = MakeEntryBlock(input,
            new ReturnStatement(
                MakeToken(TReturn, new TextLocation(1,1,length:6)),
                new BinaryExpression(
                    new ConstExpression(2,new TextLocation(1,8)),
                    MakeToken(TPlus, new TextLocation(1,9)),
                    new ConstExpression(2,new TextLocation(1,10))
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void TestLexerParserIfElse()
    {
        var input = "if 0 { } else if 1 { }";
        var expected = MakeEntryBlock(input,
            new IfStatement(
                MakeToken(TIf, new TextLocation(1,1,length:2)),
                new ConstExpression(0, new TextLocation(1,4)),
                new BlockStatement(MakeToken(TCurLeft, 1,6), new List<StatementNode>(), MakeToken(TCurRight,1,8)),
                new IfStatement(
                    MakeToken(TIf,new TextLocation(1,15,length:2)), 
                    new ConstExpression(1, new TextLocation(1,18)),
                    new BlockStatement(MakeToken(TCurLeft, 1,20), new List<StatementNode>(), MakeToken(TCurRight,1,22)),
                    null
                )
            ) 
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerParserTestStringLiteral()
    {
        var input = """string s = "hi there";""";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxString(new TextLocation(1,1,length:6)),
                MakeToken(TIdentifier,1,8,"s"),
                new ConstExpression(
                    new SyntaxToken(TStringLiteral, new TextLocation(1,12,length:10), "hi there",0)
                )
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerParserTestStringLiteralPlus()
    {
        var input = """string s = "hi there" + "y";""";
        var expected = MakeEntryBlock(input,
            new VarDeclaration(
                new TypeSyntaxString(new TextLocation(1,1,length:6)),
                MakeToken(TIdentifier,1,8,"s"),
                new BinaryExpression(
                    new ConstExpression(
                        new SyntaxToken(TStringLiteral, new TextLocation(1,12,length:10), "hi there",0)),
                    MakeToken(TPlus,1,23),
                    new ConstExpression(
                        new SyntaxToken(TStringLiteral, new TextLocation(1,25,length:3), "y",0))
                )   
            )
        );

        var parser = GetParser(GetLexer(input));
        var result = parser.Parse();

        result.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
        _diag.Should().BeEmpty();
    }

    [Fact]
    public void LexerStringLiteralNewLine()
    {
        var input = @"string s = ""h
h"";";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TString, new TextLocation(1,1,length:6)),
            MakeToken(TIdentifier,1,8,"s"),
            MakeToken(TEqual,1,10),
            new(TStringLiteral, new TextLocation(1,12,2,1), "h",0),
            MakeToken(TIdentifier,2,1, "h"),
            new(TStringLiteral,new TextLocation(2,2, length:3), ";",0), //length 3, because it counts the ""
            MakeToken(TEOF,2,5)
        };

        var lexer = GetLexer(input);
        var result = lexer.Lex();
        var expectedBag = new ErrorWarrningBag();
        expectedBag.ReportNewLineStringLiteral(new TextLocation(1,12,2,1));

        result.Should().BeEquivalentTo(expected);
        _diag.Should().BeEquivalentTo(expectedBag);
    }

    [Fact]
    public void LexerStringEscaping()
    {
        var input = """string s = "y\ny";""";
        var expected = new List<SyntaxToken>()
        {
            MakeToken(TString, new TextLocation(1,1,length:6)),
            MakeToken(TIdentifier,1,8,"s"),
            MakeToken(TEqual,1,10),
            new(TStringLiteral, new TextLocation(1,12,length:6), "y\ny",0),
            MakeToken(TSemiColon,1,18),
            MakeToken(TEOF,1,19)
        };

        var lexer = GetLexer(input);
        var result = lexer.Lex();

        result[3].Name!.Should().Be("y\ny");
        result.Should().BeEquivalentTo(expected);
        _diag.Should().BeEmpty();
    }
}