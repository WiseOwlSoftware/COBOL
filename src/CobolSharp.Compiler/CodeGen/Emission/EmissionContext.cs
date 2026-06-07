// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Shared mutable state for all CIL emission passes.
/// Constructed by CilEmitter and passed to every emitter.
/// Owns all state that was formerly scattered as private fields across CilEmitter.cs.
/// </summary>
internal sealed class EmissionContext
{
    // ── Core Cecil references ──

    public ModuleDefinition Module { get; }
    public Dictionary<IrType, TypeReference> TypeMap { get; } = new();
    public Dictionary<IrField, FieldDefinition> FieldMap { get; } = new();
    public Dictionary<IrMethod, MethodDefinition> MethodMap { get; } = new();

    // ── Program structure ──

    public TypeDefinition? ProgramType { get; set; }
    public FieldDefinition? ProgramStateField { get; set; }
    public MethodDefinition? InitializeStateMethod { get; set; }
    public FieldDefinition? AlterTableField { get; set; }

    // ── LINKAGE SECTION ──

    /// <summary>Static fields for LINKAGE SECTION parameters, keyed by USING parameter name (case-insensitive).</summary>
    public Dictionary<string, FieldDefinition> LinkageFields { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Typed-native static fields (data-model migration S3a), keyed by the emitted field name carried in
    /// <see cref="IrTypedFieldLocation"/> (when its InstanceName is null).</summary>
    public Dictionary<string, FieldDefinition> TypedFields { get; } = new(StringComparer.Ordinal);

    /// <summary>Flipped <c>01</c> groups → <c>record struct</c>s (S3b/S5), keyed by the static-instance field name
    /// (<see cref="IrTypedFieldLocation.InstanceName"/>): the static instance <see cref="FieldDefinition"/>. Member
    /// (and nested-member) <see cref="FieldDefinition"/>s are resolved at access time by walking the struct's
    /// <c>FieldType.Fields</c> along the location's <see cref="IrTypedFieldLocation.MemberPath"/> + leaf name.</summary>
    public Dictionary<string, FieldDefinition> TypedRecords { get; } = new(StringComparer.Ordinal);

    /// <summary>Flipped fixed <c>OCCURS</c> tables → typed .NET array fields (S4), keyed by the array field name
    /// carried in <see cref="IrTypedElementLocation.ArrayFieldName"/>.</summary>
    public Dictionary<string, FieldDefinition> TypedArrays { get; } = new(StringComparer.Ordinal);

    /// <summary>Pointer fields (Stage-4, docs/RECORD_STRUCT_STORAGE_DESIGN.md §10): the emitted
    /// <c>static ManagedPointer _PTR_&lt;name&gt;</c> <see cref="FieldDefinition"/>, keyed by field name (the value
    /// carried in <see cref="IrPointerStore"/>/<see cref="IrPointerCompare"/>/<see cref="IrBasedDerefLocation"/>).</summary>
    public Dictionary<string, FieldDefinition> PointerFields { get; } = new(StringComparer.Ordinal);

    // ── Per-method tracking ──

    public MethodDefinition? CurrentMethodDef { get; set; }
    public VariableDefinition? ArithmeticStatusLocal { get; set; }

    /// <summary>
    /// Cache for IrCachedLocation: maps cache key to (area, offset, length) locals.
    /// Cleared per method.
    /// </summary>
    public Dictionary<int, (VariableDefinition area, VariableDefinition offset, VariableDefinition length)>
        CachedLocationLocals { get; } = new();

    // ── Semantic context ──

    public SemanticModel? SemanticModel { get; set; }

    // ── Entry method ──

    public MethodDefinition? EntryMethod { get; set; }

    /// <summary>
    /// Shared paragraph-dispatch helper: <c>int Dispatch(int startPc, int exitPc)</c>. Runs the
    /// program's control flow from startPc following each paragraph's returned next-pc; returns when
    /// the paragraph at exitPc completes by falling through (returns exitPc+1), or when pc goes
    /// out of range (STOP RUN/EXIT PROGRAM → −1, or off the end). The main loop calls it with
    /// exitPc = −1 (no exit paragraph); PERFORM…THRU calls it with the THRU range's true endpoints.
    /// Used by CilControlFlowEmitter.EmitPerformThru. Null if the program has no paragraphs.
    /// </summary>
    public MethodDefinition? DispatchMethod { get; set; }

    // ── EXTERNAL storage ──

    /// <summary>Static fields for EXTERNAL data items, keyed by data name (case-insensitive).</summary>
    public Dictionary<string, FieldDefinition> ExternalFields { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>EXTERNAL record offset ranges: (area, offset, length) → shared byte[] field. The Area
    /// discriminator is REQUIRED — WorkingStorage and FileSection have independent offset namespaces, so a
    /// FileSection range at offset N must NOT redirect a WorkingStorage reference at N (and vice-versa).
    /// EXTERNAL WS records live here with Area=WorkingStorage (IC226A); FD ... IS EXTERNAL record areas with
    /// Area=FileSection (IC227A).</summary>
    public List<(StorageAreaKind Area, int Offset, int Length, FieldDefinition ExtField)> ExternalRanges { get; } = [];

    // ── CALL support ──

    /// <summary>
    /// Field to track the last CALL result (0=success, non-zero=exception).
    /// Allocated lazily when the first IrCallProgram is emitted.
    /// </summary>
    public FieldDefinition? LastCallResultField { get; set; }

    /// <summary>Cached reference to ManagedPointer(byte[], int, int, PicDescriptor) constructor.</summary>
    public MethodReference? ManagedPointerCtor { get; set; }

    // ── Emitter references (set after construction) ──

    public CilModuleSetup ModuleSetup { get; set; } = null!;
    public CilProgramStateEmitter ProgramState { get; set; } = null!;
    public CilControlFlowEmitter ControlFlow { get; set; } = null!;
    public CilDataEmitter Data { get; set; } = null!;
    public CilArithmeticEmitter Arithmetic { get; set; } = null!;
    public CilComparisonEmitter Comparison { get; set; } = null!;
    public CilExpressionEmitter Expression { get; set; } = null!;
    public CilLocationEmitter Location { get; set; } = null!;
    public CilStringEmitter String { get; set; } = null!;
    public CilFileIoEmitter FileIo { get; set; } = null!;

    // ── Recursive instruction emission delegate ──
    // Allows extracted emitters to call back into CilEmitter.EmitInstruction
    // without depending on the CilEmitter class directly.

    public Action<ILProcessor, IrInstruction, Func<IrValue, VariableDefinition>,
        Dictionary<IrBasicBlock, Instruction>> EmitInstruction { get; set; } = null!;

    // ── Constructor ──

    public EmissionContext(ModuleDefinition module)
    {
        Module = module;
    }
}
