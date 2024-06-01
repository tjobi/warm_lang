namespace WarmLangCompiler.Interpreter.Values;

public sealed class StrValue : Value
{
    public string Value { get; }

    public StrValue(string val)
    {
        Value = val;
    }

    public override string ToString() => $"Str \"{Value}\"";
}