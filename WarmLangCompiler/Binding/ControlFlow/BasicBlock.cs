using System.Text;

namespace WarmLangCompiler.Binding.ControlFlow;

internal sealed class BasicBlock
{

    public BasicBlock(List<BoundStatement> statements, bool isStart = false, bool isEnd = false)
    {
        Statements = statements;
        IsStart = isStart;
        IsEnd = isEnd;
        Inbound = new();
        Outgoing = new();
    }

    public List<BoundStatement> Statements { get; }
    public List<Edge> Inbound { get; }
    public List<Edge> Outgoing { get; }
    public bool IsStart { get; }
    public bool IsEnd { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Statements.Count; i++)
        {
            sb.Append(Statements[i].ToString());
            if(i < Statements.Count -1 )
            {
                sb.Append('\n');
            }
        }
        return sb.ToString();
    }
}
