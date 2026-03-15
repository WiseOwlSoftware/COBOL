// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.IR;

/// <summary>
/// One IR module per COBOL program/class. Contains types (records),
/// methods (paragraphs/sections), and globals (working-storage).
/// </summary>
public sealed class IrModule
{
    public string Name { get; }
    public List<IrType> Types { get; } = new();
    public List<IrMethod> Methods { get; } = new();
    public List<IrGlobal> Globals { get; } = new();

    public IrModule(string name) => Name = name;
}

/// <summary>
/// A global variable (WORKING-STORAGE, FILE SECTION record, etc.)
/// </summary>
public sealed class IrGlobal
{
    public string Name { get; }
    public IrType Type { get; }

    public IrGlobal(string name, IrType type)
    {
        Name = name;
        Type = type;
    }
}
