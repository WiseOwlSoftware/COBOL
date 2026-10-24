// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1925 — every diagnostic the conformance pass builds from a BOUND node names the file, line and column of
/// the statement (or data entry) it is about. A bound node carries no parse-tree span, so the binder's one statement
/// funnel stamps the cursor on <c>BoundStatement.At</c> and the pass positions its sink from it; this class pins both
/// halves: the SORT / MERGE / COMMIT / ROLLBACK placement rules (ISO §14.9.40.3 SR3, §14.9.24.3 SR1, §14.9.7.3 SR2,
/// §14.9.36.3 SR2), the SET / MOVE statement gates and the USAGE data gate. Each refusal is asserted with its
/// legal twin (the same statement outside the procedure, or at the edition that allows it), because a position
/// stamped on the wrong statement, or a refusal that no longer fires, is the failure the success path alone cannot see.
/// </summary>
public sealed class BoundArmDiagnosticPositionTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("CobolNet_BoundPos_");

    public void Dispose() { ScratchDirectory.Remove(_dir); }

    private const string Prologue =
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. PB1925T.\n" +
        "       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n" +
        "       FILE-CONTROL.\n" +
        "           SELECT SW ASSIGN TO \"pb1925-sw.tmp\".\n" +
        "           SELECT SW2 ASSIGN TO \"pb1925-sw2.tmp\".\n" +
        "           SELECT F1 ASSIGN TO \"pb1925-f1.dat\".\n" +
        "           SELECT F2 ASSIGN TO \"pb1925-f2.dat\".\n" +
        "           SELECT F3 ASSIGN TO \"pb1925-f3.dat\".\n" +
        "       DATA DIVISION.\n" +
        "       FILE SECTION.\n" +
        "       SD SW.\n" +
        "       01 SR.\n" +
        "          05 SK PIC X(4).\n" +
        "       SD SW2.\n" +
        "       01 SR2.\n" +
        "          05 SK2 PIC X(4).\n" +
        "       FD F1.\n" +
        "       01 R1 PIC X(4).\n" +
        "       FD F2.\n" +
        "       01 R2 PIC X(4).\n" +
        "       FD F3.\n" +
        "       01 R3 PIC X(4).\n" +
        "       WORKING-STORAGE SECTION.\n" +
        "       01 FLAG PIC 9 VALUE 1.\n" +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN SECTION.\n" +
        "       M1.\n";

    private const string Sort = "SORT SW2 ASCENDING KEY SK2 USING F1 GIVING F3.";
    private const string Merge = "MERGE SW2 ASCENDING KEY SK2 USING F1 F2 GIVING F3.";
    private const string Commit = "COMMIT.";
    private const string Rollback = "ROLLBACK.";

    private static string Outer(string kind) => kind switch
    {
        "sort-input" => "           SORT SW ASCENDING KEY SK INPUT PROCEDURE IS PR GIVING F3\n           STOP RUN.\n",
        "sort-output" => "           SORT SW ASCENDING KEY SK USING F1 OUTPUT PROCEDURE IS PR\n           STOP RUN.\n",
        "merge-output" => "           MERGE SW ASCENDING KEY SK USING F1 F2 OUTPUT PROCEDURE IS PR\n           STOP RUN.\n",
        // the procedure is a named section nothing sorts through: the statement inside it is NOT in a SORT / MERGE procedure
        "none" => "           SORT SW ASCENDING KEY SK USING F1 GIVING F3\n           PERFORM PR\n           STOP RUN.\n",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Compile a program whose procedure PR holds <paramref name="inner"/>; <paramref name="nestInIf"/>
    /// puts it under an IF, so the position is the nested statement's, not the IF's. Returns the statement's
    /// 1-based line, the success flag, and every error and warning line.</summary>
    private (int InnerLine, bool Ok, List<string> Lines) Build(string outer, string inner, int edition, bool nestInIf = false,
        bool permissive = false)
    {
        string bare = inner.TrimEnd('.');   // under an IF the sentence's period closes the IF, not the statement
        var body = nestInIf
            ? "       PR SECTION.\n       P1.\n           IF FLAG = 1\n               " + bare + "\n           END-IF.\n"
            : "       PR SECTION.\n       P1.\n           " + inner + "\n";
        string text = Prologue + Outer(outer) + body;
        string name = "t" + Guid.NewGuid().ToString("N")[..6] + ".cob";
        File.WriteAllText(Path.Combine(_dir, name), text);
        int line = text.Split('\n').ToList().FindIndex(l => l.TrimStart().StartsWith(bare, StringComparison.Ordinal)) + 1;
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
            Path.Combine(_dir, name), Path.Combine(_dir, "out.dll"), DialectLevel: edition, CheckOnly: true,
            CopyPaths: [_dir], SourceFormat: InitialReferenceFormat.Fixed, Permissive: permissive));
        return (line, r.Success, [.. r.Errors, .. r.Warnings]);
    }

    private static void AssertAt(IEnumerable<string> lines, int line, int column, string severity, string code)
    {
        var all = lines.ToList();
        Assert.True(all.Any(d => d.Contains($"({line},{column}): {severity} {code}")),
            $"expected '({line},{column}): {severity} {code}'; got:\n" + string.Join("\n", all.DefaultIfEmpty("(none)")));
    }

    // ── §14.9.40.3 SR3 — a file SORT may not appear in an input or output procedure, at EVERY edition ─────────────────

    [Theory]
    [InlineData("sort-input", 85)]
    [InlineData("sort-input", 2002)]
    [InlineData("sort-input", 2014)]
    [InlineData("sort-input", 2023)]
    [InlineData("sort-output", 85)]
    [InlineData("sort-output", 2023)]
    [InlineData("merge-output", 85)]
    [InlineData("merge-output", 2023)]
    public void SortInASortOrMergeProcedure_IsRefusedAtTheStatement(string outer, int edition)
    {
        var (line, ok, lines) = Build(outer, Sort, edition);
        Assert.False(ok);
        AssertAt(lines, line, 12, "error", "COBOLNET2700");
    }

    [Fact] // the statement nested under an IF is positioned at ITS line, not the IF's
    public void SortNestedInAnIf_IsPositionedAtTheNestedStatement()
    {
        var (line, ok, lines) = Build("sort-input", Sort, 2023, nestInIf: true);
        Assert.False(ok);
        AssertAt(lines, line, 16, "error", "COBOLNET2700");
    }

    [Theory] // the legal twin: the same SORT in a section no SORT / MERGE names as its procedure compiles clean
    [InlineData(85)]
    [InlineData(2023)]
    public void SortOutsideASortOrMergeProcedure_IsAccepted(int edition)
    {
        var (_, ok, lines) = Build("none", Sort, edition);
        Assert.True(ok, string.Join("\n", lines));
        Assert.DoesNotContain(lines, d => d.Contains("COBOLNET2700"));
    }

    // ── §14.9.24.3 SR1 — a MERGE there is a COBOL-2023 removal (Annex E.2 item 20) ───────────────────────────────────

    [Theory]
    [InlineData("sort-input")]
    [InlineData("sort-output")]
    [InlineData("merge-output")]
    public void MergeInASortOrMergeProcedure_At2023_IsAnErrorAtTheStatement(string outer)
    {
        var (line, ok, lines) = Build(outer, Merge, 2023);
        Assert.False(ok);
        AssertAt(lines, line, 12, "error", "COBOLNET1572");
    }

    [Fact] // --permissive turns the removal into a positioned warning; the compile succeeds (the migration mode)
    public void MergeInASortProcedure_At2023Permissive_IsAWarningAtTheStatement()
    {
        var (line, ok, lines) = Build("sort-input", Merge, 2023, permissive: true);
        Assert.True(ok, string.Join("\n", lines));
        AssertAt(lines, line, 12, "warning", "COBOLNET1572");
    }

    [Theory] // below 2023 the 2014 standard allowed it: no static diagnostic (EC-SORT-MERGE-ACTIVE is the dynamic net)
    [InlineData(2002)]
    [InlineData(2014)]
    public void MergeInASortProcedure_BelowTheRemoval_IsAccepted(int edition)
    {
        var (_, ok, lines) = Build("sort-input", Merge, edition);
        Assert.DoesNotContain(lines, d => d.Contains("COBOLNET1572"));
        Assert.True(ok, string.Join("\n", lines));
    }

    // ── §14.9.7.3 SR2 / §14.9.36.3 SR2 — COMMIT and ROLLBACK there ───────────────────────────────────────────────────

    [Theory]
    [InlineData("sort-input", Commit)]
    [InlineData("sort-output", Commit)]
    [InlineData("merge-output", Commit)]
    [InlineData("sort-input", Rollback)]
    [InlineData("sort-output", Rollback)]
    [InlineData("merge-output", Rollback)]
    public void CommitOrRollbackInASortOrMergeProcedure_IsRefusedAtTheStatement(string outer, string verb)
    {
        var (line, ok, lines) = Build(outer, verb, 2023);
        Assert.False(ok);
        AssertAt(lines, line, 12, "error", "COBOLNET1690");
    }

    [Fact] // the legal twin: COMMIT in a section no SORT / MERGE names draws no placement diagnostic
    public void CommitOutsideASortOrMergeProcedure_DrawsNoPlacementDiagnostic()
    {
        var (_, _, lines) = Build("none", Commit, 2023);
        Assert.DoesNotContain(lines, d => d.Contains("COBOLNET1690"));
    }

    // ── the sibling arms of the same pass: SET / MOVE statement gates and the USAGE data gate ──────────────────────

    private (bool Ok, List<string> Lines) CompileProgram(string procedureBody, string dataEntries, int edition)
    {
        string text =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1925S.\n       DATA DIVISION.\n"
            + "       WORKING-STORAGE SECTION.\n" + dataEntries
            + "       PROCEDURE DIVISION.\n" + procedureBody + "           STOP RUN.\n";
        string name = "s" + Guid.NewGuid().ToString("N")[..6] + ".cob";
        File.WriteAllText(Path.Combine(_dir, name), text);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
            Path.Combine(_dir, name), Path.Combine(_dir, "out.dll"), DialectLevel: edition, CheckOnly: true,
            CopyPaths: [_dir], SourceFormat: InitialReferenceFormat.Fixed));
        return (r.Success, [.. r.Errors, .. r.Warnings]);
    }

    [Fact] // SET pointer UP BY is a 2002 introduction; the gate is a bound-node gate (§14.9.39 Format 10)
    public void SetPointerUpBy_Below2002_NamesTheStatement()
    {
        var (ok, lines) = CompileProgram("           MOVE 1 TO N.\n           SET P UP BY 1.\n",
            "       01 N PIC 9.\n       01 P USAGE POINTER.\n", 85);
        Assert.False(ok);
        AssertAt(lines, 9, 12, "error", "COBOLNET0900");   // the SET, not the USAGE POINTER entry at line 6 (also refused)
    }

    [Fact] // MOVE SPACE TO a numeric item is a 2023 removal (§14.9.25.3 SR5; Annex E.2 item 1)
    public void MoveFigurativeToNumeric_At2023_NamesTheStatement()
    {
        var (ok, lines) = CompileProgram("           MOVE 1 TO N.\n           MOVE SPACE TO N.\n",
            "       01 N PIC 9.\n", 2023);
        Assert.False(ok);
        AssertAt(lines, 8, 12, "error", "COBOLNET0902");
    }

    [Theory] // §7.3.10.3 SR3 / SR4 run in the pass, after the directive text is gone: each names the directive's own place
    [InlineData("UNDEFINE \"AUTHOR\"", "COBOLNET1623")]       // SR3: AUTHOR is in none of §8.9, §8.10, §8.11
    [InlineData("RESERVE \"DISPLAY\"", "COBOLNET1623")]       // SR4: DISPLAY is a reserved word, not a fresh user word
    public void CobolWordsDirectiveCategoryViolation_NamesTheDirective(string operation, string code)
    {
        string text = "       >>COBOL-WORDS " + operation + "\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1925W.\n"
            + "       PROCEDURE DIVISION.\n           STOP RUN.\n";
        string name = "w" + Guid.NewGuid().ToString("N")[..6] + ".cob";
        File.WriteAllText(Path.Combine(_dir, name), text);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
            Path.Combine(_dir, name), Path.Combine(_dir, "out.dll"), DialectLevel: 2023, CheckOnly: true,
            CopyPaths: [_dir], SourceFormat: InitialReferenceFormat.Fixed));
        Assert.False(r.Success);
        AssertAt([.. r.Errors, .. r.Warnings], 1, 8, "error", code);
    }

    [Fact] // the USAGE gate is positioned at the data entry that declares the item
    public void UsagePointer_Below2002_NamesTheDataEntry()
    {
        var (ok, lines) = CompileProgram("           CONTINUE.\n", "       01 N PIC 9.\n       01 P USAGE POINTER.\n", 85);
        Assert.False(ok);
        AssertAt(lines, 6, 8, "error", "COBOLNET0900");
    }

    // ── the report-SUM arm of the data gates: a SUM counter's own PICTURE gate is positioned at its entry ────────────

    [Theory] // an external-float PICTURE (symbol E) is a 2002 introduction (data-model D21); the SUM-counter gate and, where the
             // entry also prints, the printable-item gate both name the entry, never a bare diagnostic
    [InlineData("           03  COLUMN 1 PIC +9.9E+99 SUM WS-K.\n", 2)]   // printable SUM: the print item and the counter
    [InlineData("           03  PIC +9.9E+99 SUM WS-K.\n", 1)]            // non-printable SUM: the counter alone
    public void ReportSumCounterPictureGate_Below2002_NamesTheSumEntry(string sumEntry, int expectedGates)
    {
        string text =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1925R.\n       ENVIRONMENT DIVISION.\n"
            + "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT PRT ASSIGN TO \"pb1925-prt.tmp\".\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       FD  PRT REPORT IS R-1.\n"
            + "       WORKING-STORAGE SECTION.\n       01  WS-K    PIC 99    VALUE 60.\n"
            + "       REPORT SECTION.\n       RD  R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.\n"
            + "       01  DE-1 TYPE DE LINE PLUS 1.\n           03  COLUMN 1 PIC 99 SOURCE WS-K.\n"
            + "       01  TYPE CF FINAL LINE PLUS 1.\n"
            + (sumEntry.Contains("COLUMN") ? "" : "           03  COLUMN 1 PIC 99 SOURCE WS-K.\n") + sumEntry
            + "       PROCEDURE DIVISION.\n           OPEN OUTPUT PRT\n           INITIATE R-1\n"
            + "           GENERATE DE-1\n           TERMINATE R-1\n           CLOSE PRT\n           STOP RUN.\n";
        string name = "r" + Guid.NewGuid().ToString("N")[..6] + ".cob";
        File.WriteAllText(Path.Combine(_dir, name), text);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
            Path.Combine(_dir, name), Path.Combine(_dir, "out.dll"), DialectLevel: 85, CheckOnly: true,
            CopyPaths: [_dir], SourceFormat: InitialReferenceFormat.Fixed));
        Assert.False(r.Success);
        var gates = r.Errors.Where(d => d.Contains("COBOLNET0900")).ToList();
        Assert.True(gates.Count >= expectedGates, "expected the PICTURE gate(s); got:\n" + string.Join("\n", r.Errors));
        int sumLine = text[..text.IndexOf(sumEntry, StringComparison.Ordinal)].Count(c => c == '\n') + 1;   // the line the entry is written on
        Assert.All(gates, d => Assert.Contains($"({sumLine},12): error COBOLNET0900", d));
    }
}
