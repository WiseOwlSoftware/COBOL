// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The WARNING pin of a conformance test (<c>wiseowl/diagnostics/</c>'s <c>warn-at</c>, carried as <see cref="SuiteTest.Warns"/>;
/// DESIGN-conformance-suite.md §4, §9; kb/Work PB5461): a program WiseOwl compiles and warns of is judged by its verdict AND by the
/// warning. The five flagged tests of the 1985 declaratives rows exercise the flagged arm of <see cref="ConformanceSuiteRunner.Judge"/>;
/// the PLANTED tests here exercise the accepted arm, which no suite test reaches, and each assertion fails when the check is missing.
/// </summary>
public sealed class ConformanceSuiteWarnsTests
{
    private const string Edition = "85";

    /// <summary>A program whose declarative section PERFORMs a nondeclarative paragraph (§1985:VII.4.6.4 GR2): WiseOwl compiles it with
    /// warning COBOL2375 and runs it (the declarative never runs: the file is never opened).</summary>
    private const string ViolatingProgram = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. WARNEDP.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F ASSIGN TO "warnedp.dat" FILE STATUS IS ST.
               DATA DIVISION.
               FILE SECTION.
               FD  F.
               01  R PIC X.
               WORKING-STORAGE SECTION.
               01  ST PIC XX.
               PROCEDURE DIVISION.
               DECLARATIVES.
               ERR SECTION.
                   USE AFTER STANDARD ERROR PROCEDURE ON F.
               ERR-1.
                   PERFORM P-1.
               END DECLARATIVES.
               MAIN-1 SECTION.
               P-1.
                   DISPLAY "ok".
                   STOP RUN.
        """;

    private static SuiteResult Judge(string source, string? warns)
    {
        string dir = ScratchDirectory.Create("warns");
        try
        {
            const string id = "planted";
            File.WriteAllText(Path.Combine(dir, id + ".cob"), source + "\n", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(dir, id + ".out"), "ok\n", new UTF8Encoding(false));
            var test = new SuiteTest(id, "planted", Path.Combine(dir, id + ".cob"), "fixed", [], [], [], "accept", [],
                new Dictionary<string, string> { [""] = Path.Combine(dir, id + ".out") }, 0, null, false,
                new Dictionary<string, string>(), null, null, Warns: warns);
            return ConformanceSuiteRunner.Judge(Edition, test);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void AnAcceptedTest_ThatPinsTheWarningWiseOwlReports_Passes()
    {
        SuiteResult result = Judge(ViolatingProgram, "COBOL2375");
        Assert.True(result.Verdict == ConformanceSuiteRunner.Pass, $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void AnAcceptedTest_ThatPinsAWarningWiseOwlDoesNotReport_Fails()
    {
        SuiteResult result = Judge(ViolatingProgram, "COBOL9999");
        Assert.True(result.Verdict == ConformanceSuiteRunner.Fail && result.Reason.Contains("reports no warning COBOL9999"),
            $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void AnAcceptedTest_ThatPinsNoWarning_IsJudgedByItsOutputAlone()
    {
        SuiteResult result = Judge(ViolatingProgram, null);
        Assert.True(result.Verdict == ConformanceSuiteRunner.Pass, $"{result.Verdict}: {result.Reason}");
    }
}
