using System.Collections.Immutable;
using System.Text;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public class BoundBlockStatement : BoundStatement
{
    public BoundBlockStatement(StatementNode syntax, ImmutableArray<BoundStatement> statements) 
    : base(syntax)
    {
        Statements = statements;
    }

    public ImmutableArray<BoundStatement> Statements { get; }

    public override string ToString()
    {
        var sb = new StringBuilder().Append("Bound Block: {");
        foreach(var stmnt in Statements)
        {
            sb.Append(stmnt);
            if(stmnt != Statements[^1])
                sb.Append(", ");
        }
        return sb.Append('}').ToString();
    }
}
