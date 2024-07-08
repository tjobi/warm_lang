using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding.Lower;

public sealed class BoundConditionalGotoStatement : BoundStatement
{
    public BoundConditionalGotoStatement(StatementNode node, BoundExpression condition, BoundLabel labelTrue, BoundLabel labelFalse) : base(node)
    {
        Condition = condition;
        LabelTrue = labelTrue;
        LabelFalse = labelFalse;
    }

    public BoundExpression Condition { get; }
    public BoundLabel LabelTrue { get; }
    public BoundLabel LabelFalse { get; }

    public override string ToString() => $"if {Condition} goto <{LabelTrue}>; else goto <{LabelFalse}>";
}