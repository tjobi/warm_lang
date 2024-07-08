using WarmLangCompiler.Binding.Lower;

namespace WarmLangCompiler.Binding.ControlFlow;

public sealed class ControlFlowGraph
{
    
    private readonly BasicBlock _start, _end;
    private readonly Dictionary<BoundLabel, BasicBlock> _labelToBlock;
    private readonly List<Edge> _edges;
    private readonly List<BasicBlock> _blocks;

    public ControlFlowGraph()
    {
        _start = new BasicBlock(new(), isStart: true);
        _end = new BasicBlock(new(), isEnd: true);
        _labelToBlock = new();
        _edges = new();
        _blocks = new();
    }

    public static ControlFlowGraph FromBody(BoundBlockStatement body)
    {
        var graph = new ControlFlowGraph();
        graph.BuildGraph(body);
        return graph;
    }

    public static bool AllPathsReturn(BoundBlockStatement body)
    {
        var graph = FromBody(body);
        foreach(var inEdge in graph._end.Inbound)
        {
            var fromBlock = inEdge.From;
            var lastStatement = fromBlock.Statements.LastOrDefault();
            if(lastStatement is null or not BoundReturnStatement)
                return false;
        }
        return true;
    }

    private void BuildGraph(BoundBlockStatement body)
    {
        var blockBuilder = new BasicBlockBuilder();
        _blocks.AddRange(blockBuilder.BuildBlocks(body));

        foreach (var block in _blocks)
        {
            foreach (var stmnt in block.Statements)
            {
                if (stmnt is BoundLabelStatement boundLabelStatement)
                    _labelToBlock[boundLabelStatement.Label] = block;
            }
        }

        Connect(_start, _blocks.Count > 0 ? _blocks[0] : _end);
        for (int i = 0; i < _blocks.Count; i++)
        {
            var block = _blocks[i];
            var isLastBlock = block == _blocks[^1];
            var next = isLastBlock ? _end : _blocks[i + 1];
            //Run through the blocks, connect the blocks to their respective branches.
            foreach(var stmnt in block.Statements)
            {
                var isLastStatement = stmnt == block.Statements[^1];
                switch(stmnt)
                {
                    case BoundGotoStatement gotoo:
                        var toBlock = _labelToBlock[gotoo.Label];
                        Connect(block, toBlock);
                        break;
                    case BoundConditionalGotoStatement iff:
                        var toThenBlock = _labelToBlock[iff.LabelTrue];
                        var toElseBlock = _labelToBlock[iff.LabelFalse];
                        Connect(block, toThenBlock, iff.Condition);
                        Connect(block, toElseBlock);
                        break;
                    case BoundReturnStatement ret:
                        Connect(block, _end);
                        break;
                    default:
                        if(isLastStatement)
                        {
                            //Console.WriteLine($"{nameof(ControlFlowGraph)}-'{stmnt}' was at the end of block!");
                            Connect(block, next);
                        }
                        break;
                        
                }
            }
        }

        //so imagine we have an if-else like "if 0 {return 2; }else{return 10;}"
        // That creates a label at the end of the if-else.. which is just deeead.
        // The issue is, that it causes a basicblock from just efter "true-branch" code to the if-else-end label then to the end.
        // We want to remove any blocks like that, any blocks that have no incoming ;(. Otherwise it messes up biig time
        bool existsBlocksNoInbound = true;
        while(existsBlocksNoInbound)
        {
            bool removedAny = false;
            foreach(var block in _blocks)
            {
                if(block.Inbound.Count == 0)
                {
                    RemoveBlockNoInbound(block);
                    removedAny = true;
                    break;
                }
            }
            existsBlocksNoInbound = removedAny;
        }
    }

    private void Connect(BasicBlock from, BasicBlock to, BoundExpression? condition = null)
    {
        var edge = new Edge(from, to, condition);
        _edges.Add(edge);
        from.Outgoing.Add(edge);
        to.Inbound.Add(edge);
    }

    private void RemoveBlockNoInbound(BasicBlock block)
    {
        foreach(var outEdge in block.Outgoing)
        {
            var to = outEdge.To;
            to.Inbound.Remove(outEdge);
            _edges.Remove(outEdge);
        }
        _blocks.Remove(block);
    }

    public void ExportToGraphviz(string outfile = "graph", string graphName = "G")
    {
        var outPath = Path.Combine(Directory.GetCurrentDirectory(), $"{outfile}.dot");
        using var writer = new StreamWriter(outPath);
        var blockToId = new Dictionary<BasicBlock, string>()
        {
            {_start, "start"}, {_end, "end"}
        };

        writer.WriteLine($"digraph {graphName}{{");
        writer.WriteLine($"  label=\"Diagram for '{graphName}'\"");
        for (int i = 0; i < _blocks.Count; i++)
        {
            var block = _blocks[i];
            var blockId = $"BasicBlock{i}";
            blockToId[block] = blockId;
            writer.WriteLine($"  {blockId} [label = \"{block}\", shape = box]");
        }
        foreach(var edge in _edges)
        {
            var fromId = blockToId[edge.From];
            var toId = blockToId[edge.To];
            var label = edge.Condition is null ? string.Empty : $"[label = \"{edge.Condition}\"]";
            writer.Write($"  {fromId} -> {toId}");
            writer.WriteLine(label);
        }
        writer.WriteLine('}');
    }
}
