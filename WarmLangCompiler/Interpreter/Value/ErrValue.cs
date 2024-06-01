namespace WarmLangCompiler.Interpreter.Values;

public sealed class ErrValue : Value
{
    public string Message { get; }

    private readonly string asString;
    public ErrValue(string msg)
    {
        Message = msg;
        asString = $"Err ({msg})";
    }
    public override string ToString() => asString;
}