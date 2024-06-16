namespace WarmLangCompiler.Binding;

public sealed class BoundProgram
{
    public BoundProgram(BoundBlockStatement statement)
    {
        Statement = statement;
    }
    public BoundBlockStatement Statement { get; }
}
