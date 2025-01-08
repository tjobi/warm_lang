namespace WarmLangCompiler.Interpreter.Values;

public abstract record class Value
{
    public static readonly Value Void = new VoidValue();
    public static readonly Value Null = NullValue.NULL;
    private protected Value() { }
    public abstract override string ToString();

    public abstract string StdWriteString();

    private record class VoidValue : Value
    {
        public override string StdWriteString() => ToString();

        public override string ToString() => "void";
    }
}
public record class NullValue : Value
{
    public static readonly NullValue NULL = new();
    public override string StdWriteString() => ToString();

    public override string ToString() => "null";

    private NullValue () {}
}