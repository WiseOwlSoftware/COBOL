// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using CobolNet.Frontend.Preprocessor;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// The compile-time <c>&gt;&gt;TURN</c> resolution (ISO/IEC 1989:2023 §7.3.25; deep-dive D10): the source-ordered
/// directive events, folded per query to decide WHETHER an EC guard is emitted at a statement at all — checking
/// OFF (the §7.3.25.4 GR1 default) compiles to NOTHING, the zero-scaffolding invariant (SSOT §18.16). A query
/// names a LEVEL-3 exception-name; an event matches it per the hierarchy expansion rules: EC-ALL covers every
/// name except EC-I-O-WARNING (GR2), a level-2 name covers its level-3 children except EC-I-O-WARNING (GR3),
/// EC-I-O-WARNING toggles only explicitly (GR4); a file-scoped event applies only to that file (GR6/GR8). The
/// fold is over events with <c>Line &lt; statementLine</c> — strict, because a directive occupies its own line
/// and a mid-statement directive applies to SUCCEEDING statements only (GR5); the LAST matching event wins
/// (GR6/GR8 — enabled/disabled "until" the next toggle).
/// </summary>
public sealed class TurnState
{
    /// <summary>One flattened (line, name, file) toggle in source order.</summary>
    private readonly record struct Ev(int Line, string Ec, string? File, bool On, bool WithLocation);

    private readonly List<Ev> _events = [];

    /// <summary>The empty state — no TURN directives; every query is disabled (GR1 default).</summary>
    public static readonly TurnState Empty = new();

    private TurnState() { }

    /// <summary>Build the state from the frontend's directive events, validating each exception-name against
    /// the catalog (§7.3.25.3 SR2 — a name shall be a §14.6.13.1 name or a user-/implementor-defined one) and
    /// the per-name edition window (the 2023-only families — VERSION_CHANGE_REFERENCE rows 40/61 — are
    /// diagnosed at --std 2002/2014; the directive itself is 2002-gated upstream).</summary>
    public static TurnState Build(IReadOnlyList<TurnEvent>? events, EditionContext edition)
    {
        if (events is null || events.Count == 0) return Empty;
        var s = new TurnState();
        foreach (var ev in events)
            foreach (var (ec, file) in ev.Names)
            {
                if (!ExceptionCatalog.TryGet(ec, out var info))
                {
                    edition.Error("COBOLNET0711", $">>TURN: '{ec}' is not an exception-name of ISO/IEC 1989 "
                        + "§14.6.13.1 (and not a valid EC-USER-/EC-IMP- name)");
                    continue;
                }
                if (info.IntroducedIn > edition.DialectLevel)
                {
                    edition.Error("COBOLNET0878", $"exception-name {info.Name} was introduced by ISO/IEC "
                        + $"1989:{info.IntroducedIn} — it requires --std {info.IntroducedIn} or later "
                        + $"(targeting COBOL-{edition.DialectLevel})");
                    continue;
                }
                s._events.Add(new Ev(ev.Line, info.Name, file, ev.On, ev.WithLocation));
            }
        return s;
    }

    /// <summary>Return a NEW state = the base events with a synthetic ENABLING event at <paramref name="imp1Line"/>
    /// spliced in for each named exception that is NOT already enabled there — the GR14 implicit TURN of an
    /// exception-checking PERFORM over imperative-statement-1 (§14.9.28.4 GR14): "if checking … is NOT enabled for
    /// imperative-statement-1 by a TURN directive, an implicit TURN directive … is assumed BEFORE the first
    /// statement in imperative-statement-1" (WITH LOCATION iff the PERFORM specified LOCATION). Correctness of the
    /// fold demands the event list stay LINE-SORTED (Fold keeps the last match with <c>Line &lt; statementLine</c>),
    /// so the synthetic is placed at imp-1's line — AFTER every pre-PERFORM directive (a pre-PERFORM
    /// <c>&gt;&gt;TURN OFF</c> therefore LOSES to it, per GR14's "not enabled ⇒ assume on") and BEFORE any real
    /// <c>&gt;&gt;TURN OFF</c> inside imp-1 (which overrides it). The "not already enabled" guard means a real
    /// enable BEFORE the PERFORM keeps its own settings (GR22 — no implicit is assumed). imp-2..5 bind against the
    /// BASE state — this overlay is discarded (never assigned back) after imp-1 (GR21/GR22).
    /// (Known edge, unobservable while the runtime is staged: a directive on the PERFORM's OWN line, and WITH
    /// LOCATION precedence when a real enable precedes — refinements for the interceptor wave.)</summary>
    public TurnState WithImplicitEnable(IEnumerable<(string Ec, string? File)> names, bool withLocation, int imp1Line)
    {
        var s = new TurnState();
        foreach (var e in _events) if (e.Line < imp1Line) s._events.Add(e);   // pre-imp-1 directives (line-sorted)
        foreach (var (ec, file) in names)
            if (!Enabled(ec, file, imp1Line))   // GR14 implicit ON only when not already enabled (else GR22 persists)
                s._events.Add(new Ev(imp1Line, ec, file, On: true, WithLocation: withLocation));
        foreach (var e in _events) if (e.Line >= imp1Line) s._events.Add(e);   // imp-1+ directives (line-sorted)
        return s;
    }

