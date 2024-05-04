using static WarmLangLexerParser.Token;
using WarmLangLexerParser;

Console.WriteLine("Something something compiler: ");

var lexer = new Lexer();
var tokens = lexer.ParseTextFile("test.test");

foreach(var token in tokens)
{
    Console.WriteLine(token);
}