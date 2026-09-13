// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ REPETITION IN A REPORT GROUP IS A SUBTREE REPLAY, AND EVERY CLAUSE RIDES IT FOR FREE (kb/Work PB565).
/// ISO §13.18.38.4 GR10 says a repeating entry "causes the entry to define integer-2 distinct report items" and
/// GR11 says every clause has "the same effect on each repetition as they would on a single data item without
/// the OCCURS clause" — so the ONE correct shape is to bind the entry's subtree once per repetition, not to
/// teach each clause about repetition. <c>DataBinder.BindReportEntries</c> does exactly that, which is why
/// §13.18.63.4 GR21's import of GR9 (a VALUE reaches every occurrence), GR22's OCCURS-DEPENDING suppressor and
/// §13.18.64's VARYING counters all became correct at once.
///
/// <para>Three guards keep it structural:</para>
/// <list type="number">
/// <item>the replay call is the ONLY repetition site — no clause binder may loop over an OCCURS count itself;</item>
/// <item>every <c>ReportColumnKindModel</c> is handled at BOTH placement readers (the emitter's compose switch
/// and the binder's line-width walk) — a new placement kind that reaches only one of them is this repo's
/// most reproducible defect, the two-arm dispatch with one arm fixed;</item>
/// <item>the residue is NAMED: the axis that still stages loud is the VERTICAL one, and its diagnostic says so,
/// so "OCCURS is not implemented" can never be re-broadened into a blanket refusal of the live axis.</item>
/// </list>
/// </summary>
public sealed class ReportRepeatingEntryDriftTests
{
    private static string BinderText() => File.ReadAllText(
        Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "DataBinder.Reports.cs"));

    private static string EmitterText() => File.ReadAllText(
        Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "CodeGen", "Verbs", "ReportWriterEmitter.cs"));

    /// <summary>The replay is ONE call site. A second place that multiplies a report entry by an OCCURS count
    /// would be a second copy of GR10, and the two would drift the day GR11, GR12 or GR13 changed.</summary>
    [Fact]
    public void TheSubtreeReplay_IsTheOnlyRepetitionSite()
    {
        string src = BinderText();
        // Population: the replay exists and this scan can see it (a rename must fail loudly, not vacuously).
        Assert.Contains("private void BindReportEntries(", src, StringComparison.Ordinal);
        Assert.Contains("for (int rep = 0; rep < occurs.Max; rep++)", src, StringComparison.Ordinal);

        // No OTHER loop in the report binder may be driven by a repeating entry's count.
        var others = Regex.Matches(src, @"<\s*\w*[Oo]ccurs\w*\.Max\b|<=\s*\w*[Oo]ccurs\w*\.Max\b")
            .Select(m => m.Value).ToList();
        Assert.Single(others);
    }

    /// <summary>Every placement kind is handled at BOTH readers. The list is derived from the enum's own
    /// declaration, so adding a kind fails here until both arms name it.</summary>
    [Fact]
    public void EveryColumnPlacementKind_IsReadByBothPlacementReaders()
    {
        string binder = BinderText(), emitter = EmitterText();
        var kinds = Regex.Matches(
                binder[binder.IndexOf("public enum ReportColumnKindModel", StringComparison.Ordinal)..],
                @"^\s{4}(\w+),\s*$", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).ToList();
        // Population: the enum really has the four placement kinds this guard was written for.
        Assert.Equal(new[] { "Absolute", "Relative", "AnchorSeed", "AnchorStep" }, kinds);

        // The binder's line-width walk and the emitter's compose switch each name every kind — except that both
        // may spell the LAST arm as the switch default, which is exhaustive by construction.
        foreach (string kind in kinds.Take(kinds.Count - 1))
        {
            Assert.Contains($"ReportColumnKindModel.{kind}", binder, StringComparison.Ordinal);
            Assert.Contains($"ReportColumnKindModel.{kind}", emitter, StringComparison.Ordinal);
        }
        Assert.Contains("anchors[spec.AnchorId]", binder, StringComparison.Ordinal);   // the width walk's anchors
        Assert.Contains("__ra{spec.AnchorId}", emitter, StringComparison.Ordinal);     // the compose method's
    }

    /// <summary>The loud residue names the AXIS, not the clause. A diagnostic reading "OCCURS … is not yet
    /// implemented" would claim the live horizontal axis too — the shape this note's defect had.</summary>
    [Fact]
    public void TheStagedResidue_NamesTheVerticalAxisOnly()
    {
        string catalog = File.ReadAllText(Path.Combine(
            TestRepo.Src("Cobol.Net.Editions"), "Diagnostics", "DiagnosticCatalog.cs"));
        int at = catalog.IndexOf("ReportOccursInGroup = new(", StringComparison.Ordinal);
        Assert.True(at > 0, "ReportOccursInGroup is gone or renamed — re-point this guard.");
        string body = catalog[at..catalog.IndexOf(");", at, StringComparison.Ordinal)];
        Assert.Contains("VERTICAL", body, StringComparison.Ordinal);
        Assert.Contains("GR10c", body, StringComparison.Ordinal);

        // And the binder raises it ONLY on the vertical axis — i.e. guarded by a LINE-clause test.
        string binder = BinderText();
        int site = binder.IndexOf("DiagnosticCatalog.ReportOccursInGroup", StringComparison.Ordinal);
        Assert.True(site > 0, "the staged arm is gone — if the vertical axis landed, delete this guard with it.");
        Assert.Contains("reportLineClause()", binder[Math.Max(0, site - 400)..site], StringComparison.Ordinal);
    }

    /// <summary>A repetition's VARYING counter is the CLOSED form over the repetition ordinal, never an
    /// accumulator: a replayed entry becomes one field per repetition, and a field-local accumulator could not
    /// span them (ISO §13.18.64.4 GR3 — occurrence n holds FROM + n × BY).</summary>
    [Fact]
    public void TheVaryingCounter_IsClosedFormOverTheRepetitionOrdinal()
    {
        string emitter = EmitterText();
        Assert.Contains("f.RepetitionOrdinal + rep", emitter, StringComparison.Ordinal);
        // The accumulator shape that cannot survive a replay.
        Assert.DoesNotContain("+= {VaryValue(", emitter, StringComparison.Ordinal);
    }
}
