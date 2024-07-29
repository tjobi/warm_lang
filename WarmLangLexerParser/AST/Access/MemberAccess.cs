namespace WarmLangLexerParser.AST;

public sealed class MemberAccess : Access
{
    public MemberAccess(Access target, SyntaxToken memberToken) : base(TextLocation.FromTo(target.Location, memberToken.Location))
    {
        Target = target;
        MemberToken = memberToken;
    }

    public Access Target { get; }
    public SyntaxToken MemberToken { get; }

    public override string ToString() => $"({Target}.{MemberToken.Name})";
}