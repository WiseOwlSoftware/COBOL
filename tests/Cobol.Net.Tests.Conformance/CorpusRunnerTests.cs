// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The per-edition conformance corpus manifests (roadmap Phase-1 shells; VERSION_TEST_MATRIX_DESIGN §5) — the DATA
/// half of the corpus runners, held in ONE non-generic type so the three partitions of
/// <see cref="CorpusRunnerTestsBase{TSlot}"/> share a single read of each <c>manifest.json</c>.
/// </summary>
internal static class ConformanceCorpus
{
    internal sealed record Manifest(IReadOnlyList<string> Enabled, IReadOnlyList<string> Pending);

    internal static string Root { get; } = TestRepo.Tests("conformance");

    // 85 carries the X3.23-1985-only goldens (the USE FOR DEBUGGING / DEBUG-ITEM facility, VCR 7.17 — a REMOVAL
    // gate whose ACCEPT edition is 85); 2002/2014/2023 carry the post-85 introductions.
    internal static string[] EditionDirs { get; } = ["85", "2002", "2014", "2023"];

    internal static Manifest Load(string dir)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json")));
        static IReadOnlyList<string> Arr(JsonElement root, string name) =>
            [.. root.GetProperty(name).EnumerateArray().Select(e => e.GetString()!)];
        return new Manifest(Arr(doc.RootElement, "enabled"), Arr(doc.RootElement, "pending"));
    }

    internal static IEnumerable<object[]> EnabledPositive()
    {
        foreach (string ed in EditionDirs)
        {
            foreach (string name in Load(Path.Combine(Root, ed)).Enabled)
            {
                yield return [ed, name];
            }
        }

        // xunit needs ≥1 row per theory; a sentinel keeps the theory alive while all entries are pending.
        yield return ["shell", "sentinel"];
    }

    internal static IEnumerable<object[]> EnabledNegative()
    {
        foreach (string name in Load(Path.Combine(Root, "negative")).Enabled)
        {
            yield return [name];
        }

        yield return ["sentinel"];
    }
}

