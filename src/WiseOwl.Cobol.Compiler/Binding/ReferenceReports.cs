// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// ⛔ <b>ONE REPORT PER WRITTEN REFERENCE, IN ONE LEDGER</b> (kb/Work PB70, PB3230). Several binders ask about the same
/// written reference (SET classifies its operand and then binds it; a receiving chokepoint resolves what a screen already
/// resolved), and each such ask must reach the same answer while the reference is reported once. The ledger is keyed by
/// the written node's IDENTITY (a <see cref="ParserRuleContext"/> or a token) and by the rule the report is about, so a
/// reference refused under one rule is still asked the next. A <see cref="ReferenceResolver"/> and its prober share one;
/// the resolver's <see cref="ReferenceResolver.ReportOnce"/> is the one way to ask it, and a probe is never granted a
/// report.
/// <para>It replaced seven sets on three types (the resolver's <c>Diagnosed</c>, the qualification part's index-name,
/// qualified-index gate and LINKAGE-leg sets, and the condition binder's three condition-name sets), the resolver's each
/// with its own reader of the probe flag.</para>
/// </summary>
internal sealed class ReferenceReports
{
    private readonly HashSet<(ReferenceReportRule Rule, object Written)> _reported = new(WrittenKey.Comparer);

    /// <summary>True the first time <paramref name="written"/> is reported under <paramref name="rule"/>.</summary>
    internal bool First(ReferenceReportRule rule, object written) => _reported.Add((rule, written));

    /// <summary>True when the data reference or reference modifier <paramref name="written"/> has been reported about
    /// itself (<see cref="ReferenceReportRule.Reference"/>): a null answer the receiving chokepoint gets back is then
    /// already reported, never silently dropped (kb/Work PB70).</summary>
    internal bool WasReported(ParserRuleContext written) => _reported.Contains((ReferenceReportRule.Reference, written));

    /// <summary>Equality by the rule and the written node's identity: two parse nodes with the same text are two references.</summary>
    private sealed class WrittenKey : IEqualityComparer<(ReferenceReportRule Rule, object Written)>
    {
        internal static readonly WrittenKey Comparer = new();

        public bool Equals((ReferenceReportRule Rule, object Written) x, (ReferenceReportRule Rule, object Written) y) =>
            x.Rule == y.Rule && ReferenceEquals(x.Written, y.Written);

        public int GetHashCode((ReferenceReportRule Rule, object Written) key) =>
            HashCode.Combine(key.Rule, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(key.Written));
    }
}

/// <summary>What a report about a written reference is about — the key <see cref="ReferenceReports"/> counts once by.</summary>
internal enum ReferenceReportRule
{
    /// <summary>The reference or reference modifier itself: an undefined or refused name, a rejected shape, a screen of
    /// its subscripts or modifier, the §2023:13.7.3 SR4 LINKAGE screen of a data-name.</summary>
    Reference,

    /// <summary>The resolution of a written index-name reference (§2023:8.4.2.2.2 Format 3): its "not defined" and its
    /// ambiguity (§2023:8.4.2.2.3 SR1, SR6).</summary>
    IndexName,

    /// <summary>The qualified-index-name-2002 edition gate of a written qualified index-name.</summary>
    QualifiedIndexGate,

    /// <summary>The §2023:13.7.3 SR4 LINKAGE screen of a written index-name, function-pointer-name or condition-name (e).</summary>
    LinkageLeg,

    /// <summary>The uniqueness of a written condition-name reference (§2023:8.4.2.2 Format 2), and its qualification when the
    /// written qualifiers reach no level-88 the word declares.</summary>
    ConditionName,

    /// <summary>A reference modifier written on a condition-name, which names no data item (§2023:8.4.3.3.3 SR5; kb/Work PB2468).</summary>
    ConditionNameRefMod,
}
