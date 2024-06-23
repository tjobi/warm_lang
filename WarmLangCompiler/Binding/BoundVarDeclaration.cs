using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundVarDeclaration : BoundStatement
{
    public BoundVarDeclaration(StatementNode node, string name, BoundExpression rightHandSide)
    : base(node)
    {
        Name = name;
        RightHandSide = rightHandSide;
    }

    public string Name { get; }
    public BoundExpression RightHandSide { get; }
}