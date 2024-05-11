namespace WarmLangCompiler.Interpreter;

public interface IEnv<T>
{
    public T Lookup(string name);

    public (T, IEnv<T>) Declare(string name, T value);

    public IEnv<T> Push();

    public IEnv<T> Pop();
}

public interface IAssignableEnv<T> : IEnv<T>
{
    public (T, IAssignableEnv<T>) Assign(string name, T value);
}