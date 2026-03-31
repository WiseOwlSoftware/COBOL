// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Module/type/method/global setup: SeedPrimitiveTypes, DefineType, DefineGlobal,
/// GetTypeRef, DefineMethodSignature, CreateEntryMethodSignature, EmitAlternateEntryMethod.
/// </summary>
internal sealed class CilModuleSetup
{
    private readonly EmissionContext _ctx;

    internal CilModuleSetup(EmissionContext ctx) => _ctx = ctx;

    // TODO: Stage 5 — move SeedPrimitiveTypes from CilEmitter
    // TODO: Stage 5 — move DefineType from CilEmitter
    // TODO: Stage 5 — move DefineGlobal from CilEmitter
    // TODO: Stage 5 — move GetTypeRef from CilEmitter
    // TODO: Stage 5 — move DefineMethodSignature from CilEmitter
    // TODO: Stage 5 — move CreateEntryMethodSignature from CilEmitter
    // TODO: Stage 5 — move EmitAlternateEntryMethod from CilEmitter
}
