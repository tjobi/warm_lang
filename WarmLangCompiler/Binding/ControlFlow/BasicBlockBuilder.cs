using WarmLangCompiler.Binding.Lower;

namespace WarmLangCompiler.Binding.ControlFlow;

internal sealed class BasicBlockBuilder
{
    private readonly List<BasicBlock> _blocks;
    private List<BoundStatement> _currentBlock;
    public BasicBlockBuilder()
    {
        _blocks = new();
        _currentBlock = new();
    }

    public List<BasicBlock> BuildBlocks(BoundBlockStatement body)
    {
        foreach(var stmnt in body.Statements)
        {
            switch(stmnt)
            {
                case BoundGotoStatement:
                case BoundConditionalGotoStatement:
                case BoundReturnStatement:
                {
                    _currentBlock.Add(stmnt);
                    StartBasicBlock();
                } break;
                case BoundLabelStatement:
                {
                    StartBasicBlock();
                    _currentBlock.Add(stmnt);
                } break;
                default:
                {
                    _currentBlock.Add(stmnt);
                } break;
            }
        }
        EndBasicBlock();
        return _blocks;
    }

    private void EndBasicBlock() => StartBasicBlock();

    private void StartBasicBlock()
    {
        if(_currentBlock.Count > 0)
        {
            _blocks.Add(new BasicBlock(_currentBlock));
            _currentBlock = new();
        }
    }
}