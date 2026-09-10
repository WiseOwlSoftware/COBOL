// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Antlr4.Runtime;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ EVERY alternative of the <c>dataDescriptionClause</c> grammar rule must have a
/// <see cref="DataClauseKind"/> — checked against the alternatives the GENERATED PARSER actually offers, not
/// against a list somebody remembered.
///
/// <para><b>Why this test exists (kb/Work PB487).</b> ISO §13.16.3 states FIVE rules over a data description
/// entry's WHOLE clause list — SR12 (SAME AS), SR13 (CONSTANT RECORD), SR14 (TYPE), SR17 (ANY LENGTH) and SR18
/// (DYNAMIC LENGTH); SR17 and SR18 literally read "the only other clauses permitted are …". Each was implemented
/// as an <c>||</c> chain over whichever local decode flags the author happened to remember, and each was
/// INCOMPLETE: SR12's chain could not see ALIGNED, DYNAMIC LENGTH, GROUP-USAGE, PROPERTY, SELECT WHEN or the
/// validation clauses, so <c>01 A SAME AS Z ALIGNED.</c> compiled clean; SR17's could not see nine of the
/// fourteen clauses it excludes. A hand list cannot stay complete as the grammar grows, so the rules now read a
/// SET and this test is what keeps the set total.</para>
///
/// <para><b>Both directions.</b> A grammar alternative with no <see cref="DataClauseKind"/> would silently drop
/// OUT of every permitted-set rule (the failure this closes); a map row naming a context type the grammar no
/// longer produces is a dead lookup, which this repository has learned is also never contradicted. Both fail
/// here.</para>
/// </summary>
public sealed class DataClauseKindDriftTests : CobolNetTestBase
{
    /// <summary>The sub-rule context types <c>dataDescriptionClause</c> can actually produce, read out of the
    /// GENERATED parser: ANTLR emits one no-argument accessor per alternative sub-rule on the alternative's
    /// context class, each returning that sub-rule's context type.</summary>
    private static IReadOnlyList<Type> GrammarAlternatives() =>
        typeof(Core.DataDescriptionClauseContext)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0
                        && typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType)
                        && m.ReturnType != typeof(Core.DataDescriptionClauseContext))
            .Select(m => m.ReturnType)
            .Distinct()
            .ToList();

    [Fact]
    public void EveryGrammarAlternative_HasADataClauseKind()
    {
        var alts = GrammarAlternatives();
        Assert.NotEmpty(alts);   // a reflection query that finds nothing is a broken probe, not a green result
        var missing = alts.Where(t => !DataClauseKinds.ByContextType.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0,
            "dataDescriptionClause grew an alternative with no DataClauseKind, so it is invisible to the "
            + "ISO §13.16.3 SR12/SR13/SR14/SR17/SR18 permitted-set rules: "
            + string.Join(", ", missing)
            + ". Add a member to DataClauseKind, a row to DataClauseKinds.ByContextType, and DECIDE whether the "
            + "new clause belongs in SameAsCoPermitted / ConstantRecordExcluded / LengthClauseCoPermitted.");
    }

    [Fact]
    public void EveryMappedContextType_IsStillAGrammarAlternative()
    {
        var alts = GrammarAlternatives().ToHashSet();
        var dead = DataClauseKinds.ByContextType.Keys.Where(t => !alts.Contains(t)).Select(t => t.Name).ToList();
        Assert.True(dead.Count == 0,
            "DataClauseKinds.ByContextType maps a context type dataDescriptionClause no longer produces — a "
            + "dead lookup is never contradicted, so it must be removed with its grammar alternative: "
            + string.Join(", ", dead));
    }

    [Fact]
    public void EveryKindIsMappedExactlyOnce_AndNoneIsNone()
    {
        var kinds = DataClauseKinds.ByContextType.Values.ToList();
        Assert.DoesNotContain(DataClauseKind.None, kinds);
        Assert.Equal(kinds.Count, kinds.Distinct().Count());   // two alternatives sharing one bit would merge them
        // Every declared member except None is reachable — an unmapped member is a bit no rule can ever see.
        var declared = Enum.GetValues<DataClauseKind>().Where(k => k != DataClauseKind.None).ToList();
        var unmapped = declared.Where(k => !kinds.Contains(k)).Select(k => k.ToString()).ToList();
        Assert.True(unmapped.Count == 0,
            "DataClauseKind declares a member no grammar alternative maps to: " + string.Join(", ", unmapped));
    }

    /// <summary>The §13.16.3 permitted-set constants are TRANSCRIPTIONS of the rules' own sentences; this pins
    /// each against the words, so a future edit that widens one has to change the assertion too.</summary>
    [Fact]
    public void PermittedSets_MatchTheRuleSentences()
    {
        // SR12: "…with any clauses except CONSTANT RECORD, entry-name, EXTERNAL, GLOBAL, level-number, and OCCURS."
        Assert.Equal(
            DataClauseKind.ConstantRecord | DataClauseKind.External | DataClauseKind.Global | DataClauseKind.Occurs,
            DataClauseKinds.SameAsCoPermitted);
        // SR13 ¶1: "The ANY LENGTH, BASED, BLANK WHEN ZERO, DYNAMIC LENGTH, select-when, SYNCHRONIZED, and
        // TYPEDEF clauses and validation-clauses shall not be specified in the same data description entry
        // with the CONSTANT RECORD clause…"
        Assert.Equal(
            DataClauseKind.AnyLength | DataClauseKind.Based | DataClauseKind.BlankWhenZero
            | DataClauseKind.DynamicLength | DataClauseKind.SelectWhen | DataClauseKind.Synchronized
            | DataClauseKind.Typedef | DataClauseKind.Validation,
            DataClauseKinds.ConstantRecordExcluded);
        // SR17/SR18: "…the only other clauses permitted are level-number, entry-name, PICTURE, USAGE, and VALUE."
        // Neither length clause is co-permitted with the other — the subject's own bit is OR-ed in at the site.
        Assert.Equal(
            DataClauseKind.Picture | DataClauseKind.Usage | DataClauseKind.Value,
            DataClauseKinds.LengthClauseCoPermitted);
        Assert.Equal(DataClauseKind.None, DataClauseKinds.LengthClauseCoPermitted & DataClauseKind.AnyLength);
        Assert.Equal(DataClauseKind.None, DataClauseKinds.LengthClauseCoPermitted & DataClauseKind.DynamicLength);
        // The error production is in NO permitted set — an unrecognized word never satisfies a composition rule.
        Assert.Equal(DataClauseKind.None, DataClauseKinds.SameAsCoPermitted & DataClauseKind.Unrecognized);
        Assert.Equal(DataClauseKind.None, DataClauseKinds.LengthClauseCoPermitted & DataClauseKind.Unrecognized);
    }

    /// <summary>Every bit has a spelling, so a diagnostic can NAME the offending clause rather than say
    /// "some other clause" — the half of the message that makes it actionable.</summary>
    [Fact]
    public void EveryKind_HasASpelling()
    {
        foreach (var k in Enum.GetValues<DataClauseKind>())
        {
            if (k == DataClauseKind.None) { Assert.Equal("", DataClauseKinds.Name(k)); continue; }
            Assert.False(string.IsNullOrWhiteSpace(DataClauseKinds.Name(k)),
                $"DataClauseKind.{k} has no spelling in DataClauseKinds.Name — a permitted-set diagnostic would "
                + "silently name nothing.");
        }
    }
}
