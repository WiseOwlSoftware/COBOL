// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

/// <summary>
/// The intentional P6→P9 seam (rearch PHASE-06 Step 2, the doc's "OO entanglement seam"): the OO DATA/BODY bind
/// orchestration (<c>OoBindInterfaceData</c>, <c>OoBindClassData</c>, <c>OoBindClassBody</c>) physically remains
/// on <c>CSharpEmitter</c> partials until PHASE 09 moves the OO subsystem into <c>Oo/</c> — but it only MUTATES
/// BINDER STATE (verified by the rearchitecture survey; it emits nothing), so <see cref="BinderDriver.Bind"/>
/// reaches it through this interface and the Binding layer never references a CodeGen type. Realized as an
/// interface rather than the design doc's <c>OoBindCallbacks</c> delegate record — the same seam, one named
/// contract instead of a bundle of anonymous delegates (deviation recorded in PHASE-06 §STATUS). The doc's other
/// two seam members were RELOCATED instead of bridged (P6 Step 5): the override-crossing harmonize lives in
/// <c>StorageFormPass</c> (a StoreAsImage settle-step) and the class file-connector qualification in
/// <c>BinderDriver.QualifyClassFiles</c> — both pure binder-model mutations with no emitter-state dependency.
/// </summary>
internal interface IOoBindHost
{
    /// <summary>Hands the host the group-bind session (turn state, class table, edition, the SHARED uid-band
    /// allocator) BEFORE any OO bind call — the host's OO methods draw uid bands from the same sequence as the
    /// driver's per-unit binds, exactly as the fused pipeline did.</summary>
    void BeginBind(BindSession session);

    /// <summary>Bind one INTERFACE's prototype formals (§10.6.2 SR4 — LINKAGE-only data divisions).</summary>
    void BindInterfaceData(OoInterfaceSymbol iface);

    /// <summary>Phase A of class binding — the DATA + SIGNATURES (OBJECT + FACTORY halves; deep-dive D1 pass-1).</summary>
    void BindClassData(OoClassUnit cls);

    /// <summary>Phase B — the method BODIES bind into the class's one pc space (§11.7).</summary>
    void BindClassBody(OoClassUnit cls);

    /// <summary>The per-interface DATA forests the host built in <see cref="BindInterfaceData"/> — carried onto
    /// <see cref="BoundCompilation.InterfaceData"/> for interface emission.</summary>
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> InterfaceData { get; }
}

/// <summary>The shared state of ONE group bind (rearch PHASE-06 Step 2): built by <see cref="BinderDriver.Bind"/>
/// and handed to the <see cref="IOoBindHost"/> so the emitter-hosted OO binds and the driver's per-unit binds draw
/// from the SAME uid-band sequence and fold the SAME compile-time TurnState — exactly as the fused
/// <c>CallBindRunUnit</c> did with instance fields.</summary>
internal sealed class BindSession
{
    public required TurnState Turn { get; init; }
    public required OoClassTable OoClasses { get; init; }
    public required EditionContext Edition { get; init; }

    private int _uidBand;

    /// <summary>Take the next disjoint 100k uid band (one per DataBinder, so nested-class struct/profile names
    /// never shadow a container's — the band discipline of the fused pipeline's <c>_callUidBand</c>).</summary>
    public int TakeUidBand()
    {
        int band = _uidBand;
        _uidBand += 100_000;
        return band;
    }
}
