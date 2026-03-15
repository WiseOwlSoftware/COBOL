// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.IR;

/// <summary>
/// IR method — one per COBOL paragraph/section. Contains basic blocks
/// with linear instruction sequences and explicit control flow.
/// </summary>
public sealed class IrMethod
{
    public string Name { get; }
    public IrType? ReturnType { get; }
    public List<IrParameter> Parameters { get; } = new();
    public List<IrLocal> Locals { get; } = new();
    public List<IrBasicBlock> Blocks { get; } = new();

    public IrBasicBlock Entry => Blocks[0];

    public IrMethod(string name, IrType? returnType)
    {
        Name = name;
        ReturnType = returnType;
    }
}

public sealed class IrParameter
{
    public string Name { get; }
    public IrType Type { get; }

    public IrParameter(string name, IrType type)
    {
        Name = name;
        Type = type;
    }
}

public sealed class IrLocal
{
    public string Name { get; }
    public IrType Type { get; }

    public IrLocal(string name, IrType type)
    {
        Name = name;
        Type = type;
    }
}

/// <summary>
/// Basic block in IR — linear sequence of instructions, terminated
/// by a branch, jump, or return.
/// </summary>
public sealed class IrBasicBlock
{
    public string Name { get; }
    public List<IrInstruction> Instructions { get; } = new();

    public IrBasicBlock(string name) => Name = name;
}

/// <summary>
/// SSA-ish virtual register. Each instruction result gets a unique ID.
/// </summary>
public readonly struct IrValue
{
    public int Id { get; }
    public IrType Type { get; }

    public IrValue(int id, IrType type)
    {
        Id = id;
        Type = type;
    }

    public override string ToString() => $"%{Id}";
}

/// <summary>
/// Hands out monotonically increasing IrValue IDs per method.
/// </summary>
public sealed class IrValueFactory
{
    private int _nextId;

    public IrValue Next(IrType type) => new IrValue(_nextId++, type);
}
