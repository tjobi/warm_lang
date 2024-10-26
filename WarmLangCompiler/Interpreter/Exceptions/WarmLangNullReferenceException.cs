using WarmLangLexerParser;

namespace WarmLangCompiler.Interpreter.Exceptions;
public sealed class WarmLangNullReferenceException : WarmLangException
{
    public WarmLangNullReferenceException(TextLocation location, string msg)
    : base($"null reference at {location}: {msg}") { }
}