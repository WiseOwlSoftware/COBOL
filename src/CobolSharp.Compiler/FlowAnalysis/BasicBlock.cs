namespace CobolSharp.Compiler.FlowAnalysis;

/// <summary>
/// A basic block in the control-flow graph. Contains a sequence of statements
/// with single entry/single exit — control flow only changes at block boundaries.
/// </summary>
public sealed class BasicBlock
{
    public int Id { get; }
    public List<object> Statements { get; } = new(); // BoundStatement when binder is wired
    public List<BasicBlock> Successors { get; } = new();
    public List<BasicBlock> Predecessors { get; } = new();
    public bool IsExit { get; }

    public BasicBlock(int id, bool isExit = false)
    {
        Id = id;
        IsExit = isExit;
    }
}

/// <summary>
/// Control-flow graph for a paragraph, section, or program.
/// Entry and Exit are synthetic blocks; real code lives in between.
/// </summary>
public sealed class ControlFlowGraph
{
    public BasicBlock Entry { get; }
    public BasicBlock Exit { get; }
    public IReadOnlyList<BasicBlock> Blocks { get; }

    public ControlFlowGraph(BasicBlock entry, BasicBlock exit, List<BasicBlock> blocks)
    {
        Entry = entry;
        Exit = exit;
        Blocks = blocks;
    }
}
