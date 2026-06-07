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

    /// <summary>Typed-native standalone fields the data-model migration flips out of the byte areas (S3a).
    /// Empty unless <c>EnableTypedFields</c> is on.</summary>
    public List<IrTypedFieldDef> TypedFieldDefs { get; } = [];

    /// <summary>Flipped <c>01</c> groups → .NET <c>record struct</c>s (S3b). Empty unless flipping is on.</summary>
    public List<IrTypedRecordDef> TypedRecordDefs { get; } = [];

    /// <summary>Flipped fixed <c>OCCURS</c> tables → typed .NET array fields (S4). Empty unless flipping is on.</summary>
    public List<IrTypedArrayDef> TypedArrayDefs { get; } = [];

    /// <summary>
    /// Default target paragraph indices for each ALTER slot.
    /// Empty when no ALTER statements are used (zero overhead).
    /// Index = alter slot, Value = default paragraph index (-1 for bare GO TO).
    /// </summary>
    public List<int> AlterDefaults { get; } = [];

    /// <summary>
    /// Ordered list of paragraph methods for the Entry dispatch loop. Position == paragraph
    /// index (the pc value returned by fall-through/GO TO/PERFORM), INCLUDING declarative
    /// paragraphs, so the dispatch switch and every pc value share one index space.
    /// Set by CreateEntryPoint; used by CilEmitter.EmitEntryMethod.
    /// </summary>
    public List<IrMethod> ParagraphDispatchOrder { get; } = [];

    /// <summary>
    /// Index (into ParagraphDispatchOrder) of the first non-declarative paragraph — the program's
    /// entry point per ISO §14.4 (execution begins at the first paragraph after END DECLARATIVES).
    /// The dispatch loop starts pc here so leading DECLARATIVES paragraphs are skipped; they remain
    /// in the switch at their own indices because the USE handler reaches them via PERFORM.
    /// </summary>
    public int EntryParagraphIndex { get; set; }

    /// <summary>True if this program is declared IS INITIAL (re-initialize WORKING-STORAGE per CALL).</summary>
    public bool IsInitial { get; set; }

    /// <summary>
    /// Parameterless method that registers this program's file connectors (RegisterFileHandlerWithOrg
    /// + the per-file Set* calls), produced by CreateEntryPoint. It is NOT FileRuntime.Init (that stays
    /// in Main, the run-unit entry, so a sub-program never disposes the caller's open files). CilEmitter
    /// calls it from Entry once per activation, guarded by a per-program _filesRegistered flag, so a
    /// CALLed subprogram's internal files are registered on its own activation (ISO §14.6) and its open
    /// file/position survives subsequent CALLs. Null only if CreateEntryPoint has not run yet.
    /// </summary>
    public IrMethod? RegisterFilesMethod { get; set; }

    /// <summary>
    /// PROCEDURE DIVISION USING parameter names (LINKAGE SECTION item names).
    /// Positional: UsingParameterNames[i] maps to Entry args[i].
    /// Empty if no USING clause.
    /// </summary>
    public List<string> UsingParameterNames { get; } = [];

    /// <summary>
    /// ENTRY points declared in the program. Each entry has a name and optional
    /// USING parameter names. Registered in CobolProgramRegistry under their own names.
    /// </summary>
    public List<(string Name, IReadOnlyList<string> UsingParams)> EntryPoints { get; } = [];

    /// <summary>
    /// GLOBAL USE AFTER ERROR declaratives this program exposes to its CONTAINED programs
    /// (ISO §14.9.49.4 GR4 / §8.4.6.2.2). Each entry names the dispatch scope (-1 file-name; 0/1/2/3
    /// INPUT/OUTPUT/I-O/EXTEND), the optional file name (for scope -1), and the inclusive paragraph-index
    /// range of the declarative section in <see cref="ParagraphDispatchOrder"/>. CilEmitter emits a public
    /// static handler per entry that runs that range via the shared Dispatch helper, and registers it in
    /// <c>GlobalUseDeclarativeRegistry</c> during InitializeState so a contained program can invoke it.
    /// </summary>
    public List<(int Scope, string? FileName, int StartIndex, int EndIndex)> GlobalUseHandlers { get; } = [];
}

/// <summary>
/// A module-level storage area (WORKING-STORAGE, FILE SECTION, etc.)
/// backed by a byte array in the emitted ProgramState.
/// </summary>
/// <param name="Name">Storage area identifier (e.g., "WorkingStorage").</param>
/// <param name="Type">Always <see cref="IrPrimitiveType.ByteArray"/> in current usage.</param>
public sealed record IrGlobal(string Name, IrType Type);

/// <summary>A typed-native field flipped out of the COBOL byte areas (data-model migration S3/S4): the emitted
/// field <paramref name="Name"/>. For a character field (<paramref name="IsNumeric"/> false): a .NET
/// <see cref="string"/> of <paramref name="Width"/> positions initialized to <paramref name="InitValue"/>
/// (already padded/truncated). For an unsigned-integer numeric field (<paramref name="IsNumeric"/> true, S4): a
/// .NET <see cref="long"/> of <paramref name="Width"/> COBOL digits initialized to <paramref name="NumericInit"/>.
/// For a signed/scaled numeric field (<paramref name="IsNumeric"/> true, <paramref name="IsDecimal"/> true, S4): a
/// .NET <see cref="decimal"/> whose <paramref name="Width"/> is the byte storage width, initialized to
/// <paramref name="DecimalInit"/>.</summary>
public sealed record IrTypedFieldDef(string Name, int Width, string InitValue,
    bool IsNumeric = false, long NumericInit = 0,
    bool IsDecimal = false, decimal DecimalInit = 0m);

/// <summary>A flipped <c>01</c> group → a .NET <c>record struct</c> (data-model migration S3b): the emitted
/// struct <paramref name="StructTypeName"/>, its static-instance field <paramref name="InstanceName"/>, and the
/// typed string <paramref name="Members"/> (one per elementary child).</summary>
public sealed record IrTypedRecordDef(string StructTypeName, string InstanceName, IReadOnlyList<IrTypedFieldDef> Members);

/// <summary>A flipped fixed <c>OCCURS</c> table → a typed .NET array field (data-model migration S4): the emitted
/// array field <paramref name="Name"/> of <paramref name="ElementCount"/> elements, each shaped by
/// <paramref name="Element"/> (CLR type via <c>TypedFieldClrType</c> + per-slot initial value). Element access is
/// <c>ldsfld array; index; ldelem|stelem</c>; <c>InitializeState</c> allocates <c>new T[ElementCount]</c> and fills
/// every slot from <paramref name="Element"/>'s init (never <c>default(T)</c>, ADR §1.7).</summary>
public sealed record IrTypedArrayDef(string Name, int ElementCount, IrTypedFieldDef Element);
