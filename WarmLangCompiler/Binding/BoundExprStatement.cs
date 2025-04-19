using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public class BoundExprStatement : BoundStatement
{
    public BoundExprStatement(StatementNode syntax, BoundExpression expression)
        :base(syntax)
    {
        Expression = expression;
    }

    public BoundExpression Expression { get; }

    public override string ToString() => $"({Expression};)";
}