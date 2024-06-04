namespace WarmLangCompiler.Interpreter.Values;

public sealed class VoidValue : Value
{
    private static readonly VoidValue _voidValue = new();
    static public VoidValue Instance => _voidValue; 

    private VoidValue(){ }

    public override string ToString() => "void";
}