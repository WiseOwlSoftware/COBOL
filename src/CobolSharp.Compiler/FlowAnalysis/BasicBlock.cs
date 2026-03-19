// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.FlowAnalysis;

/// <summary>
/// A basic block in the control-flow graph. Contains a sequence of statements
/// with single entry/single exit -- control flow only changes at block boundaries.
/// </summary>
public sealed class BasicBlock(int id, bool isExit = false)
{
    public int Id { get; } = id;
    public List<object> Statements { get; } = []; // BoundStatement when binder is wired
    public List<BasicBlock> Successors { get; } = [];
    public List<BasicBlock> Predecessors { get; } = [];
    public bool IsExit { get; } = isExit;
}

/// <summary>
/// Control-flow graph for a paragraph, section, or program.
/// Entry and Exit are synthetic blocks; real code lives in between.
/// </summary>
public sealed record ControlFlowGraph(
    BasicBlock Entry,
    BasicBlock Exit,
    IReadOnlyList<BasicBlock> Blocks);
