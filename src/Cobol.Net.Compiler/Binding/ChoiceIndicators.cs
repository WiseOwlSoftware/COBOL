// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE READER OF A GENERAL FORMAT'S CHOICE INDICATORS — ISO §5.2.6.4 — for the half a grammar cannot
/// express.
///
/// <para><b>What the figure says.</b> A brace or bracket carrying choice indicators (the `|` bars just inside
/// it) means: "When enclosed by brackets, zero or more of the alternatives contained within the choice
/// indicators shall be specified, but any single alternative may be specified only once" (braces: one or more),
/// and "The alternatives may be specified in any order." A parser rule can say ZERO-OR-MORE and ANY ORDER with
/// one <c>*</c>; it can say ONLY ONCE only by enumerating every ordering, which is factorial in the number of
/// alternatives and unreadable at three. So the grammar writes <c>(a | b | c)*</c> and the ONLY-ONCE half is
/// read here, once, by every format that has such a group.</para>
///
/// <para><b>Why it is a type and not an <c>if</c> at the verb</b> (kb/Work PB407, CLAUDE.md rule 5). The GOBACK
/// tail was written <c>(raisingPhrase | statusPhrase)?</c> — an ordered at-most-ONE stack — so four legal
/// spellings were refused as syntax errors. Relaxing that one rule to <c>*</c> and writing "if both, error" at
/// the GOBACK binder would make the NEXT figure with choice indicators need its own second copy. §14.9.18.2 is
/// not the only such figure; §14.9.20.2's category-name brace, §13.18.13.2's FOR ALPHANUMERIC / FOR NATIONAL
/// pair and §12.3.6.2's SOURCE-COMPUTER clauses each read the same rule, and each wrote it out again.</para>
/// </summary>
internal static class ChoiceIndicators
{
    /// <summary>Take the ONE occurrence of a choice-indicator alternative that a general format admits, and
    /// diagnose a repeat (§5.2.6.4, "any single alternative may be specified only once"). Returns the FIRST
    /// occurrence so binding continues on a shape the rest of the binder can read; null when the alternative
    /// was not specified at all — which the enclosing BRACKET makes legal, and which is why this returns a
    /// nullable rather than demanding one.</summary>
    /// <param name="edition">The one diagnostic sink.</param>
    /// <param name="occurrences">Every parse of this alternative, in written order (ANTLR's array for a
    /// <c>*</c>/<c>+</c> sub-rule).</param>
    /// <param name="statement">The statement as the user wrote it, for the message ("GOBACK").</param>
    /// <param name="alternative">The alternative as the figure names it ("the RAISING phrase").</param>
    /// <param name="clause">The clause whose general format this is ("14.9.18.2"), so the message cites the
    /// FIGURE as well as §5.2.6.4.</param>
    public static T? AtMostOnce<T>(EditionContext edition, T[] occurrences,
                                   string statement, string alternative, string clause)
        where T : ParserRuleContext
    {
        if (occurrences.Length > 1)
            edition.Error(DiagnosticCatalog.ChoiceAlternativeRepeated,
                $"{statement}: {alternative} is specified {occurrences.Length} times; ISO §{clause}'s general "
                + "format encloses it in choice indicators, and §5.2.6.4 admits any single alternative only "
                + "once (in any order, but once)");
        return occurrences.Length > 0 ? occurrences[0] : null;
    }
}
