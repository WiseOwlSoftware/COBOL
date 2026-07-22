// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Binding;

/// <summary>
/// The compile-time <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> resolution (ISO/IEC 1989:2023 §7.3.23): the source-ordered
/// ON/OFF directive toggles, folded per reference-modification site to decide whether a zero-length result is
/// ALLOWED there (§8.4.3.3.4 item 5c). The default is OFF (§7.3.23.3 GR1 — a zero-length ref-mod raises
/// EC-BOUND-REF-MOD). The fold is over events with <c>Line &lt; siteLine</c> — strict, because a directive occupies
/// its own line and applies to the text that FOLLOWS it (the <c>&gt;&gt;TURN</c> GR5 discipline); the LAST matching
/// event wins ("until" the next toggle / end of compilation group).
/// </summary>
public sealed class RefModZeroLengthState
{
    private readonly IReadOnlyList<RefModZeroLengthEvent> _events;

    /// <summary>The empty state — no directive; every site is OFF (the §7.3.23.3 GR1 default).</summary>
    public static readonly RefModZeroLengthState Empty = new([]);

    private RefModZeroLengthState(IReadOnlyList<RefModZeroLengthEvent> events) => _events = events;

    /// <summary>Build the state from the frontend's directive events (already introduction-gated + syntax-checked by
    /// <see cref="RefModZeroLengthDirectiveProcessor"/>). Null/empty ⇒ the OFF default everywhere.</summary>
    public static RefModZeroLengthState Build(IReadOnlyList<RefModZeroLengthEvent>? events)
        => events is null || events.Count == 0 ? Empty : new RefModZeroLengthState(events);

    /// <summary>Is <c>REF-MOD-ZERO-LENGTH</c> ON at a reference modification on <paramref name="siteLine"/>? The
    /// most recent toggle strictly BEFORE the site wins; OFF when no toggle precedes it.</summary>
    public bool IsOnAt(int siteLine)
    {
        bool on = false;   // §7.3.23.3 GR1 — the default is OFF
        foreach (var e in _events)
        {
            if (e.Line >= siteLine) break;   // events are in line order; a directive applies to succeeding text only
            on = e.On;
        }
        return on;
    }

    /// <summary>Whether the <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> directive is NOT explicitly specified (neither ON nor
    /// OFF) at <paramref name="siteLine"/> — no toggle precedes the site. This is the tri-state distinction
    /// <see cref="IsOnAt"/> cannot make (it folds absence to OFF): the FLAG-14 REF-MOD-ZERO-LENGTH option
    /// (ISO §7.3.15.4 GR4 i) flags a reference modification ONLY when the directive is in this unspecified state
    /// (and EC-BOUND-REF-MOD checking is on).</summary>
    public bool IsUnspecifiedAt(int siteLine)
    {
        foreach (var e in _events)
        {
            if (e.Line >= siteLine) break;   // no toggle reaches the site
            return false;                     // a toggle (ON or OFF) precedes it — explicitly specified
        }
        return true;
    }
}
