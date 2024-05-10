namespace WarmLangLexerParser.Exceptions;

public class LexerException : Exception 
{
    public int Line { get; }
    public int Column { get; }
    private readonly string message;
    public override string Message => message;
    public LexerException(string file, string msg, int line, int col) : base()
    {
        Line = line;
        Column = col;
        message = $"In file: {file}, line: {line}, column: {col} -> {msg}";
    }
}