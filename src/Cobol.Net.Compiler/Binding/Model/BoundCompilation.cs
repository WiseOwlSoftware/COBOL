// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Model;

using Core = CobolParserCore;

/// <summary>
/// The immutable result of the Binder phase (rearch PHASE-06 Step 2 — replaces the emitter-owned
/// <c>BoundRunUnit</c>): everything the <c>VersionConformancePass</c> gates and the emit half renders, produced by
/// <see cref="BinderDriver.Bind"/>. The emitter consumes it READ-ONLY — no CodeGen write into the Binding data
/// model remains (exit criterion #2; the storage-form decision and the file-connector registry-key qualification
/// both run inside Bind).
/// <para><paramref name="Tree"/> is the parse-tree ROOT — the conformance pass's parse-tree arm walks it for the
/// syntactic gates that have no bound-node representation (introduction/removal on RECOGNITION, DEVLOG 724).
/// Carrying the parse root on this GROUP record does NOT put a parse context on any bound NODE — the
/// <c>BoundTree.cs</c> "no raw parse context" invariant stands.</para>
/// </summary>
/// <param name="Tree">The compilation group's parse root (the conformance pass's parse-arm input).</param>
/// <param name="Units">Program units in emission order (containers precede containees).</param>
/// <param name="ClassUnits">CLASS-ID units (each carrying its OBJECT + FACTORY halves).</param>
/// <param name="OoClasses">The group's pass-1 class symbol table (OO deep-dive D1).</param>
/// <param name="InterfaceData">Per-interface DATA forests (prototype LINKAGE formals) for interface emission.</param>
/// <param name="OoAdapters">The §9.3.8.2.3 5a/5c2 covariant-return adapter pairs (returned by
/// <see cref="OoConformance.ValidateImplements"/>; the interface emitter renders one explicit implementation per pair).</param>
/// <param name="Turn">The group's compile-time TurnState (ISO §7.3.25; EC deep-dive D10).</param>
/// <param name="EcActive">ANY EC-model feature in use (gates every machinery emission; SSOT §18.16).</param>
/// <param name="AnyFiles">Any unit/class declares files or declaratives (drives the IO using + Init/CloseAll).</param>
internal sealed record BoundCompilation(
    Core.CompilationUnitContext Tree,
    IReadOnlyList<BoundUnit> Units,
    IReadOnlyList<OoClassUnit> ClassUnits,
    OoClassTable OoClasses,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> InterfaceData,
    IReadOnlyList<AdapterPair> OoAdapters,
    TurnState Turn,
    bool EcActive,
    bool AnyFiles);
