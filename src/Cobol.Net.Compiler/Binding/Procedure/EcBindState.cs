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

    /// <summary>True while binding imperative-statement-2/3/4 of an exception-checking PERFORM (a WHEN / WHEN
    /// OTHER / WHEN COMMON body). Relaxes RESUME's SR1 "declarative only" gate (RESUME NEXT STATEMENT is legal
    /// there) and drives XS-RESUME-OPERAND (RESUME AT procedure-name is rejected in a WHEN phrase). NOT set for
    /// imperative-statement-1 (the guarded body) or imperative-statement-5 (FINALLY).</summary>
    public bool InF3When { get; set; }

    /// <summary>This unit contains at least one exception-checking (Format-3) PERFORM → the emitter must install
    /// the ambient F3-frame stack and route raise sites through <c>__EcPerform</c> even when the unit declares no
    /// F3 USE declaratives (the <c>EcDispatchExpr</c> UnitHasF3 gate is insufficient — §5.4/§5.2-6).</summary>
    public bool F3Perform { get; set; }

    /// <summary>Allocate the next per-unit Format-3 PERFORM id (0-based) — disambiguates nested F3 PERFORMs for the
    /// emitted try/catch(<c>ExitPerformSignal</c>)-<c>when(Id==n)</c> boundary and the imp-1/FINALLY labels
    /// (<c>__f3fin{n}</c>/<c>__f3end{n}</c>). Reset per unit.</summary>
    private int _f3PerformCounter;
    public int NextF3PerformId() => _f3PerformCounter++;

    // EcFeatures accumulators (the emitter's gating summary — BoundProgram.Ec), in ctor order.
    public bool Checked { get; set; }
    public bool IoChecked { get; set; }
    public bool Raise { get; set; }
    public bool Resume { get; set; }
    public bool F3 { get; set; }
    public bool Functions { get; set; }
    public bool Raising { get; set; }

    public EcFeatures BuildFeatures() => new(Checked, IoChecked, Raise, Resume, F3, Functions, Raising, F3Perform);
}
