// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// VERSION TEST MATRIX — Phase 0 (docs/VERSION_TEST_MATRIX_DESIGN.md). Tests the compiler as N per-ISO-edition
/// compilers: a (construct × target-edition) matrix whose expected outcome is COMPUTED from each construct's
/// edition metadata (introducedIn / removedIn), per the design's <c>f(case, V)</c>.
///
/// Phase 0 proves the harness end-to-end on what the greenfield supports TODAY:
/// - INTRODUCTION-GATING (both directions): a post-85 construct (DELETE FILE, introduced 2023) is REJECTED below its
///   introducing edition and COMPILES at it — the greenfield already gates this at the grammar (is2023()).
/// - CONTINUITY (INV-1): an existing COBOL-85 NIST program still compiles at every later edition (no removed/
///   word-collision construct in it).
/// Not yet covered (later phases, per the design): removed-construct gating (needs the greenfield EditionValidator —
/// e.g. ALTER is a lexer token but not an implemented statement), behaviour-variant gating (INV-3), and the full
/// negative corpus. Those rows are the worklist the matrix drives.
/// </summary>
public sealed class VersionMatrixTests
{
    private static readonly int[] Editions = [85, 2002, 2014, 2023];

    /// <summary>One construct in the matrix: a minimal program and its edition metadata (the catalogue of §4 — inline
    /// for Phase 0; a canonical <c>constructs.json</c> is the Phase-2 source of truth).</summary>
    private sealed record Construct(string Id, string Source, int IntroducedIn, int? RemovedIn = null);

    private static readonly Construct[] Catalogue =
    [
        // Nucleus — edition-invariant: introduced in COBOL-85, never removed → compiles at every edition.
        new("nucleus-move-display", """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NUC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC X(5) VALUE "HI".
            PROCEDURE DIVISION.
            MAIN.
                MOVE "BYE" TO W.
                DISPLAY W.
                STOP RUN.
            """, IntroducedIn: 85),

        // DELETE FILE — introduced in COBOL-2023 (grammar gate is2023()). Rejected at 85/2002/2014; compiles at 2023.
        new("delete-file-2023", """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DF.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "f.dat" ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 R PIC X(10).
            PROCEDURE DIVISION.
            MAIN.
                DELETE FILE F.
                STOP RUN.
            """, IntroducedIn: 2023),
    ];

    /// <summary>The matrix expected-outcome function f(case, V): the construct compiles iff its introducing edition is
    /// reached and it has not been removed by V (design §2).</summary>
    private static bool ExpectCompiles(Construct c, int edition)
        => edition >= c.IntroducedIn && (c.RemovedIn is null || edition < c.RemovedIn);

    public static IEnumerable<object[]> Matrix()
    {
        foreach (var c in Catalogue)
            foreach (int v in Editions)
                yield return [c.Id, v];
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Construct_MatchesEditionExpectation(string constructId, int edition)
    {
        var c = Array.Find(Catalogue, x => x.Id == constructId)!;
        bool expectCompiles = ExpectCompiles(c, edition);
        var (ok, detail) = CompileAt(c.Source, edition);

        if (expectCompiles)
            Assert.True(ok, $"[{constructId}] expected to COMPILE at COBOL-{edition} (introduced {c.IntroducedIn}"
                + $"{(c.RemovedIn is { } r ? $", removed {r}" : "")}) but failed:\n{detail}");
        else
            Assert.False(ok, $"[{constructId}] expected to be REJECTED at COBOL-{edition} (not valid until "
                + $"{c.IntroducedIn}{(c.RemovedIn is { } r ? $" / removed {r}" : "")}) but it compiled.");
    }

    /// <summary>INV-1 (continuity): a COBOL-85 NIST program — green at 85 — must still COMPILE at every later edition,
    /// unless it uses a construct the reference doc marks removed-by-then or a word reserved by then (design §3). These
    /// nucleus programs use neither, so they compile at every edition.</summary>
    [Theory]
    [InlineData("NC101A", 2002)]
    [InlineData("NC101A", 2014)]
    [InlineData("NC101A", 2023)]
    [InlineData("NC211A", 2023)]
    [InlineData("NC136A", 2023)]
    public void Cobol85Program_StillCompilesAtLaterEdition(string testName, int edition)
    {
        string root = RepoRoot();
        string src = Path.Combine(root, "tests", "nist", "programs", testName + ".cob");
        Assert.True(File.Exists(src), $"NIST source not found: {src}");
        var (ok, detail) = CompileNistAt(src, testName, edition);
        Assert.True(ok, $"[INV-1 continuity] {testName} (green at 85) failed to compile at COBOL-{edition}; if this is "
            + $"a genuine removal/reserved-word collision it must trace to a VERSION_CHANGE_REFERENCE row, else it is a "
            + $"regression:\n{detail}");
    }

    private static (bool ok, string detail) CompileAt(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_VM_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "prog.dll"), DialectLevel: edition));
            return (r.Success, r.Success ? "" : $"{r.Status}: {string.Join("\n", r.Errors)}");
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    private static (bool ok, string detail) CompileNistAt(string src, string testName, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_VM_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, testName + ".dll"), NistTestName: testName, DialectLevel: edition));
            return (r.Success, r.Success ? "" : $"{r.Status}: {string.Join("\n", r.Errors)}");
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "nist"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/nist) not found");
    }
}
