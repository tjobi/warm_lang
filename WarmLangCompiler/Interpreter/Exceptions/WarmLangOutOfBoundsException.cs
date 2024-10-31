namespace WarmLangCompiler.Interpreter.Exceptions;

public sealed class WarmLangOutOfBoundsException : WarmLangException
{
    public WarmLangOutOfBoundsException(int idx, int lowerBound, int upperBound)
    : this($"Index {idx} out of bounds {lowerBound};{upperBound}") { }

    public WarmLangOutOfBoundsException(string msg)
    : base(msg) { }
}