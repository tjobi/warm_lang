using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding.Lower;

public sealed class BoundLabelStatement : BoundStatement
{
    public BoundLabelStatement(StatementNode node, BoundLabel label) : base(node)
    {
        Label = label;
    }

    public BoundLabel Label { get; }

    public override string ToString() => $"{Label}:";
}