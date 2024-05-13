namespace WarmLangLexerParser.Exceptions;

public class ParserException : Exception 
{
    public int Line { get; }
    public int Column { get; }
    private readonly string message;
    public override string Message => message;
    public ParserException(string msg, int line, int col) : base()
    {
        Line = line;
        Column = col;
        message = $"Parser failed: On line {line}, column {col} with message \"{msg}\"";
    }

    public ParserException(string msg, SyntaxToken token) : this(msg, token.Line, token.Column) { }
}