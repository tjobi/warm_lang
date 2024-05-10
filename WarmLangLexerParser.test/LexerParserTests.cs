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
}