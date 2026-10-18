// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Drives real NIST CCVS programs through WiseOwl COBOL end-to-end and compares the produced output to the NIST golden
/// (<c>tests/nist/valid/&lt;TEST&gt;.txt</c>) on the guard's acceptance basis (drop CR, strip per-line trailing spaces,
/// and mask the volatile COMPUTED= operand). The golden is the authoritative oracle — it was validated over the
/// whole 364-program corpus — so a match here proves WiseOwl COBOL runs the program correctly. This is the harness the G5 corpus drive runs through: each NC/SM/IC/…
/// program that goes green becomes a permanent regression test by adding its name here.
/// </summary>
/// <remarks>
/// ⛔ THIS CLASS IS PARTITIONED — see <see cref="TestPartitioning"/> (plan §11 A13). On the battery #41 trx it was
/// the Conformance leg's SECOND pole: 349 theory rows running SERIALLY for 237.9 s, because xUnit 2.9.2 makes each
/// test CLASS one collection. The rows are declared ONCE here and sliced <c>index % <see cref="Partitions"/></c>
/// ways by the concrete <c>NistDifferentialTests_P0 … _P5</c> below. The partitions keep
/// <c>NistDifferentialTests</c> in their names so every existing <c>FullyQualifiedName~NistDifferentialTests</c>
/// filter (CI's shard matrix, plan §0) still selects them unchanged.
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class NistDifferentialTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count, chosen from the MEASURED serial cost: 237.9 s ÷ 6 ≈ 40 s per collection, which
    /// puts this class below the assembly's next-largest (the split version matrix, ~60 s).</summary>
    public const int Partitions = 6;

    // The green∪divergent NIST set now lives in tests/nist/corpus.tsv — the ONE source of truth (folds the former
    // per-program [InlineData] list and chains.tsv; loaded by CorpusManifest). Adding a
    // green program is a manifest row, not a code edit. Per-program provenance (subsystem + ISO §) is in DEVLOG + git
    // history; a divergent row carries its ISO § citation in the corpus.tsv note.
    [PartitionedRowSource(nameof(GreenPrograms))]
    public static IEnumerable<object[]> AllGreenPrograms() => CorpusManifest.GreenData();

    /// <summary>This partition's share of the green∪divergent NIST corpus.</summary>
    public static IEnumerable<object[]> GreenPrograms() =>
        TestPartitioning.SliceRows<TSlot>(AllGreenPrograms(), Partitions);

    [Theory]
    [MemberData(nameof(GreenPrograms))]
    public void NistProgram_MatchesGolden(string testName)
    {
        string goldenPath = TestRepo.Nist("valid", testName + ".txt");
        Assert.True(File.Exists(goldenPath), $"golden not found: {goldenPath}");

        // A row that declares TERMINATES asserts the abnormal termination itself, not the report golden: the golden
        // records a run that continued past a fatal I-O status nothing covers, which Annex A.1 item 103 now ends.
        // ⛔ The files the run left at that end ARE graded (kb/Work PB804; the owner's PB3353 requirement that the
        // abnormal path is covered as fully as the success path): what the program had written and closed, its
        // print file and its console output when the run unit ended are what a regression that loses or corrupts a
        // record on the way out would change, and graded against tests/nist/valid-files/<TEST>/ like any other run's.
        if (CorpusManifest.Rows.First(r => string.Equals(r.Name, testName, StringComparison.OrdinalIgnoreCase)).ExpectedTermination is { } ec)
        {
            var terminated = NistRunner.Run(testName);
            Assert.False(terminated.Ok, $"{testName} is declared to terminate on {ec} but ran clean");
            Assert.Contains("exit non-zero", terminated.Detail);
            Assert.Contains(ec, terminated.Detail);
            AssertWrittenFilesMatch(testName, terminated);
            return;
        }

        var run = NistRunner.Run(testName);
        Assert.True(run.Ok, run.Detail);
        Assert.Equal(NistRunner.Normalize(File.ReadAllText(goldenPath)), run.Report);

        // ⛔ AND EVERYTHING ELSE THE PROGRAM WROTE (kb/Work PB804): the report is one file of the run. RW104A's
        // report-writer output, a SORT/MERGE or file-I/O program's data files and the console output beside a print
        // file are graded against tests/nist/valid-files/<TEST>/ — see NistOutputs.
        AssertWrittenFilesMatch(testName, run);
    }

    private static void AssertWrittenFilesMatch(string testName, NistRunResult run)
    {
        if (NistOutputs.UpdateRequested) NistOutputs.Capture(NistOutputs.GoldenDir(testName), run.Written);
        var problems = NistOutputs.Grade(run.Written, NistOutputs.GoldenDir(testName));
        Assert.True(problems.Count == 0,
            $"{testName} wrote files that do not match tests/nist/valid-files/{testName}/:{Environment.NewLine}  "
            + string.Join(Environment.NewLine + "  ", problems));
    }

    // Producer→consumer chains come from CorpusManifest.Chains (folded into tests/nist/corpus.tsv from the former
    // chains.tsv). A chain consumer's first op on the shared TF### file is OPEN INPUT/I-O of a file its predecessors
    // create, so NistRunner compiles and runs the predecessors (in order) inside the consumer's OWN isolated directory
    // first — deterministic start-clean, zero cross-test coupling under xunit parallelism.
}

// ⛔ THE SIX PARTITIONS — each its own xUnit collection. TestPartitionAudit proves they cover the corpus exactly
// once; adding or removing one without changing NistDifferentialTestsBase<>.Partitions is a RED, not a silent
// drop of NIST programs from the regression net.

/// <summary>NIST golden partition 0 of <see cref="NistDifferentialTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class NistDifferentialTests_P0 : NistDifferentialTestsBase<Slot0>;

/// <summary>NIST golden partition 1.</summary>
public sealed class NistDifferentialTests_P1 : NistDifferentialTestsBase<Slot1>;

/// <summary>NIST golden partition 2.</summary>
public sealed class NistDifferentialTests_P2 : NistDifferentialTestsBase<Slot2>;

/// <summary>NIST golden partition 3.</summary>
public sealed class NistDifferentialTests_P3 : NistDifferentialTestsBase<Slot3>;

/// <summary>NIST golden partition 4.</summary>
public sealed class NistDifferentialTests_P4 : NistDifferentialTestsBase<Slot4>;

/// <summary>NIST golden partition 5.</summary>
public sealed class NistDifferentialTests_P5 : NistDifferentialTestsBase<Slot5>;
