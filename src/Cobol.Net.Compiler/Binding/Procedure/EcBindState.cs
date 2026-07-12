// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// The EC exception-condition bind state (P7 Step 10r — the plan's "EcBindState on ctx"): the per-unit
/// mutable state the <c>EcBinder</c> members and the Declaratives half share, hoisted off the god class.
/// <see cref="Turn"/>/<see cref="ProgramName"/> are configured per bound unit (ConfigureEc);
/// <see cref="PdRaising"/>/<see cref="PdRaisingClasses"/> hold the PROCEDURE DIVISION header RAISING lists
/// (§14.2.1/§14.2.2 — per-method reset via EcLoadPdRaising); <see cref="DeclEcPairs"/> the USE F3 SR14
/// cross-USE (ec,file) pairs (per division); the seven bits accumulate the emitter's
/// <see cref="EcFeatures"/> gating summary in ctor order.
/// </summary>
internal sealed class EcBindState
{
    /// <summary>The compilation group's source-ordered &gt;&gt;TURN fold (§7.3.25).</summary>
    public TurnState Turn { get; set; } = TurnState.Empty;

    /// <summary>This unit's PROGRAM-ID name — the §15.30.3 r2 location element.</summary>
    public string ProgramName { get; set; } = "";

    /// <summary>PD-header RAISING exception-names (§14.2.1; the GOBACK/EXIT SR2 check).</summary>
    public HashSet<string> PdRaising { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>PD-header RAISING class names (§14.2.2 SR8; the SR4a check).</summary>
    public HashSet<string> PdRaisingClasses { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>USE F3 SR14 cross-USE (ec,file) pairs — the set spans sections, per division.</summary>
    public HashSet<string> DeclEcPairs { get; } = new(StringComparer.OrdinalIgnoreCase);

    // EcFeatures accumulators (the emitter's gating summary — BoundProgram.Ec), in ctor order.
    public bool Checked { get; set; }
    public bool IoChecked { get; set; }
    public bool Raise { get; set; }
    public bool Resume { get; set; }
    public bool F3 { get; set; }
    public bool Functions { get; set; }
    public bool Raising { get; set; }

    public EcFeatures BuildFeatures() => new(Checked, IoChecked, Raise, Resume, F3, Functions, Raising);
}
