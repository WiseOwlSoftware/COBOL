// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Program state initialization: EmitProgramState, EmitProgramStateAllocation,
/// EmitValueClauseInitialization, ComputeOccursExtent, EmitAlterTableInitialization,
/// EmitResetStateMethod, EmitLocalStorageDefaultsSnapshot,
/// EmitExternalStorageInitialization, TryGetExternalField.
/// </summary>
internal sealed class CilProgramStateEmitter
{
    private readonly EmissionContext _ctx;

    internal CilProgramStateEmitter(EmissionContext ctx) => _ctx = ctx;

    // TODO: Stage 5 — move EmitProgramState from CilEmitter
    // TODO: Stage 5 — move EmitProgramStateAllocation from CilEmitter
    // TODO: Stage 5 — move EmitValueClauseInitialization from CilEmitter
    // TODO: Stage 5 — move ComputeOccursExtent from CilEmitter
    // TODO: Stage 5 — move EmitAlterTableInitialization from CilEmitter
    // TODO: Stage 5 — move EmitResetStateMethod from CilEmitter
    // TODO: Stage 5 — move EmitLocalStorageDefaultsSnapshot from CilEmitter
    // TODO: Stage 5 — move EmitExternalStorageInitialization from CilEmitter
    // TODO: Stage 5 — move TryGetExternalField from CilEmitter
}
