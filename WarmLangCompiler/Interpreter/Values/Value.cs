namespace WarmLangCompiler.Interpreter.Values;

public abstract record class Value
{
    public static readonly Value Void = new VoidValue();
    public static readonly Value Null = new NullValue();
    private protected Value() { }
    public abstract override string ToString();

    public abstract string StdWriteString();

    private record class VoidValue : Value
    {
        public override string StdWriteString() => ToString();

        public override string ToString() => "void";
    }

    private record class NullValue : Value
    {
        public override string StdWriteString() => ToString();

        public override string ToString() => "null";
    }
}
