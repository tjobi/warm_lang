using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundIfStatement : BoundStatement
{
    public BoundIfStatement(StatementNode node, BoundExpression condition, BoundStatement then, BoundStatement? @else)
    : base(node)
    {
        Condition = condition;
        Then = then;
        Else = @else;
    }

    public BoundExpression Condition { get; }
    public BoundStatement Then { get; }
    public BoundStatement? Else { get; }

    public override string ToString() => $$$"""if {{{Condition}}} { {{{Then}}} } else { {{{Else}}} }""";
}