// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A REPORT SECTION ENTRY'S DATA-NAME IS NOT "UNDEFINED" WHERE IT MAY NOT BE REFERENCED (kb/Work PB1046).
/// ISO §13.8.6.2.3: "Report items shall not be accessed or referred to by any clauses in any other section of the
/// data division or by any other procedure division statements, except in the following cases: 1) Sum counters may
/// be inspected or altered in the procedure division. 2) Report groups of type DETAIL are accessed by the GENERATE
/// statement", and §13.15.3 SR7: "The data-name shall not be referenced in any other way." The reference is refused
/// (COBOL5092), under THAT rule — it used to be told COBOL1639 "no declaration gives the name", false about a
/// name the report section declares. One lookup (<c>DataBinder.ReportEntriesNamed</c>) and one refusal
/// (<c>DataBinder.ReportEntryReferenceRefused</c>) serve every arm that used to say "not defined"; the complement
/// test names the arms so a fourth cannot be added without asking.
/// </summary>
public sealed class ReportItemReferenceTests
{
    private const string ReportItemCode = "COBOL5092";

    private static string Source(string wsExtra, string reportExtra, string procedure, string globalRd = "",
        string fileExtra = "", string innerProgram = "") => string.Join("\n",
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. PB1046U.",
        "       ENVIRONMENT DIVISION.",
        "       INPUT-OUTPUT SECTION.",
        "       FILE-CONTROL.",
        "           SELECT PRT ASSIGN TO \"pb1046u.rpt\".",
        fileExtra,
        "       DATA DIVISION.",
        "       FILE SECTION.",
        $"       FD  PRT{(globalRd.Length > 0 ? " GLOBAL" : "")} REPORT IS RR.",
        "       WORKING-STORAGE SECTION.",
        "       01  W PIC X(3) VALUE \"ABC\".",
        "       01  N PIC 9(3) VALUE 7.",
        "       01  PTR USAGE POINTER.",
        wsExtra,
        "       REPORT SECTION.",
        $"       RD  RR{globalRd}.",
        "       01  DL TYPE DETAIL LINE PLUS 1.",
        "           05  PV COLUMN 5 PIC X(3) SOURCE W.",
        "           05  PN COLUMN 10 PIC 9(3) SOURCE N.",
        "           05  SC COLUMN 15 PIC 9(4) SUM N.",
        reportExtra,
        "       PROCEDURE DIVISION.",
        "       MAIN.",
        procedure,
        "           STOP RUN.",
        innerProgram,
        "       END PROGRAM PB1046U.",
        "");

    private static string Inner(string body) => string.Join("\n",
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. PB1046UI.",
        "       PROCEDURE DIVISION.",
        "       SHOW.",
        body,
        "           EXIT PROGRAM.",
        "       END PROGRAM PB1046UI.");

    /// <summary>§13.8.6.2.3 — every procedure division statement shape that names a printable item, a group entry
    /// or a report group (qualified in every way §8.4.2.2.3 SR4 allows) draws the ONE report-item refusal, never
    /// the undefined-name verdict.</summary>
    [Theory]
    [InlineData("MOVE PV TO W")]
    [InlineData("MOVE W TO PV")]
    [InlineData("DISPLAY PV OF DL")]
    [InlineData("DISPLAY PV OF DL OF RR")]
    [InlineData("DISPLAY PV OF RR")]
    [InlineData("DISPLAY DL")]
    [InlineData("DISPLAY DL OF RR")]
    [InlineData("ADD 1 TO PN")]
    [InlineData("IF PV = \"ABC\" CONTINUE END-IF")]
    [InlineData("SET PTR TO ADDRESS OF PV")]
    [InlineData("MOVE PV(1:2) TO W")]
    [InlineData("INITIALIZE DL")]
    public void AReportEntryReferencedByAProcedureStatement_IsRefusedAsAReportItem(string statement)
    {
        var d = Bind(Source("", "", "           " + statement + "."));
        Assert.Single(d, x => x.Contains(ReportItemCode));
        Assert.DoesNotContain(d, x => x.Contains("COBOL1639"));
    }

    /// <summary>§13.8.6.2.3 1) — "Sum counters may be inspected or altered in the procedure division": the SUM entry's
    /// data-name stays referenceable, and only the printable-item faces around it are refused.</summary>
    [Fact]
    public void ASumCounter_IsStillReferenceableInTheProcedureDivision()
    {
        var d = Bind(Source("", "", "           ADD 100 TO SC.\n           MOVE SC TO N."));
        Assert.DoesNotContain(d, x => x.Contains("error"));
    }

    /// <summary>§13.8.6.2.3 2) — GENERATE names the DETAIL group, by name and qualified by the report-name.</summary>
    [Theory]
    [InlineData("GENERATE DL")]
    [InlineData("GENERATE DL OF RR")]
    public void AGenerateOfADetailGroup_IsNotAReportItemReference(string statement)
    {
        var d = Bind(Source("", "", "           OPEN OUTPUT PRT.\n           INITIATE RR.\n           " + statement
            + ".\n           TERMINATE RR.\n           CLOSE PRT."));
        Assert.DoesNotContain(d, x => x.Contains("error"));
    }

