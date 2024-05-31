namespace WarmLangLexerParser.AST.Typs;

/// <summary>
/// Fake typ created by parser when it reads an invalid type?
/// </summary>
public sealed class TypInvalid : Typ
{
    public override string ToString() => "TypInvalid";
}