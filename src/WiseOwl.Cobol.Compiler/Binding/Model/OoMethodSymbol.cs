// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>One method of a class roster (ISO §11.7): the COBOL name, the emitted C# method name, the header
/// USING/RETURNING presence recorded at pass-1, and — filled by <c>OoDataBinder.BindMethodData</c> at class
/// bind time (BEFORE any statement binds, so every INVOKE site in the group sees the full signature) — the
/// resolved formal list, RETURNING item, and data roots (port slice 2, deep-dive D3/D6).</summary>
public sealed record OoMethodSymbol
{
    /// <summary>A method of origin <paramref name="origin"/>: <see cref="OoSymbolOrigin.Source"/> (a METHOD-ID of this group, whose
    /// definition the bind session's source map holds, kb/Work PB2401), <see cref="OoSymbolOrigin.Standard"/> (the standard class BASE's,
    /// ISO §16.2), <see cref="OoSymbolOrigin.Imported"/> (read from an assembly of the external repository, DESIGN-external-repository
    /// §10.1) or <see cref="OoSymbolOrigin.Generated"/> (a PROPERTY clause's accessor, ISO §13.18.42).</summary>
    public OoMethodSymbol(string name, bool hasUsing, bool hasReturning, OoSymbolOrigin origin)
    {
        (Name, HasUsing, HasReturning, Origin) = (name, hasUsing, hasReturning, origin);
    }

    /// <summary>The COBOL method name (an accessor's is its pinned GET/SET family name, ISO §11.7.4 GR1 a).</summary>
    public string Name { get; }

    /// <summary>Whether the PROCEDURE DIVISION header has a USING phrase (an accessor's formal for a SET).</summary>
    public bool HasUsing { get; }

    /// <summary>Whether the PROCEDURE DIVISION header has a RETURNING phrase (an accessor's result for a GET).</summary>
    public bool HasReturning { get; }

    /// <summary>Where this method came from (<see cref="OoSymbolOrigin"/>) — the ONE question every consumer asks, never "has it a
    /// definition" (kb/Work PB4459). It pairs <see cref="OoClassSymbol.Origin"/>: a method is <see cref="OoSymbolOrigin.Source"/> exactly
    /// when it has a METHOD-ID definition, which the bind session's <c>SourceMap.Of</c> answers (kb/Work PB2401); a standard, a generated
    /// and an imported method have no body to bind and no position of their own, so asking for one is a loud internal error.</summary>
    public OoSymbolOrigin Origin { get; }

    /// <summary>The name this method is externalized under (ISO §11.7.4 GR1 b): "the name is
    /// method-name-1. However, literal-1, if specified, is the name of method that is externalized to the
    /// operating environment"). It defaults to <see cref="Name"/>, and it is the ROSTER KEY: §14.9.23.2
    /// gives INVOKE no word form for the method at all — only literal-1 or identifier-2 — and §14.9.23.4
    /// GR2 a) resolves that literal "as described in 8.3.2.2, User-defined words", i.e. against the
    /// externalized name. Keying the roster here also realizes §11.7.3 SR9's "if method-name-1 OR
    /// literal-1 is the same as a method-name inherited or implemented" without a second lookup path
    /// (kb/Work PB303).</summary>
    public string ExternalizedName { get => _externalizedName ?? Name; init => _externalizedName = value; }
    private readonly string? _externalizedName;

    /// <summary>The key a UNIVERSAL dispatch selects this method by (kb/Work PB1405): §8.3.2.2 1) treats a universal
    /// INVOKE's method-name "as a COBOL word that maps to the externalized name of the method to be invoked", so the
    /// key is <see cref="ExternalizedName"/> — the same roster key the typed path resolves by — under the ONE
    /// case-insensitive word mapping the invocation side applies (<see cref="WiseOwl.Cobol.Runtime.CobolObject.NormalizeMethodName"/>,
    /// the Annex C fold, <see cref="WiseOwl.Cobol.Runtime.CobolNames"/>). Never <see cref="Name"/>: an AS phrase replaces it.</summary>
    public string DispatchKey => WiseOwl.Cobol.Runtime.CobolObject.NormalizeMethodName(ExternalizedName);

    /// <summary>The emitted CLR method name — the module's ABI, the same for every backend and the one the
    /// repository records (<c>MethodRecord.ClrName</c>; kb/Work PB4265). Starts as the sanitized-uppercase COBOL name; an OVERRIDE adopts
    /// its base slot's name verbatim (C# requires the override member name to match), and a collision with
    /// the owning class's type name (C# CS0542) or an emitted field takes a deterministic suffix — always a
    /// SYMBOL-level rename so every INVOKE site in the group follows automatically (§8.3.2.2: the
    /// user-word→externalized-name mapping is implementor-defined).</summary>
    public required string ClrName { get; set; }

    /// <summary>The class that declares this method (set at pass-1) — the marshaling qualifier for the
    /// formal's class-level statics (numeric profiles) at CONTENT-conversion call sites.</summary>
    public OoClassSymbol Owner { get; set; } = null!;

