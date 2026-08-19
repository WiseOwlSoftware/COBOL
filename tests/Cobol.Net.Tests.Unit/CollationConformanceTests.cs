// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.Collation;
using CobolNet.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The Unicode CLDR root-collation CONFORMANCE tests (cldr/common/uca/CollationTest_CLDR_*_SHORT.txt, release-48-2,
/// UCA 17.0.0) run against the derived table + engine: every line of the file is a string, and the file is in sorted
/// order under the named variable weighting with ties broken by NFD code point order — so the engine at
/// <see cref="CollationStrength.Identical"/> must find every consecutive pair non-decreasing.
/// <list type="bullet">
/// <item>Always: the committed 1-in-25 samples under TestData/collation/ (any subsequence of a sorted list is sorted).</item>
/// <item>Opt-in: the FULL files, when <c>COBOLNET_UCA_CONFORMANCE_DIR</c> names the directory holding them
/// (they are ~2.3 MB each and are not committed).</item>
/// </list>
/// Lines whose code points spell an unpaired-surrogate PAIR that UTF-16 would fuse into one supplementary character
/// cannot be represented as the file intends and are skipped (counted, reported).
/// </summary>
public sealed class CollationConformanceTests(ITestOutputHelper output)
{
    private static readonly string SampleDir = Path.Combine(TestRepo.Root, "tests", "Cobol.Net.Tests.Unit", "TestData", "collation");

    [Theory]
    [InlineData("CollationTest_CLDR_NON_IGNORABLE_SAMPLE.txt", AlternateHandling.NonIgnorable)]
    [InlineData("CollationTest_CLDR_SHIFTED_SAMPLE.txt", AlternateHandling.Shifted)]
    public void Sample_IsInSortedOrder(string file, AlternateHandling alternate) =>
        AssertSorted(Path.Combine(SampleDir, file), alternate, minimumLines: 8000);

    [Theory]
    [InlineData("CollationTest_CLDR_NON_IGNORABLE_SHORT.txt", AlternateHandling.NonIgnorable)]
    [InlineData("CollationTest_CLDR_SHIFTED_SHORT.txt", AlternateHandling.Shifted)]
    public void FullFile_IsInSortedOrder_WhenAvailable(string file, AlternateHandling alternate)
    {
        string? dir = Environment.GetEnvironmentVariable("COBOLNET_UCA_CONFORMANCE_DIR");
        if (string.IsNullOrEmpty(dir)) { output.WriteLine("COBOLNET_UCA_CONFORMANCE_DIR not set — full-file run skipped"); return; }
        string path = Path.Combine(dir, file);
        Assert.True(File.Exists(path), $"{path} not found");
        AssertSorted(path, alternate, minimumLines: 200_000);
    }

    private void AssertSorted(string path, AlternateHandling alternate, int minimumLines)
    {
        var collator = new Collator(CollationTable.Root, CollationStrength.Identical, alternate);
        string? prev = null, prevLine = null;
        int lines = 0, skipped = 0, violations = 0;
        var report = new StringBuilder();
        foreach (string raw in File.ReadLines(path, Encoding.UTF8))
        {
            if (raw.Length == 0 || raw[0] == '#') continue;
            if (!TryDecode(raw, out string s)) { skipped++; continue; }
            lines++;
            if (prev is not null && collator.Compare(prev, s) > 0)
            {
                violations++;
                if (violations <= 25)
                    report.Append($"line {lines}: [{prevLine}] > [{raw}]  keys {collator.GetKey(prev)}  ||  {collator.GetKey(s)}\n");
            }
            prev = s;
            prevLine = raw;
        }
        output.WriteLine($"{Path.GetFileName(path)}: {lines} lines checked, {skipped} skipped (surrogate pairs), {violations} violation(s)");
        Assert.True(lines >= minimumLines, $"only {lines} lines checked in {path} — the population is not what the test expects");
        Assert.True(violations == 0, $"{violations} ordering violation(s) in {Path.GetFileName(path)} (first 25):\n{report}");
    }

    /// <summary>"0061 0301 …" \U00002192 the string. False when the sequence contains a high surrogate code point immediately
    /// followed by a low one (UTF-16 cannot keep them apart).</summary>
    private static bool TryDecode(string line, out string s)
    {
        var sb = new StringBuilder();
        int prevCp = -1;
        foreach (string tok in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int cp = Convert.ToInt32(tok, 16);
            if (prevCp is >= 0xD800 and <= 0xDBFF && cp is >= 0xDC00 and <= 0xDFFF) { s = ""; return false; }
            if (cp is >= 0xD800 and <= 0xDFFF) sb.Append((char)cp);
            else sb.Append(char.ConvertFromUtf32(cp));
            prevCp = cp;
        }
        s = sb.ToString();
        return true;
    }
}
