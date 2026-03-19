// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.IR;

/// <summary>
/// IR method -- one per COBOL paragraph, section, or compiler-generated entry point.
/// Uses a basic-block CFG: each block holds a linear instruction sequence
/// terminated by an explicit branch, jump, or return.
/// </summary>
public sealed class IrMethod(string name, IrType? returnType)
{
    /// <summary>CIL method name (paragraph/section name or synthetic like "Main").</summary>
    public string Name { get; } = name;

    /// <summary>Null for void methods (most COBOL paragraphs).</summary>
    public IrType? ReturnType { get; } = returnType;
    public List<IrParameter> Parameters { get; } = [];
    public List<IrLocal> Locals { get; } = [];
    public List<IrBasicBlock> Blocks { get; } = [];

    /// <summary>The first block in the list, which is the method entry point.</summary>
    public IrBasicBlock Entry => Blocks[0];

    private int _blockCounter;

    /// <summary>Creates a new basic block with a unique suffix to avoid CIL label collisions.</summary>
    public IrBasicBlock CreateBlock(string name) => new($"{name}_{_blockCounter++}");
}

/// <summary>
/// A formal parameter of an <see cref="IrMethod"/> (e.g., ProgramState reference).
/// </summary>
/// <param name="Name">Parameter name used in CIL.</param>
/// <param name="Type">The IR type of the parameter.</param>
public sealed record IrParameter(string Name, IrType Type);

/// <summary>
/// A method-local variable in the IR, emitted as a CIL local.
/// </summary>
/// <param name="Name">Local variable name (used for debugging metadata).</param>
/// <param name="Type">The IR type of the local.</param>
public sealed record IrLocal(string Name, IrType Type);

/// <summary>
/// A basic block in the control-flow graph. Contains a straight-line sequence of
/// IR instructions with no internal branches. The last instruction is always a
/// terminator (branch, conditional branch, or return).
/// </summary>
public sealed class IrBasicBlock(string name)
{
    /// <summary>Unique label used as the CIL branch target.</summary>
    public string Name { get; } = name;
    public List<IrInstruction> Instructions { get; } = [];
}

/// <summary>
/// A virtual register in the IR. Each instruction that produces a result
/// is assigned a unique IrValue. Not full SSA -- values can be reassigned
/// in lowering -- but IDs are unique within a method for readability.
/// </summary>
/// <param name="Id">Monotonically increasing identifier, unique per method.</param>
/// <param name="Type">The IR type of the value this register holds.</param>
public readonly record struct IrValue(int Id, IrType Type)
{
    public override string ToString() => $"%{Id}";
}

/// <summary>
/// Allocator for <see cref="IrValue"/> registers. One instance per method ensures
/// IDs are unique within that method's scope.
/// </summary>
public sealed class IrValueFactory
{
    private int _nextId;

    /// <summary>Allocates the next virtual register with the given type.</summary>
    public IrValue Next(IrType type) => new(_nextId++, type);
}
