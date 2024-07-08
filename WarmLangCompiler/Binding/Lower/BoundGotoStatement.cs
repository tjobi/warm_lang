using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding.Lower;

public sealed class BoundGotoStatement : BoundStatement
{
    public BoundGotoStatement(StatementNode node, BoundLabel label) : base(node)
    {
        Label = label;
    }

    public BoundLabel Label { get; }

    public override string ToString() => $"goto <{Label}>";
}
