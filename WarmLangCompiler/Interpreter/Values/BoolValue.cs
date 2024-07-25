namespace WarmLangCompiler.Interpreter.Values;

public sealed record class BoolValue : Value
{
    public static readonly BoolValue True = new(true);

    public static readonly BoolValue False = new(false);

    public bool Value { get; }

    private BoolValue(bool value) 
    {
        Value = value;
    }

    public BoolValue Negate()
    {
        if(Value)
            return True;
        return False;
    }

    public static BoolValue FromBool(bool val) => val ? True : False;

    public override string ToString() => $"Bool {StdWriteString()}";

    public override string StdWriteString() => Value ? "True" : "False";

    public static implicit operator bool(BoolValue boo) => boo.Value;
}