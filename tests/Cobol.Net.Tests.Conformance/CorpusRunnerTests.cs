// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The per-edition corpus DISCOVERY RUNNERS (roadmap Phase-1 shells; VERSION_TEST_MATRIX_DESIGN §5): each
/// edition directory under <c>tests/conformance/</c> carries a <c>manifest.json</c> — ENABLED programs compile
/// at that <c>--std</c> strict (and, given a sibling <c>.out</c>, run and byte-compare); PENDING programs are
/// catalogued but not asserted (the mass-red guard: most of the 2002 corpus exercises features the greenfield
/// has not implemented yet — the wave that lands a feature flips its programs to enabled). The NEGATIVE corpus
/// (<c>tests/conformance/negative/</c>) inverts the contract: enabled entries MUST fail with their
/// <c>.err</c>-file diagnostic at each edition their manifest entry names. The integrity facts make silent
/// non-discovery impossible: every on-disk program must be listed.
/// </summary>
public sealed class CorpusRunnerTests
{
    private sealed record Manifest(IReadOnlyList<string> Enabled, IReadOnlyList<string> Pending);

    private static readonly string Root = Path.Combine(EditionHarness.RepoRoot(), "tests", "conformance");
    private static readonly string[] EditionDirs = ["2002", "2014", "2023"];

    private static Manifest Load(string dir)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json")));
        static IReadOnlyList<string> Arr(JsonElement root, string name) =>
            [.. root.GetProperty(name).EnumerateArray().Select(e => e.GetString()!)];
        return new Manifest(Arr(doc.RootElement, "enabled"), Arr(doc.RootElement, "pending"));
    }

    /// <summary>Every on-disk .cob is manifest-listed (enabled ⊕ pending) — nothing silently undiscovered.</summary>
    [Theory]
    [InlineData("2002")]
    [InlineData("2014")]
    [InlineData("2023")]
    [InlineData("negative")]
    public void Manifest_CoversEveryProgram_NoOverlap(string edition)
    {
        string dir = Path.Combine(Root, edition);
        var m = Load(dir);
        var onDisk = Directory.EnumerateFiles(dir, "*.cob").Select(Path.GetFileNameWithoutExtension).ToHashSet();
        var listed = m.Enabled.Concat(m.Pending).ToHashSet();
        var unlisted = onDisk.Except(listed).Order().Take(5).ToList();
        var phantom = listed.Except(onDisk).Order().Take(5).ToList();
        Assert.True(unlisted.Count == 0, $"{edition}: unlisted programs (add to the manifest): {string.Join(", ", unlisted)}");
        Assert.True(phantom.Count == 0, $"{edition}: manifest lists missing programs: {string.Join(", ", phantom)}");
        Assert.Empty(m.Enabled.Intersect(m.Pending));
    }

    public static IEnumerable<object[]> EnabledPositive()
    {
        foreach (string ed in EditionDirs)
            foreach (string name in Load(Path.Combine(Root, ed)).Enabled)
                yield return [ed, name];
        // xunit needs ≥1 row per theory; a sentinel keeps the theory alive while all entries are pending.
        yield return ["shell", "sentinel"];
    }

    [Theory]
    [MemberData(nameof(EnabledPositive))]
    public void EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(string edition, string name)
    {
        if (edition == "shell") return;   // the empty-manifest sentinel
        string dir = Path.Combine(Root, edition);
        string src = Path.Combine(dir, name + ".cob");
        var (ok, errors, _) = EditionHarness.CompileFull(File.ReadAllText(src), int.Parse(edition));
        Assert.True(ok, $"[{edition}/{name}] must compile strict: {string.Join("\n", errors)}");
        // Output comparison (sibling .out) upgrades here when the first wave enables a run-bearing program —
        // the shell asserts compilation; the enabling wave owns the run contract (roadmap Phase 3+).
    }

    public static IEnumerable<object[]> EnabledNegative()
    {
        foreach (string name in Load(Path.Combine(Root, "negative")).Enabled)
            yield return [name];
        yield return ["sentinel"];
    }

    [Theory]
    [MemberData(nameof(EnabledNegative))]
    public void EnabledNegativeCase_RejectsWithItsDiagnostic(string name)
    {
        if (name == "sentinel") return;
        string dir = Path.Combine(Root, "negative");
        string expected = File.ReadAllText(Path.Combine(dir, name + ".err")).Trim();
        // Convention: <name>.cob's first line is a comment naming the editions: *> reject-at: 2002 2014 2023
        string src = File.ReadAllText(Path.Combine(dir, name + ".cob"));
        var editions = src.Split('\n')[0].Contains("reject-at:")
            ? src.Split('\n')[0].Split("reject-at:")[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
            : throw new InvalidOperationException($"{name}.cob missing the '*> reject-at:' header");
        foreach (int ed in editions)
        {
            var (ok, errors, _) = EditionHarness.CompileFull(src, ed);
            Assert.False(ok, $"[negative/{name}] must be REJECTED at --std {ed}");
            EditionHarness.AssertHasDiagnostic(errors, expected);
        }
    }
}
