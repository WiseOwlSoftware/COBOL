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

    // ── EXTERNAL storage ──

    /// <summary>Static fields for EXTERNAL data items, keyed by data name (case-insensitive).</summary>
    public Dictionary<string, FieldDefinition> ExternalFields { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>EXTERNAL record offset ranges in WorkingStorage: (wsOffset, wsLength) to (extField).</summary>
    public List<(int WsOffset, int WsLength, FieldDefinition ExtField)> ExternalRanges { get; } = [];

    // ── CALL support ──

    /// <summary>
    /// Field to track the last CALL result (0=success, non-zero=exception).
    /// Allocated lazily when the first IrCallProgram is emitted.
    /// </summary>
    public FieldDefinition? LastCallResultField { get; set; }

    /// <summary>Cached reference to CobolDataPointer(byte[], int, int, PicDescriptor) constructor.</summary>
    public MethodReference? CobolDataPointerCtor { get; set; }

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
