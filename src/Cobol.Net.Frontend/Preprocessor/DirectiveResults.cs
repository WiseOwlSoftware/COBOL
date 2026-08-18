// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The frontend's ISO §7.3 directive outputs for one compilation group, as ONE record the binder consumes
/// (<c>CSharpEmitter.Bind</c> / <c>BinderDriver.Bind</c>): the <c>&gt;&gt;TURN</c> events (§7.3.25 — the group's
/// compile-time TurnState), the <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> events (§7.3.23), the <c>&gt;&gt;FLAG-02</c> /
/// <c>&gt;&gt;FLAG-14</c> events (§7.3.14 / §7.3.15), the <c>&gt;&gt;COBOL-WORDS</c> map (§7.3.10) and the
/// <c>&gt;&gt;LEAP-SECOND</c> state (§7.3.17). A directive that gains behavior adds a member here — never a new
/// positional parameter on Bind (kb/Work PB65).
/// </summary>
public sealed record DirectiveResults(
    IReadOnlyList<TurnEvent> TurnEvents,
    IReadOnlyList<RefModZeroLengthEvent> RefModZeroLengthEvents,
    IReadOnlyList<FlagEvent> FlagEvents,
    CobolWordsMap CobolWordsMap,
    bool LeapSecondOn)
{
    /// <summary>No directives at all — the OFF/empty default for every member.</summary>
    public static readonly DirectiveResults None = new([], [], [], CobolWordsMap.Empty, false);
}
