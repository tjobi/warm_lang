namespace WarmLangLexerParser.ErrorReporting;

public sealed class ReportedErrorWarning
{
    public string Message { get; }
    public bool IsError { get; }
    public int Line { get; }
    public int Column { get; }

    public ReportedErrorWarning(string msg, bool isError, int line, int column)
    {
        Message = msg;
        IsError = isError;
        Line = line;
        Column = column;
    }

    public static ReportedErrorWarning Error(string msg, int line, int col) => new(msg, true, line, col); 

    public override string ToString()
    {
        var pretext = IsError ? "ERROR:" : "WARNING:";
        return $"({Line},{Column}): {pretext}: {Message}";
    }

}