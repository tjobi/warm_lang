namespace WarmLangLexerParser.AST;

public sealed class InvalidAccess : Access
{
    public InvalidAccess(TextLocation location) : base(location) { }

    public override string ToString() => "Invalid acces";
}