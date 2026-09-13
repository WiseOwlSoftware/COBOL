// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// A level-88 condition-name (ISO §13.18.4 / §8.8.4.2.7 r2): a named boolean predicate over a conditional variable
/// (its immediately superior data item). It owns no storage — referencing it tests whether the parent's current
/// value is among the condition's VALUE set (singletons and THRU ranges); <c>SET cond TO TRUE</c> moves the first
/// VALUE into the parent (COBOLNET_DESIGN §3.5). The compiler renders both forms over the parent's <see cref="Place"/>.
/// </summary>
public sealed class Condition88
{
    /// <summary>The condition-name as written in the source.</summary>
    public required string Name { get; init; }

    /// <summary>The conditional variable (the immediately superior data item the condition tests).</summary>
    public required DataItem Parent { get; init; }

    /// <summary>
    /// The VALUE set: each entry is a single value (<c>High</c> null) or an inclusive THRU range. The raw source
    /// text of each operand is kept (e.g. <c>"\"Y\""</c>, <c>5</c>, <c>-9</c>); the emitter decodes it against the
    /// parent's category. The first entry's <c>Low</c> is what <c>SET … TO TRUE</c> stores.
    /// </summary>
    public List<(string Low, string? High)> Values { get; } = [];

    /// <summary>alphabet-name-1 of the clause's <c>IN</c> phrase as written (ISO §13.18.63.2 formats 3 and 5 —
    /// <c>[ IN alphabet-name-1 ]</c> stands OUTSIDE the repeated literal group, so one alphabet governs the whole
    /// VALUE set), or null when the phrase is absent. §14.7.8 rule 2 makes it the collating sequence every THRU
    /// range in the set is evaluated in, overriding the no-phrase arm's implementor default — which for this
    /// compiler is the PROGRAM COLLATING SEQUENCE.
    /// <para>⛔ IT WAS PARSED AND DROPPED (kb/Work PB398), which is a silent wrong answer rather than a missing
    /// feature: <c>88 X VALUE "M" THRU "A" IN AL</c> under an AL that reverses the alphabet answered NO for a
    /// value the range contains, because the range was weighed natively. The EVALUATE twin could not even be
    /// written. §14.7.8's opening sentence — "This specification applies to THROUGH phrases specified in the VALUE
    /// clause and the EVALUATE statement" — is why both now resolve through the one
    /// <c>DataBinder.TryResolveRangeAlphabet</c>.</para></summary>
    public string? Alphabet { get; set; }
}
