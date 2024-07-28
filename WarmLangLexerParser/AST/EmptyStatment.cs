namespace WarmLangLexerParser.AST;

public sealed class EmptyStatment : StatementNode
{

    private static readonly EmptyStatment get = new();
    private EmptyStatment() : base(TextLocation.EmptyFile) { }

    public static EmptyStatment Get { get => get; }

    public override string ToString() => "Empty Statement";
}
