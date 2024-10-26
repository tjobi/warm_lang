using WarmLangLexerParser;

namespace WarmLangCompiler.Interpreter.Exceptions;

public class WarmLangException : Exception
{
    public WarmLangException(string msg): base(msg) { }
}