    /// <summary>The method's checking slice (<see cref="MethodChecking"/>): §14.9.23.4 GR7 c)'s EC-OO-UNIVERSAL half
    /// at the method's PROCEDURE DIVISION HEADER, §14.8.4.1's activated-method EC-EXTERNAL mask and automatic propagation
    /// (§7.3.21.4 GR1/GR3), written by <c>CheckingFold</c> once every method symbol exists and before any
    /// method body binds, and read by <c>OoEmitter</c>.
    /// <para>⛔ It is folded in the binder and not in the emitter because CODEGEN OWNS NO TurnState — every
    /// <c>&gt;&gt;TURN</c> query in the compiler lives in the binder and codegen consumes only the results. Reading
    /// TurnState from an emitter would be a second mechanism for a job the binder already does.</para></summary>
    public MethodChecking Checking { get; set; } = MethodChecking.None;

    /// <summary>True for a FACTORY method (§11.4) — the SELF/SUPER roster selector and diagnostic wording;
    /// its formals' profiles/statics live on the FACTORY class, so CONTENT-conversion call sites qualify by
    /// <see cref="OoClassSymbol.FactoryClrName"/>.</summary>
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

    /// <summary>The method as the SOURCE names it, for a diagnostic: <c>GET PROPERTY p</c> / <c>SET PROPERTY p</c>
    /// for an accessor (whose <see cref="Name"/> is the implementor-defined roster name of §11.7.4 GR1 a), never
    /// written by the user), else the method-name.</summary>
    public string SourceName => Accessor switch
    {
        'G' => $"GET PROPERTY {PropertyName}",
        'S' => $"SET PROPERTY {PropertyName}",
        _ => Name,
    };

    /// <summary>Which method of the standard class BASE this is (ISO §16.2 — its factory interface's New, its
    /// object interface's FactoryObject), or <see cref="StandardMethod.None"/> for every method written in the
    /// compilation group. Set exactly when <see cref="Origin"/> is <see cref="OoSymbolOrigin.Standard"/>: such a method has no METHOD-ID in
    /// source and no COBOL body: the runtime class <c>WiseOwl.Cobol.Runtime.BASE</c> is its
    /// implementation, so an invocation site binds it through its own form rather than the user-method tail.</summary>
    public StandardMethod Standard { get; init; }

    /// <summary>True when <see cref="ClrName"/> is a fact no compilation of this group may change — a standard method's (the
    /// runtime's member) or an imported one's (the built assembly's, DESIGN-external-repository §10.1, §10.6) — so
    /// <c>OoClassTable.NameMethodSlots</c> names a slot after it and never renames it.</summary>
    public bool HasFixedClrName => Origin is OoSymbolOrigin.Standard or OoSymbolOrigin.Imported;

    /// <summary>The method's own OPTIONS paragraph folded over the class channel's model (§10.6.1 method
    /// skeleton / §11.9.4 GR1; kb/Work PB135) — null when the method writes none. BindMethodRoster swaps it
    /// in around THIS method's body bind.</summary>
    public OptionsModel? MethodOptions { get; set; }

    /// <summary>The method's PD-header RAISING phrase (§14.2.1), partitioned by <c>RaisingPhrase.Partition</c>
    /// into the §14.2.1 tuples — loaded into the statement binder's per-source-element state before this
    /// method's body binds (a method IS a source element, §14.9.18.3 SR2/SR4).</summary>
    public List<RaisingTarget> Raising { get; } = [];

    /// <summary>For a PROPERTY-clause-synthesized accessor: the SUBJECT data item (the emitter renders a
    /// direct field read/write — observably identical to the spec's implicit MOVE method, §13.18.42 GR1/GR2
    /// :21214-21229, because the cloned descriptions are identical by construction). Null for explicit
    /// GET/SET PROPERTY methods (they carry real bodies).</summary>
    public DataItem? PropertySubject { get; set; }







    /// <summary>The AFTER-DATA-BIND half of the description (P9 R7 — phase-explicit): attached by
    /// <c>OoDataBinder.BindMethodData</c> once this method's data has bound; the pc range is stamped into it
    /// later still by <c>StatementBinder.BindClassBody</c>. Reading it before attach is a null-deref — the
    /// intended type-level ordering guard (never a silent -1 sentinel).</summary>
    public OoMethodBinding? Binding { get; internal set; }

    /// <summary>The method's own name scope (§11.7 GR5 shadowing; sibling invisibility — trap #6).</summary>
    public OoMethodDataScope DataScope { get; } = new();

    /// <summary>The base-chain method this method OVERRIDES (slice 3a — §9.3.6 dispatch is on the runtime
    /// class; D7: emitted as C# <c>override</c>), or null for a fresh <c>virtual</c> slot. Marked by roster key
    /// by <see cref="OoClassTable.ResolveOverrides"/> once every roster — PROPERTY-clause accessors included — is
    /// complete (kb/Work PB1274); the SIGNATURE conformance (§9.3.8.2) validates right after
    /// (<see cref="OoConformance.ValidateOverrideSignatures"/>).</summary>
    public OoMethodSymbol? OverrideOf { get; set; }
}