    /// <summary>The 1-based lines of every <c>&gt;&gt;TURN</c> directive that NAMES one of <paramref name="ecNames"/>
    /// (matched on the canonical exception-name, ON or OFF alike) — the FLAG-02 b EC-PROGRAM-EXCEPTIONS detector
    /// flags such a directive when its source element calls a function or invokes a method (§7.3.14.4 GR4 b). A
    /// directive naming several of the set contributes its line once per name; the caller de-duplicates.</summary>
    public IEnumerable<int> DirectiveLinesNaming(IReadOnlySet<string> ecNames)
        => _events.Where(e => ecNames.Contains(e.Ec)).Select(e => e.Line);

    /// <summary>True when ANY enabling event exists — the group-level EC-machinery gate (a compilation group
    /// with no enabling TURN, no F3, no RAISE/RESUME emits byte-identical code to a pre-EC build).</summary>
    public bool AnyEnabled => _events.Any(e => e.On);

    /// <summary>True when any enabling event ANYWHERE in the group covers level-3 <paramref name="level3"/> —
    /// the group-level gate for machinery that must exist wherever the condition COULD pair (the EC-EXTERNAL
    /// descriptor registrations: an element whose own mask is zero must still register its descriptions when a
    /// LATER element in the group can check against them, §14.8.4 — TURN events are line-scoped across the
    /// whole group text).</summary>
    public bool AnyEnabledFor(string level3) => _events.Any(e => e.On && NameMatches(e.Ec, level3));

    /// <summary>Is checking for level-3 name <paramref name="level3"/> (for <paramref name="file"/>, when the
    /// name is EC-I-O-scoped) enabled at the statement starting on <paramref name="statementLine"/>?</summary>
    public bool Enabled(string level3, string? file, int statementLine) =>
        Fold(level3, file, statementLine) is { On: true };

    /// <summary>Did the event that ENABLED <paramref name="level3"/> at this statement carry WITH LOCATION
    /// (§7.3.25.4 GR7 — the EXCEPTION-LOCATION/-STATEMENT information capture)?</summary>
    public bool WithLocation(string level3, string? file, int statementLine) =>
        Fold(level3, file, statementLine) is { On: true, WithLocation: true };

    private Ev? Fold(string level3, string? file, int statementLine)
    {
        Ev? last = null;
        foreach (var e in _events)
        {
            if (e.Line >= statementLine) break;   // events are in line order; GR5 — succeeding statements only
            if (!NameMatches(e.Ec, level3)) continue;
            if (e.File is not null && (file is null || !e.File.Equals(file, StringComparison.OrdinalIgnoreCase)))
                continue;   // a file-scoped event applies only to that file (GR6/GR8)
            last = e;
        }
        return last;
    }

    /// <summary>Does directive name <paramref name="evName"/> cover level-3 <paramref name="level3"/>?
    /// EC-ALL → everything but EC-I-O-WARNING (GR2); a level-2 name → its level-3 children but EC-I-O-WARNING
    /// (GR3); a level-3 name → itself (the only way WARNING toggles — GR4).</summary>
    private static bool NameMatches(string evName, string level3)
    {
        if (evName.Equals(level3, StringComparison.OrdinalIgnoreCase)) return true;
        if (level3.Equals("EC-I-O-WARNING", StringComparison.OrdinalIgnoreCase)) return false;   // explicit only (GR4)
        if (evName.Equals(ExceptionCatalog.EcAll, StringComparison.OrdinalIgnoreCase)) return true;   // GR2
        return ExceptionCatalog.TryGet(evName, out var info) && info.Level == 2
            && ExceptionCatalog.UnderLevel2(level3, evName);   // GR3
    }
}
