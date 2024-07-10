namespace WarmLangCompiler.Interpreter.Values;

public sealed record class StrValue : Value
{
    public string Value { get; }

    public StrValue(string val)
    {
        Value = val;
    }

    public override string ToString() => $"Str \"{Value}\"";

    public static implicit operator string(StrValue str) => str.Value;

    public static StrValue operator +(StrValue a, StrValue b) => new(a.Value+b.Value);
}