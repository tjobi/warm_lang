namespace WarmLangCompiler.Binding;


public class BoundConstant
{
    public object Value { get; }

    public T GetCastValue<T>() 
    {
        if(Value is T v) 
        {
            return v;
        }
        throw new Exception($"BoundConstant someone had a wrong assumption of value '{Value}'");
    } 

    public BoundConstant(object val) 
    {
        Value = val;
    }
}
