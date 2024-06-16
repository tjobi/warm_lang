using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public class BoundExprStatement : BoundStatement
{
    public BoundExprStatement(StatementNode syntax, BoundExpression bound)
        :base(syntax)
    {
        Bound = bound;
    }

    public BoundExpression Bound { get; }
}