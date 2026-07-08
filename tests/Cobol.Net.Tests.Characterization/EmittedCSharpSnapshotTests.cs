// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Characterization;

/// <summary>
/// Gate 3 (rearchitecture Phase 0): snapshot the GENERATED C# for every positive corpus program — advisory/reviewed
/// proof that a pure-reorg refactor left emission byte-identical. An intentional emit change re-baselines with review
/// (while gate 1 proves it behavior-preserving). Negative programs emit no C# and are covered by gate 2.
/// Seed/re-baseline with <c>COBOLNET_UPDATE_SNAPSHOTS=1</c> (local only).
/// </summary>
public sealed class EmittedCSharpSnapshotTests
{
    public static IEnumerable<object[]> Positive() => CharacterizationCorpus.Positive();

    [Theory]
    [MemberData(nameof(Positive))]
    public void EmittedCSharp_MatchesSnapshot(string name, int edition)
    {
        string path = CharacterizationCorpus.SourcePath(name, negative: false);
        var probe = CompilerProbe.Probe(path, edition, emit: true);
        Assert.True(probe.Ok, $"positive characterization program '{name}' must compile: "
            + string.Join("; ", probe.Diagnostics));
        Assert.NotNull(probe.EmittedCSharp);
        string file = System.IO.Path.Combine(CharacterizationCorpus.SnapshotDir, $"{name}.{edition}.g.cs.txt");
        Snapshot.Assert(file, probe.EmittedCSharp!);
    }
}
