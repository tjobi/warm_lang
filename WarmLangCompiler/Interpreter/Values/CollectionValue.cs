namespace WarmLangCompiler.Interpreter.Values;

public abstract record CollectionValue : Value 
{
    public virtual int Length { get; }

    public abstract Value GetAt(int i);
}

public abstract record MutableCollectionValue : CollectionValue 
{
    public abstract Value SetAt(int i, Value v);
}