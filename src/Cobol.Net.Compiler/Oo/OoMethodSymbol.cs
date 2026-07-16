// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

/// <summary>One method of a class roster (ISO §11.7): the COBOL name, the emitted C# method name, the header
/// USING/RETURNING presence recorded at pass-1, and — filled by <c>DataBinder.OoBindMethodData</c> at class
/// bind time (BEFORE any statement binds, so every INVOKE site in the group sees the full signature) — the
/// resolved formal list, RETURNING item, and data roots (port slice 2, deep-dive D3/D6).</summary>
public sealed record OoMethodSymbol(
    string Name, bool HasUsing, bool HasReturning,
    CobolParserCore.MethodDefinitionContext Ctx)
{
    /// <summary>The emitted C# method name. Starts as the sanitized-uppercase COBOL name; an OVERRIDE adopts
    /// its base slot's name verbatim (C# requires the override member name to match), and a collision with
    /// the owning class's type name (C# CS0542) or an emitted field takes a deterministic suffix — always a
    /// SYMBOL-level rename so every INVOKE site in the group follows automatically (§8.3.2.2: the
    /// user-word→externalized-name mapping is implementor-defined).</summary>
    public required string CsName { get; set; }

    /// <summary>The class that declares this method (set at pass-1) — the marshaling qualifier for the
    /// formal's class-level statics (numeric profiles) at CONTENT-conversion call sites.</summary>
    public OoClassSymbol Owner { get; set; } = null!;

    /// <summary>True for a FACTORY method (§11.4) — the SELF/SUPER roster selector and diagnostic wording;
    /// its formals' profiles/statics live on the FACTORY class, so CONTENT-conversion call sites qualify by
    /// <see cref="OoClassSymbol.FactoryCsName"/>.</summary>
    public bool IsFactory { get; init; }

    /// <summary>METHOD-ID … OVERRIDE (§11.7 SR3/SR4a — the OVERRIDE/FINAL wave, DEVLOG 605): an explicit
    /// override declaration. Redefinition WITHOUT it is the SR4a 0837 (error strict; warning + the pre-wave
    /// name-match inference under <c>--permissive</c> — the documented migration leniency).</summary>
    public bool HasOverride { get; init; }

    /// <summary>METHOD-ID … [IS] FINAL (§11.7 GR3 — shall not be overridden; 0839 on the attempt). Emits
    /// C# <c>sealed override</c> (or a non-virtual fresh slot at a root).</summary>
    public bool IsFinal { get; init; }

    /// <summary>The property-accessor identity (§11.7/§13.18.42 — the PROPERTY wave): 'G'/'S' for a GET/SET
    /// accessor of <see cref="PropertyName"/> (explicit <c>METHOD-ID. GET|SET PROPERTY p</c> or synthesized
    /// from a PROPERTY clause), '\0' for an ordinary named method. Accessor rosters key by the PINNED
    /// implementor-defined names <c>__GET_&lt;P&gt;</c>/<c>__SET_&lt;P&gt;</c> (§11.7.4 GR1a — the `__`
    /// cannot-collide rule), so override/0829/implements machinery applies to accessors unchanged.</summary>
    public char Accessor { get; init; }
    public string? PropertyName { get; init; }

    /// <summary>The method PD-header RAISING partition (§14.2.2; the EC-OO wave D-EO8): level-3 EC-USER
    /// names + classes of the group — loaded into the statement binder's per-source-element sets before
    /// this method's body binds (a method IS a source element, §14.9.18.3 SR2/SR4a).</summary>
    public List<string> RaisingEcNames { get; } = [];
    public List<string> RaisingClasses { get; } = [];

    /// <summary>For a PROPERTY-clause-synthesized accessor: the SUBJECT data item (the emitter renders a
    /// direct field read/write — observably identical to the spec's implicit MOVE method, §13.18.42 GR1/GR2
    /// :21214-21229, because the cloned descriptions are identical by construction). Null for explicit
    /// GET/SET PROPERTY methods (they carry real bodies).</summary>
    public DataItem? PropertySubject { get; set; }

    /// <summary>The method's contiguous pc range in its class's one dispatch space — assigned by
    /// <c>StatementBinder.BindClassBody</c> (the exit-bounded range IS the fall-through guard: running past
    /// the last paragraph returns from the method, never into a sibling's paragraphs — the legacy trap #4).</summary>
    public int EntryPc { get; set; } = -1;
    public int EndPc { get; set; } = -2;   // Entry > End ⇔ an empty method body

    /// <summary>The ordered PD USING formals (§14.9.23.4 GR3 — positional correspondence; every formal is
    /// BY REFERENCE, the header BY VALUE phrase being an unparsed grammar extension).</summary>
    public List<OoFormal> Formals { get; } = [];

    /// <summary>The PD RETURNING item (a LINKAGE 01/77 — §14.2.3 GR6: callee-allocated; the method's C# return
    /// value delivers it, §14.9.23.4 GR8), or null for a void method.</summary>
    public DataItem? Returning { get; set; }

    /// <summary>The method's LINKAGE roots (ALL of them — formals, the RETURNING item, and any unattached
    /// entry): each becomes a capturable C# LOCAL of the emitted method.</summary>
    public List<DataItem> LinkageRoots { get; } = [];

    /// <summary>LOCAL-STORAGE roots → C# locals, re-initialized on every activation (§14.5.3).</summary>
    public List<DataItem> LocalRoots { get; } = [];

    /// <summary>Method WORKING-STORAGE roots → STATIC fields (D3 — shared across instances, persistent across
    /// activations, §11.7; ILLEGAL at 2023, §13.5.3 SR 1 — the version-conformance pass window row).</summary>
    public List<DataItem> StaticRoots { get; } = [];

    /// <summary>The method's own name scope (§11.7 GR5 shadowing; sibling invisibility — trap #6).</summary>
    public OoMethodDataScope DataScope { get; } = new();

    /// <summary>The base-chain method this method OVERRIDES (slice 3a — §9.3.6 dispatch is on the runtime
    /// class; D7: emitted as C# <c>override</c>), or null for a fresh <c>virtual</c> slot. Marked at pass-1
    /// by name (the OVERRIDE attribute is not in the grammar yet — the documented SR4a leniency); the
    /// SIGNATURE conformance (§9.3.8.2) validates after all class data binds
    /// (<see cref="OoConformance.ValidateOverrideSignatures"/>).</summary>
    public OoMethodSymbol? OverrideOf { get; set; }
}
