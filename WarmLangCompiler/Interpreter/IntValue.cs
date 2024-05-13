namespace WarmLangCompiler.Interpreter;


public sealed class IntValue : Value
{
    public int Value { get; }

    public IntValue(int val)
    {
        Value = val;
    }

    public override string ToString() => $"Int {Value}";
}