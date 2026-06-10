// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Drives real NIST CCVS programs through COBOL.NET end-to-end and compares the produced output to the NIST golden
/// (<c>tests/nist/valid/&lt;TEST&gt;.txt</c>) on the guard's acceptance basis (drop CR, strip per-line trailing spaces,
/// and mask the volatile COMPUTED= operand). The golden is the authoritative oracle — it was validated against the
/// legacy byte engine over the whole 364-program corpus — so a match here proves COBOL.NET runs the program correctly,
/// not merely that it agrees with the legacy. This is the harness the G5 corpus drive runs through: each NC/SM/IC/…
/// program that goes green becomes a permanent regression test by adding its name here.
/// </summary>
public sealed class NistDifferentialTests
{
    [Theory]
    [InlineData("NC101A")]   // the first full NC program: MULTIPLY/DIVIDE + the CCVS print-file report
    [InlineData("NC110M")]
    [InlineData("NC111A")]
    [InlineData("NC112A")]
    [InlineData("NC113M")]
    [InlineData("NC127A")]
    [InlineData("NC136A")]
    [InlineData("NC211A")]   // the first NC conditional program: abbreviated/compound conditions, OCCURS-group image,
                             // signed→alphanumeric de-sign, ALL "literal", IS NUMERIC over alphanumeric (DEVLOG 506–511)
    // Additional nucleus programs that already byte-match the golden — located by the compile/run/diff corpus sweep
    // and locked in as permanent regressions (DEVLOG 513). They exercise the MOVE / PERFORM / GO TO / ADD /
    // SUBTRACT / IF surface already built for the eight above; no new feature was needed to green them.
    [InlineData("NC118A")]   // nucleus arithmetic + data movement (MOVE / PERFORM / ADD)
    [InlineData("NC119A")]   // nucleus arithmetic (MOVE / SUBTRACT / ADD)
    [InlineData("NC177A")]   // nucleus arithmetic, ADD/MOVE heavy
    [InlineData("NC205A")]   // nucleus conditional + data movement
    // Greened by the PERFORM-range control fix (DEVLOG 514): PERFORM proc-1 THRU proc-2 N TIMES now iterates the
    // range N times (§14.9.28 GR9) instead of once, so the COMP ON SIZE ERROR drain-loops reach overflow.
    [InlineData("NC106A")]   // SUBTRACT + COMP ON SIZE ERROR, driven by a PERFORM THRU … TIMES loop
    [InlineData("NC176A")]   // ADD + COMP ON SIZE ERROR, driven by a PERFORM THRU … TIMES loop
    [InlineData("NC134A")]   // nucleus arithmetic exercising PERFORM THRU … TIMES ranges
    // Greened by group-level SIGN clause inheritance (ISO §13.18.52 GR1–3, DEVLOG 525): a group SIGN applies to
    // every subordinate signed numeric DISPLAY item, nearest enclosing clause wins (GF-17's SIGN LEADING SEPARATE
    // group overrides the 01's SIGN TRAILING; the separate '+' is readable through a REDEFINES view).
    [InlineData("NC116A")]   // SIGN clause precedence (GF-16/17/18) + signed data movement
    // Greened by the SET statement + index machinery (ISO §14.9.39 Formats 1-2 + §13.18.60 USAGE INDEX, DEVLOG 526):
    // SET index/index-item/numeric TO …, SET UP/DOWN BY, index-names in relations and subscripts.
    [InlineData("NC121M")]   // table handling via SET + index subscripting
    [InlineData("NC123A")]   // SET + GO TO DEPENDING over table paragraphs
    [InlineData("NC131A")]   // SET across index-names, USAGE INDEX items (incl. a USAGE INDEX group), numeric receivers
    [InlineData("NC137A")]   // SET + relative index subscripting
    [InlineData("NC141A")]   // SET + multi-dimensional table indexing
    [InlineData("NC248A")]   // SET + table relation conditions
    public void NistProgram_MatchesGolden(string testName)
    {
        string root = RepoRoot();
        string goldenPath = Path.Combine(root, "tests", "nist", "valid", testName + ".txt");
        Assert.True(File.Exists(goldenPath), $"golden not found: {goldenPath}");

        var (ok, output, detail) = RunNist(root, testName);
        Assert.True(ok, detail);
        Assert.Equal(Normalize(File.ReadAllText(goldenPath)), output);
    }

    /// <summary>Compile a NIST program (with CCVS X-card preprocessing) and run it in an isolated temp directory,
    /// returning the program's output read from its print file (the CCVS report) — or stdout for a DISPLAY-only
    /// program — normalized to the NIST acceptance basis.</summary>
    private static (bool ok, string output, string detail) RunNist(string root, string testName)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Nist_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(root, "tests", "nist", "programs", testName + ".cob");
            if (!File.Exists(src)) return (false, "", $"source not found: {src}");
            string dll = Path.Combine(dir, testName + ".dll");

            var result = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, NistTestName: testName, DialectLevel: 85));
            if (!result.Success)
                return (false, "", $"[compile] {result.Status}: {string.Join("\n", result.Errors)}");

            var (runOk, stdout, runDetail) = CutRunner.Run(dll, dir);
            if (!runOk) return (false, "", $"[run] exit non-zero: {runDetail}");

            // The CCVS report lands in the print file (assign target → <lowercased>.txt in the run dir); a
            // DISPLAY-only program produces no print file and is read from stdout — exactly the guard's discovery order.
            string printFile = Path.Combine(dir, testName.ToLowerInvariant() + ".txt");
            string raw = File.Exists(printFile) ? File.ReadAllText(printFile)
                : Directory.EnumerateFiles(dir, "*.txt").FirstOrDefault() is { } any ? File.ReadAllText(any)
                : stdout;
            return (true, Normalize(raw), runDetail);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The NIST acceptance basis (exactly <c>scripts/guard.sh</c>'s <c>normalize()</c>): drop CR, strip
    /// per-line trailing spaces, and mask the COMPUTED= operand (a value some CCVS programs print that is not part of
    /// the pass/fail decision). Applied identically to the golden and the produced output.</summary>
    private static string Normalize(string s)
    {
        var lines = s.ReplaceLineEndings("\n").Split('\n')
            .Select(line => System.Text.RegularExpressions.Regex.Replace(line.TrimEnd(' '), "COMPUTED=  [0-9]*", "COMPUTED=  XXXXXXXXX"));
        return string.Join("\n", lines).TrimEnd('\n');
    }

    /// <summary>Walk up from the test assembly to the repository root (the directory holding <c>tests/nist</c>).</summary>
    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "nist"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/nist) not found");
    }
}