    /// <summary>§8.4.2.1 — a name an ordinary item declares resolves THERE: a procedure division reference to a
    /// working-storage item that shares the spelling of a report entry is that item's, not the report item's.</summary>
    [Fact]
    public void AnOrdinaryItemOfTheSameSpelling_TakesTheReference()
    {
        var d = Bind(Source("       01  PV PIC X(3) VALUE \"XYZ\".", "", "           MOVE PV TO W."));
        Assert.DoesNotContain(d, x => x.Contains("error"));
    }

    /// <summary>The refusal is for a name the qualifiers REACH: a qualifier that is no level above the entry (and not
    /// its report) reaches nothing, so the reference is the undefined-name verdict it always was.</summary>
    [Theory]
    [InlineData("MOVE PV OF W TO W")]
    [InlineData("MOVE PV OF PN TO W")]
    [InlineData("MOVE NOWHERE TO W")]
    public void AReferenceTheQualifiersDoNotReach_KeepsTheUndefinedNameVerdict(string statement)
    {
        var d = Bind(Source("", "", "           " + statement + "."));
        Assert.DoesNotContain(d, x => x.Contains(ReportItemCode));
        Assert.Contains(d, x => x.Contains("COBOL1639"));
    }

    /// <summary>§13.18.27.4 GR1/GR2 — a GLOBAL report's names are visible to the contained program, so a printable
    /// item there is the same refusal; its sum counter and its DETAIL group stay legal. A report WITHOUT the GLOBAL
    /// clause is no name of the contained program at all, so the old verdict is the right one.</summary>
    [Fact]
    public void AGlobalReportsItem_IsRefusedInAContainedProgram_AndANonGlobalOneIsUndefinedThere()
    {
        string legal = Source("", "", "           CALL \"PB1046UI\".", " GLOBAL", innerProgram:
            Inner("           ADD 100 TO SC.\n           GENERATE DL."));
        Assert.DoesNotContain(Bind(legal), x => x.Contains("error"));

        string refused = Source("", "", "           CALL \"PB1046UI\".", " GLOBAL", innerProgram:
            Inner("           DISPLAY PV.\n           DISPLAY DL."));
        var d = Bind(refused);
        Assert.Equal(2, d.Count(x => x.Contains(ReportItemCode)));
        Assert.DoesNotContain(d, x => x.Contains("COBOL1639"));

        string hidden = Source("", "", "           CALL \"PB1046UI\".", "", innerProgram:
            Inner("           DISPLAY PV."));
        var h = Bind(hidden);
        Assert.DoesNotContain(h, x => x.Contains(ReportItemCode));
        Assert.Contains(h, x => x.Contains("COBOL1639"));
    }

    /// <summary>§13.8.6.2.3 — "any clauses in any other section of the data division": the FILE-CONTROL clause that
    /// names a data-name (FILE STATUS) and a working-storage OCCURS … DEPENDING ON each draw the same refusal.</summary>
    [Fact]
    public void AReportEntryNamedByAClauseOfAnotherSection_IsRefusedAsAReportItem()
    {
        var status = Bind(Source("", "", "", fileExtra: "           SELECT DAT ASSIGN TO \"pb1046u.dat\" FILE STATUS IS PN."));
        Assert.Single(status, x => x.Contains(ReportItemCode) && x.Contains("FILE STATUS"));

        var depending = Bind(Source(
            "       01  TAB.\n           05  ELT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON PN.", "", ""));
        Assert.Single(depending, x => x.Contains(ReportItemCode) && x.Contains("DEPENDING ON"));
    }

    /// <summary>The complement of the scans above (feedback_measure_the_selectors_complement): the three arms that
    /// used to say "not defined" about a report entry's name each ask the one refusal before they say it, and nothing
    /// else reports the descriptor.</summary>
    [Fact]
    public void EveryUndefinedNameArm_AsksTheOneRefusal()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string rel in new[]
                 {
                     Path.Combine("Binding", "ReferenceResolver.cs"),   // procedure division: ReportUnidentified
                     Path.Combine("Binding", "DataBinder.cs"),          // file / LINAGE / APPLY COMMIT: ResolveClauseOperand
                     Path.Combine("Binding", "DataBinder.Odo.cs"),      // OCCURS … DEPENDING ON
                 })
            Assert.Contains("ReportEntryReferenceRefused(", File.ReadAllText(Path.Combine(root, rel)));

        var reporters = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("Generated", StringComparison.Ordinal) && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("DiagnosticCatalog.ReportItemReference", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();
        Assert.Equal(["DataBinder.Reports.cs"], reporters);
    }

    private static IReadOnlyList<string> Bind(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb1046_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(2023);
            new CSharpEmitter().Bind(tree!, edition);
            return [.. edition.Diagnostics];
        }
        finally { FileHarness.TryDelete(path); }
    }
}
