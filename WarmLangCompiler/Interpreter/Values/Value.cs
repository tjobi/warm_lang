using System.Text;
using WarmLangCompiler.Binding;
using WarmLangCompiler.Symbols;

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

    private NullValue() { }
}

public sealed record FunctionValue : Value
{
    public FunctionValue(FunctionSymbol symbol, BoundBlockStatement body)
    {
        Symbol = symbol;
        Body = body;
    }

    public FunctionSymbol Symbol { get; }
    public BoundBlockStatement Body { get; }

    public override string StdWriteString()
    {
        var sb = new StringBuilder();
        sb.Append('(');
        sb.AppendJoin(", ", Symbol.Parameters);
        sb.Append(')');
        sb.Append(" => ").Append(Symbol.ReturnType);
        return sb.ToString();
    }
} 