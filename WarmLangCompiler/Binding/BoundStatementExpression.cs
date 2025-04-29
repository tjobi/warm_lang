using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public class BoundStatementExpression : BoundStatement
{
    public BoundStatementExpression(StatementNode node) : base(node) { }

    public override string ToString()
    {
        throw new NotImplementedException();
    }
}