namespace WarmLangCompiler.Interpreter.Values;


public sealed record class IntValue : Value
{
    public int Value { get; }

    public IntValue(int val)
    {
        Value = val;
    }

    public override string ToString() => $"Int {Value}";

    public override string StdWriteString() => Value.ToString();

    public static implicit operator int(IntValue i) => i.Value;

    public static IntValue operator +(IntValue a, IntValue b) => new((int)a+b);
    public static IntValue operator *(IntValue a, IntValue b) => new(a.Value*b.Value);
    public static IntValue operator -(IntValue a, IntValue b) => new(a.Value-b.Value);
    public static IntValue operator /(IntValue a, IntValue b) => new(a.Value/b.Value);
}