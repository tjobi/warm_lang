namespace WarmLangCompiler.Interpreter.Values;

public sealed record class VoidValue : Value
{
    private static readonly VoidValue _voidValue = new();
    static public VoidValue Void => _voidValue; 

    private VoidValue(){ }

    public override string ToString() => "void";
}