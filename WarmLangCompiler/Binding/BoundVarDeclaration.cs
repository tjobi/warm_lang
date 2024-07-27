using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundVarDeclaration : BoundStatement
{
    public BoundVarDeclaration(StatementNode node, VariableSymbol name, BoundExpression rightHandSide)
    : base(node)
    {
        Symbol = name;
        RightHandSide = rightHandSide;
    }

    public VariableSymbol Symbol { get; }
    public BoundExpression RightHandSide { get; }

    public TypeSymbol Type => Symbol.Type;
}