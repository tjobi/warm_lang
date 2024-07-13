using WarmLangLexerParser;
using static WarmLangLexerParser.TokenKind;
internal static class EmitterTokenKindExtensions
{
    internal static string ILInstruction(this TokenKind kind) => kind switch 
    {
        TPlus => "add",
        TStar => "mul",
        TMinus => "sub",
        TSlash => "div",
        _ => throw new Exception($"No IL-instruction for {kind.AsString()}")
    };
}