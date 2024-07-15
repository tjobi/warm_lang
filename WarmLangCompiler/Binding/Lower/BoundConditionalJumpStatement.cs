using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding.Lower;

public sealed class BoundConditionalGotoStatement : BoundStatement
{
    public BoundConditionalGotoStatement(StatementNode node, BoundExpression condition, BoundLabel labelTrue, BoundLabel labelFalse, bool fallThroughBranch = true) : base(node)
    {
        Condition = condition;
        LabelTrue = labelTrue;
        LabelFalse = labelFalse;
        FallThroughBranch = fallThroughBranch;
    }

    public BoundExpression Condition { get; }
    public BoundLabel LabelTrue { get; }
    public BoundLabel LabelFalse { get; }
    public bool FallThroughBranch { get; }

    public bool FallsThroughTrueBranch => FallThroughBranch;

    public override string ToString() => $"if {Condition} goto <{LabelTrue}>; else goto <{LabelFalse}>";
}