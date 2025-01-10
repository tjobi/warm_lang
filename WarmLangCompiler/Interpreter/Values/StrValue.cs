namespace WarmLangCompiler.Interpreter.Values;

public sealed record class StrValue : CollectionValue
{
    public static readonly StrValue DEFAULT = new(string.Empty);
    public string Value { get; }
    public override int Length => Value.Length;

    public StrValue(string val)
    {
        Value = val;
    }
    public override Value GetAt(int i) => new IntValue(Value[i]);
    public override string ToString() => $"Str \"{Value}\"";

    public override string StdWriteString() => Value;


    public static implicit operator string(StrValue str) => str.Value;

    public static StrValue operator +(StrValue a, StrValue b) => new(a.Value+b.Value);
}