/// <summary>
/// The per-edition corpus DISCOVERY RUNNERS: each edition directory under <c>tests/conformance/</c> carries a
/// <c>manifest.json</c> — ENABLED programs compile at that <c>--std</c> strict (and, given a sibling <c>.out</c>,
/// run and byte-compare); PENDING programs are catalogued but not asserted (the mass-red guard: most of the 2002
/// corpus exercises features the greenfield has not implemented yet — the wave that lands a feature flips its
/// programs to enabled). The NEGATIVE corpus (<c>tests/conformance/negative/</c>) inverts the contract: enabled
/// entries MUST fail with their <c>.err</c>-file diagnostic at each edition their manifest entry names.
/// </summary>
/// <remarks>
/// ⛔ THIS CLASS IS PARTITIONED — see <see cref="TestPartitioning"/> (plan §11 A13). It was the Conformance leg's
/// THIRD pole on the battery #41 trx (1,005 rows, 83.9 s serial), and once the version matrix and the NIST
/// differential are split it would BECOME the pole; xUnit 2.9.2 makes each test CLASS one collection. The
/// partitions keep <c>CorpusRunnerTests</c> in their names so every existing
/// <c>FullyQualifiedName~CorpusRunnerTests</c> filter (CI's shard matrix, plan §0) still selects them unchanged.
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class CorpusRunnerTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count, chosen from the MEASURED serial cost: 83.9 s ÷ 3 ≈ 28 s per collection, which
    /// keeps it below the split version matrix (~60 s) rather than becoming the leg's new pole.</summary>
    public const int Partitions = 3;

    [PartitionedRowSource(nameof(EnabledPositive))]
    public static IEnumerable<object[]> AllEnabledPositive() => ConformanceCorpus.EnabledPositive();

    /// <summary>This partition's share of the enabled positive corpus.</summary>
    public static IEnumerable<object[]> EnabledPositive() =>
        TestPartitioning.SliceRows<TSlot>(AllEnabledPositive(), Partitions);

    [Theory]
    [MemberData(nameof(EnabledPositive))]
    public void EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(string edition, string name)
    {
        if (edition == "shell") return;   // the empty-manifest sentinel
        string dir = Path.Combine(ConformanceCorpus.Root, edition);
        string src = Path.Combine(dir, name + ".cob");
        // The RUN CONTRACT (landed with the first enabling wave — the W3 corpus audit, DEVLOG 597): an
        // enabled program with a sibling .out must compile STRICT at its edition, run, and byte-match the
        // expected output (line endings normalized — the .out files are LF; a Windows run emits CRLF).
        string outFile = Path.Combine(dir, name + ".out");
        string tmp = Path.Combine(Path.GetTempPath(), "CobolNet_Corpus_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tmp);
        try
        {
            string dll = Path.Combine(tmp, name + ".dll");
            var r = CobolNet.CompilerDriver.Compile(new CobolNet.CompilerDriver.Options(
                src, dll, DialectLevel: int.Parse(edition)));
            Assert.True(r.Success, $"[{edition}/{name}] must compile strict: {string.Join("\n", r.Errors)}");
            if (!File.Exists(outFile)) return;   // compile-only entry (no expected output recorded)
            var (ran, stdout, detail) = CutRunner.Run(dll, tmp);
            Assert.True(ran, $"[{edition}/{name}] must run: {detail}");
            // The same comparison basis as the NIST differential harness (CutRunner.Normalize — LF line
            // endings, per-line trailing-space trim, no trailing newline).
            Assert.Equal(CutRunner.Normalize(File.ReadAllText(outFile)), CutRunner.Normalize(stdout));
        }
        finally { try { Directory.Delete(tmp, recursive: true); } catch { /* best-effort */ } }
    }

    [PartitionedRowSource(nameof(EnabledNegative))]
    public static IEnumerable<object[]> AllEnabledNegative() => ConformanceCorpus.EnabledNegative();

    /// <summary>This partition's share of the enabled negative corpus.</summary>
    public static IEnumerable<object[]> EnabledNegative() =>
        TestPartitioning.SliceRows<TSlot>(AllEnabledNegative(), Partitions);

    [Theory]
    [MemberData(nameof(EnabledNegative))]
    public void EnabledNegativeCase_RejectsWithItsDiagnostic(string name)
    {
        if (name == "sentinel") return;
        string dir = Path.Combine(ConformanceCorpus.Root, "negative");
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

// ⛔ THE THREE PARTITIONS — each its own xUnit collection. TestPartitionAudit proves they cover both corpora
// exactly once.

/// <summary>Conformance-corpus partition 0 of <see cref="CorpusRunnerTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class CorpusRunnerTests_P0 : CorpusRunnerTestsBase<Slot0>;

/// <summary>Conformance-corpus partition 1.</summary>
public sealed class CorpusRunnerTests_P1 : CorpusRunnerTestsBase<Slot1>;

/// <summary>Conformance-corpus partition 2.</summary>
public sealed class CorpusRunnerTests_P2 : CorpusRunnerTestsBase<Slot2>;

/// <summary>
/// The corpus INTEGRITY facts — whole-directory assertions that make silent non-discovery impossible: every
/// on-disk program must be listed. They are about a manifest as a WHOLE, so they run ONCE and are deliberately not
/// on the partitioned base (an inherited theory would run three times and assert the same thing thrice).
/// </summary>
public sealed class CorpusRunnerTests
{
    /// <summary>Every on-disk .cob is manifest-listed (enabled ⊕ pending) — nothing silently undiscovered.</summary>
    [Theory]
    [InlineData("85")]
    [InlineData("2002")]
    [InlineData("2014")]
    [InlineData("2023")]
    [InlineData("negative")]
    public void Manifest_CoversEveryProgram_NoOverlap(string edition)
    {
        string dir = Path.Combine(ConformanceCorpus.Root, edition);
        var m = ConformanceCorpus.Load(dir);
        var onDisk = Directory.EnumerateFiles(dir, "*.cob").Select(Path.GetFileNameWithoutExtension).ToHashSet();
        var listed = m.Enabled.Concat(m.Pending).ToHashSet();
        var unlisted = onDisk.Except(listed).Order().Take(5).ToList();
        var phantom = listed.Except(onDisk).Order().Take(5).ToList();
        Assert.True(unlisted.Count == 0, $"{edition}: unlisted programs (add to the manifest): {string.Join(", ", unlisted)}");
        Assert.True(phantom.Count == 0, $"{edition}: manifest lists missing programs: {string.Join(", ", phantom)}");
        Assert.Empty(m.Enabled.Intersect(m.Pending));
    }
}
