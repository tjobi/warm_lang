namespace WarmLangCompiler.Interpreter;

public interface IEnv<K,T>
{
    public T Lookup(K name);

    public (T, IEnv<K,T>) Declare(K name, T value);

    public IEnv<K,T> Push();

    public IEnv<K,T> Pop();
}

public interface IAssignableEnv<K,T> : IEnv<K,T>
{
    public (T, IAssignableEnv<K,T>) Assign(K name, T value);
}