// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.CodeGen;

using Core = CobolParserCore;

/// <summary>
/// The BIND-HOST facade of the Roslyn backend (P7 Step 9n — the recorded deviation from the original
/// "CSharpEmitter is gone" slogan): the emitter god class is dissolved — emission lives on
/// <see cref="ProgramEmitter"/> (run-unit orchestration) → <see cref="UnitEmitters"/> (the per-unit
/// composition root) → <see cref="DispatchEmitter"/>/<see cref="StatementEmitter"/>/<c>Verbs/*Emitter</c> —
/// and THIS class keeps only the bind-side surface until P9 relocates the OO bind bodies into the binder:
/// the <see cref="Bind"/>/<see cref="EmitBound"/> driver entries, the <see cref="IOoBindHost"/> seam
/// (P6→P9), the OO BIND half (the <c>Oo</c> partial), and the bind-session state the OO methods read
/// (<c>_bindSession</c>/<c>_turnState</c>/<c>_ooClasses</c>/<c>_ooIfaceData</c>). It emits NOTHING.
/// </summary>
public sealed partial class CSharpEmitter : IOoBindHost
{
    /// <summary>BIND the WHOLE compilation group in <paramref name="tree"/> to an immutable
    /// <see cref="BoundCompilation"/> (multi-unit run-unit binding — interprogram design D3 / SSOT §18 #8), under
    /// the targeted EDITION (<paramref name="edition"/> — bind-time rejection diagnostics accumulate there; the
    /// driver fails the compile when any exist, BEFORE emit). A thin shim over
    /// <see cref="BinderDriver.Bind"/> (rearch PHASE-06 Step 2 — the Binder phase owns the orchestration) with
    /// THIS instance as the <see cref="IOoBindHost"/>: the OO bind bodies physically remain on this class's
    /// partials until P9. <see cref="EmitBound"/> renders C# from the result — codegen never runs on an errored
    /// tree.</summary>
    internal BoundCompilation Bind(Core.CompilationUnitContext tree, EditionContext? edition = null,
        IReadOnlyList<CobolNet.Frontend.Preprocessor.TurnEvent>? turnEvents = null)
        => new BinderDriver().Bind(tree, edition ?? new EditionContext(2023), turnEvents, this);

    /// <summary>Render typed-native C# from an already-bound immutable <see cref="BoundCompilation"/> (the emit
    /// half of the bind/emit split) — a fresh <see cref="ProgramEmitter"/> per call; the compilation carries
    /// everything emission needs (incl. the OO class table + interface forests), so the emit side reads NO
    /// bind-host state.</summary>
    internal string EmitBound(BoundCompilation comp) => new ProgramEmitter().Emit(comp);

    // ── The IOoBindHost seam (P6→P9): BinderDriver reaches the emitter-hosted OO bind bodies through these;
    //    they only mutate binder state (never emit). BeginBind restores the shared-session fields the OO
    //    methods read (_turnState for ConfigureEc, _ooClasses for symbol resolution, the uid-band source). ──

    private BindSession? _bindSession;
    private TurnState _turnState = TurnState.Empty;

    void IOoBindHost.BeginBind(BindSession session)
    {
        _bindSession = session;
        _turnState = session.Turn;
        _ooClasses = session.OoClasses;
    }

    void IOoBindHost.BindInterfaceData(OoInterfaceSymbol iface) => OoBindInterfaceData(iface, _bindSession!.Edition);
    void IOoBindHost.BindClassData(OoClassUnit cls) => OoBindClassData(cls, _bindSession!.Edition);
    void IOoBindHost.BindClassBody(OoClassUnit cls) => OoBindClassBody(cls);
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> IOoBindHost.InterfaceData => _ooIfaceData;
}
