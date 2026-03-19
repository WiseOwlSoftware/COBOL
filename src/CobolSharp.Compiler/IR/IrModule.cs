// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.IR;

/// <summary>
/// Top-level IR container for a single COBOL program. Corresponds 1:1 to
/// a .NET assembly. Aggregates the record types (01-level groups),
/// methods (paragraphs/sections), and globals (WORKING-STORAGE / FILE SECTION records)
/// produced by the lowering pass.
/// </summary>
public sealed class IrModule(string name)
{
    /// <summary>PROGRAM-ID or class name, used as the assembly and type name.</summary>
    public string Name { get; } = name;

    /// <summary>Record types emitted as explicit-layout structs.</summary>
    public List<IrType> Types { get; } = [];

    /// <summary>Methods emitted as static methods on the program class.</summary>
    public List<IrMethod> Methods { get; } = [];

    /// <summary>Global byte-array fields representing COBOL storage areas.</summary>
    public List<IrGlobal> Globals { get; } = [];
}

/// <summary>
/// A module-level storage area (WORKING-STORAGE, FILE SECTION, etc.)
/// backed by a byte array in the emitted ProgramState.
/// </summary>
/// <param name="Name">Storage area identifier (e.g., "WorkingStorage").</param>
/// <param name="Type">Always <see cref="IrPrimitiveType.ByteArray"/> in current usage.</param>
public sealed record IrGlobal(string Name, IrType Type);
