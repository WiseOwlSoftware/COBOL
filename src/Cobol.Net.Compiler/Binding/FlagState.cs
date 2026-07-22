// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Binding;

/// <summary>
/// The compile-time <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> resolution (ISO §7.3.14 / §7.3.15): the
/// source-ordered ON/OFF directive toggles, folded PER OPTION per source line so
/// <see cref="Validation.FlagConformancePass"/> can decide whether a given option is flagging at a construct's
/// line. Every option's default is OFF (§7.3.14.4 GR5 / §7.3.15.4 GR5). The fold is over events with
/// <c>Line &lt; siteLine</c> — strict, because a directive occupies its own line and applies to the text that
/// FOLLOWS it (the <c>&gt;&gt;TURN</c> GR2 discipline); the LAST toggle affecting the option wins.
/// <c>ALL</c> (an event with an empty option list) fans out to every option of that directive — including an
/// <c>ALL … OFF</c>, the GR2 "turns off all flagging options" reset.
/// </summary>
public sealed class FlagState
{
    private readonly IReadOnlyList<FlagEvent> _events;

    /// <summary>The empty state — no directive; every option is OFF (the GR5 default) at every line.</summary>
    public static readonly FlagState Empty = new([]);

    private FlagState(IReadOnlyList<FlagEvent> events) => _events = events;

    /// <summary>Build the state from the frontend's directive events (already syntax-checked by
    /// <see cref="FlagDirectiveProcessor"/>). Null/empty ⇒ the OFF default everywhere.</summary>
    public static FlagState Build(IReadOnlyList<FlagEvent>? events)
        => events is null || events.Count == 0 ? Empty : new FlagState(events);

    /// <summary>Whether <paramref name="option"/> is flagging (ON) at a construct on
    /// <paramref name="siteLine"/>. The most recent toggle affecting the option strictly BEFORE the site wins;
    /// OFF when none precedes it. An event of the OTHER directive, or a specific-option event that does not name
    /// this option, does not affect it; an <c>ALL</c> event of this directive does.</summary>
    public bool IsOnAt(int siteLine, FlagOption option)
    {
        var directive = FlagOptions.Info(option).Directive;
        bool on = false;   // GR5 — every option defaults OFF
        foreach (var e in _events)
        {
            if (e.Line >= siteLine) break;                       // events are line-ordered; only preceding text applies
            if (e.Which != directive) continue;                  // the other directive cannot toggle this option
            bool affects = e.Options.Count == 0 || e.Options.Contains(option);   // empty ⇒ ALL fan-out
            if (affects) on = e.On;
        }
        return on;
    }

    /// <summary>Whether ANY option is (or could be) flagging anywhere — lets the pass skip the whole parse-tree
    /// walk when no FLAG directive is present (the zero-overhead invariant: a source with no FLAG directive is
    /// never walked).</summary>
    public bool Any => _events.Count > 0;
}
