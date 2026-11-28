// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Antlr4.Runtime;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ EVERY FILE DESCRIPTION CLAUSE THE GRAMMAR PARSES HAS DECIDED WHICH §2023:13.4.5.2 FORMATS CONTAIN IT (kb/Work PB1238).
///
/// <para><b>Why it exists.</b> Until PB1238 no code decided which of ISO §2023:13.4.5.2's three formats an FD was, so
/// every clause bound against every organization: LINAGE on a relative file, CODE-SET on an indexed one and LINAGE
/// on a report FD compiled clean, and the five file-control-entry alternatives the grammar carries in the FD
/// position had no binder arm at all — parsed and silently DROPPED. The admissibility table
/// (<see cref="FileDescriptionFormats"/>) is only automatic for the NEXT clause if a new <c>fileDescriptionClause</c>
/// alternative cannot land without a row; these facts read the generated parser, not a list, so it cannot.</para>
/// </summary>
public sealed class FileDescriptionFormatDriftTests
{
    /// <summary>The alternatives of <c>fileDescriptionClause</c> as the GENERATED parser exposes them: its
    /// no-argument accessors returning a rule context, less the <c>unrecognizedDataClause</c> error production.</summary>
    private static IReadOnlyList<Type> Alternatives() =>
        typeof(Core.FileDescriptionClauseContext)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0
                        && typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType)
                        && m.ReturnType != typeof(Core.UnrecognizedDataClauseContext))
            .Select(m => m.ReturnType)
            .Distinct()
            .ToList();

    /// <summary>Both directions: every alternative has a row (a new clause is decided, never bound by default)
    /// and every row names a live alternative (a deleted clause leaves no stale row behind).</summary>
    [Fact]
    public void TheAdmissibilityTable_IsTotalOverTheGrammarsAlternatives()
    {
        var alternatives = Alternatives();
        Assert.True(alternatives.Count >= 15,
            $"only {alternatives.Count} fileDescriptionClause alternative(s) found — the reflection probe is broken "
            + "and the obligations below would hold vacuously");
        var missing = alternatives.Where(t => !FileDescriptionFormats.ByContext.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0, "fileDescriptionClause alternative(s) with no FileDescriptionFormats row, so no "
            + "§2023:13.4.5.2 format decides them: " + string.Join(", ", missing));
        var stale = FileDescriptionFormats.ByContext.Keys.Except(alternatives).Select(t => t.Name).ToList();
        Assert.True(stale.Count == 0, "FileDescriptionFormats row(s) naming no fileDescriptionClause alternative: "
            + string.Join(", ", stale));
        Assert.Equal(FileDescriptionFormats.Rows.Count, FileDescriptionFormats.ByContext.Count);
    }

    /// <summary>A row either names the formats that contain its clause or names the entry the clause belongs to —
    /// never neither (a clause no format admits and no message can redirect) and never both.</summary>
    [Fact]
    public void EveryRow_IsAdmittedSomewhere_OrNamesItsHomeEntry()
    {
        foreach (var row in FileDescriptionFormats.Rows)
            foreach (int year in new[] { 85, 2002 })
            {
                var formats = row.FormatsIn(EditionInfo.Of(year));
                Assert.True((formats != FileDescriptionFormat.None) ^ (row.HomeEntry is not null),
                    $"{row.Name} at {year}: Formats={formats}, HomeEntry={row.HomeEntry ?? "null"}");
            }
    }

    /// <summary>⛔ THE 1985 COLUMNS ARE THE 1985 PAGES' (kb/Work PB6027). ANSI X3.23-1985's report-file entry (§1985:XIII.3.2.2, and
    /// §1985:X.4.2.2 Format 3 with EXTERNAL and GLOBAL, both rendered from the printed pages) prints BLOCK CONTAINS, RECORD, LABEL
    /// RECORDS, VALUE OF, CODE-SET and REPORT, and no DATA RECORDS clause, which the sequential and the relative-or-indexed entries
    /// print (§1985:X.4.2.2 Formats 1 and 2). Each row's 1985 admission is held to that list, so a row copied from the 2023 lists
    /// cannot admit a clause the 1985 report-file entry does not print.</summary>
    [Fact]
    public void At1985_TheReportFileEntry_AdmitsExactlyTheClausesItsPrintedFormatShows()
    {
        var edition = EditionInfo.Of(85);
        var admitted = FileDescriptionFormats.Rows
            .Where(r => r.IntroducedIn <= 85 && (r.FormatsIn(edition) & FileDescriptionFormat.Report) != 0)
            .Select(r => r.Context).ToHashSet();
        Assert.Equal(new HashSet<Type>
        {
            typeof(Core.ExternalClauseContext), typeof(Core.GlobalClauseContext), typeof(Core.BlockContainsClauseContext),
            typeof(Core.RecordClauseContext),
            typeof(Core.LabelRecordsClauseContext), typeof(Core.ValueOfClauseContext), typeof(Core.CodeSetClauseContext),
            typeof(Core.ReportClauseContext),
        }, admitted);
        Assert.NotEqual(FileDescriptionFormat.None,
            FileDescriptionFormats.ByContext[typeof(Core.DataRecordsClauseContext)].FormatsIn(edition) & FileDescriptionFormat.Sequential);
    }

    /// <summary>The format is decided from the file control entry first: §2023:9.1.22 makes a report file SEQUENTIAL,
    /// so a relative or indexed file is Format 2 even when its FD writes a REPORT clause (§2023:13.4.5.3 SR7).</summary>
    [Theory]
    [InlineData(FileOrganization.Sequential, false, "Sequential")]
    [InlineData(FileOrganization.LineSequential, false, "Sequential")]
    [InlineData(FileOrganization.Sequential, true, "Report")]
    [InlineData(FileOrganization.LineSequential, true, "Report")]
    [InlineData(FileOrganization.Relative, false, "RelativeOrIndexed")]
    [InlineData(FileOrganization.Indexed, true, "RelativeOrIndexed")]
    public void TheFormat_IsDecidedByOrganizationThenReportClause(
        FileOrganization organization, bool hasReport, string expected) =>
        Assert.Equal(Enum.Parse<FileDescriptionFormat>(expected), FileDescriptionFormats.Of(organization, hasReport));

    /// <summary>⛔ THE LINAGE CLAIM IS HELD TO THE TABLE (kb/Work PB2503). Annex A.4.11 item 10 lists the LINAGE clause
    /// (§2023:13.18.34) among the optional Report Writer elements, and A.4.1 lets an implementation accept it "only when
    /// support for that language element is claimed". <c>docs/CONFORMANCE.md</c> §5's A.4.11 row says the clause is
    /// CLAIMED and that it is a Format 1 clause only; this reads that row and the table, so neither can change alone:
    /// a LINAGE row that admits a second format, or a row that stops saying "CLAIMED", is red.</summary>
    [Fact]
    public void TheLinageClaim_AgreesWithTheFileDescriptionFormatTable()
    {
        // A.4.11's two sentences are held by scripts/docchecks/facts.py (linage_claim_findings); the table's side is here,
        // so no compiler test opens the Markdown (kb/Work PB4453).
        Assert.Equal(FileDescriptionFormat.Sequential,
            FileDescriptionFormats.ByContext[typeof(Core.LinageClauseContext)].Formats);
    }
}
