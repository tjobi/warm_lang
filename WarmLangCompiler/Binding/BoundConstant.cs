namespace WarmLangCompiler.Binding;

public class BoundConstant
{
    public int Value { get; }

    public BoundConstant(int value)
    {
        Value = value;
    }
}
