// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Collections.Concurrent;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Regression guard for compiler thread-safety. The integration suite runs xUnit test classes in parallel
/// (MaxParallelThreads = CPU count, no opt-out), so multiple <see cref="Compilation.Compile"/> calls overlap
/// IN-PROCESS. Any static mutable field in a compile pass is then a data race: one compilation clobbers
/// another's state mid-walk.
///
/// The historical symptom was a spurious <c>CBL1603</c> ("START KEY operand not a record key of file") that
/// surfaced ONLY under parallel load on the Linux CI runner — <c>BoundTreeValidator._model</c>, a static field
/// consulted by <c>ValidateStart</c>'s <c>ResolveKeyOfReference</c>, was clobbered by a concurrent compile (or
/// nulled by its <c>finally</c>) so a valid START's record key failed to resolve and a phantom compile error was
/// reported. Because the validator is validation-only, a clobber can ONLY produce a spurious diagnostic →
/// compile failure (never wrong runtime output). This is the recurring CI flake behind
/// <c>FileIO_Start_PositionsForReadNext</c> and <c>ReadPrevious_AfterStartEqual_ReturnsTheEqualKey</c> — both
/// START-with-KEY programs, the one path whose compile success depends on that static field (DEVLOG 512).
///
/// This test forces the race deterministically: many threads compile several DISTINCT START-with-KEY programs in
/// tight loops; every compile must succeed with zero diagnostics. With the static field it fails intermittently;
/// once the validator holds its model per-instance it is rock solid across thousands of iterations.
/// </summary>
public sealed class ConcurrentCompilationTests
{
    // Three DISTINCT indexed-file START-with-KEY programs. Distinctness matters: a clobbered _model must point at
    // a DIFFERENT program's SemanticModel for the cross-model race to bite (a single shared program would only
    // exercise the finally-null path, since ResolveKeyOfReference could still succeed against an identical model).
    private static readonly string[] Programs =
    [
        StartProgram("RACER1", "ka.dat", "KA-KEY",        2,  8),
        StartProgram("RACER2", "kb.dat", "KB-LONGER-KEY", 4, 12),
        StartProgram("RACER3", "kc.dat", "KC-K",          3,  6),
    ];

    private static string StartProgram(string id, string file, string key, int keyDigits, int fillerLen) =>
        "       IDENTIFICATION DIVISION.\n" +
        $"       PROGRAM-ID. {id}.\n" +
        "       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n" +
        "       FILE-CONTROL.\n" +
        $"           SELECT F ASSIGN TO \"{file}\"\n" +
        "               ORGANIZATION IS INDEXED\n" +
        "               ACCESS MODE IS DYNAMIC\n" +
        $"               RECORD KEY IS {key}\n" +
        "               FILE STATUS IS WS-ST.\n" +
        "       DATA DIVISION.\n" +
        "       FILE SECTION.\n" +
        "       FD F.\n" +
        "       01 F-REC.\n" +
        $"          05 {key} PIC 9({keyDigits}).\n" +
        $"          05 F-FILLER PIC X({fillerLen}).\n" +
        "       WORKING-STORAGE SECTION.\n" +
        "       01 WS-ST PIC XX.\n" +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN.\n" +
        $"           MOVE 1 TO {key}.\n" +
        $"           START F KEY IS EQUAL TO {key}\n" +
        "               INVALID KEY DISPLAY \"BAD\"\n" +
        "           END-START.\n" +
        "           STOP RUN.\n";

    [Fact]
    public void ParallelCompiles_OfStartKeyPrograms_NeverSpuriouslyFail()
    {
        int threads = Math.Max(16, Environment.ProcessorCount * 2);
        const int iterationsPerThread = 40;
        var failures = new ConcurrentBag<string>();

        string root = Path.Combine(Path.GetTempPath(), "CobolSharp_Race_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        try
        {
            // Pre-write the three sources once; every compile re-reads them (read-only, race-free).
            var sourcePaths = new string[Programs.Length];
            for (int p = 0; p < Programs.Length; p++)
            {
                sourcePaths[p] = Path.Combine(root, $"s{p}.cob");
                File.WriteAllText(sourcePaths[p], Programs[p]);
            }

            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                // Each thread writes to its own output dir (the .dll + sidecars are overwritten each iteration),
                // so the only shared state under contention is the compiler itself — exactly what we are testing.
                string outDir = Path.Combine(root, $"t{t}");
                Directory.CreateDirectory(outDir);
                string outputPath = Path.Combine(outDir, "p.dll");

                for (int i = 0; i < iterationsPerThread; i++)
                {
                    string sourcePath = sourcePaths[(t + i) % Programs.Length];
                    var result = new Compilation().Compile(sourcePath, outputPath);
                    if (!result.Success)
                        failures.Add($"t{t} i{i} ({Path.GetFileName(sourcePath)}): "
                            + string.Join(" | ", result.Diagnostics.Select(d => d.ToString())));
                }
            });
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best-effort */ }
        }

        Assert.True(failures.IsEmpty,
            $"{failures.Count} of {threads * iterationsPerThread} parallel compiles of valid START-with-KEY "
            + "programs failed — a compiler thread-safety race (a static mutable field in a compile pass). "
            + "Examples:\n" + string.Join("\n", failures.Take(5)));
    }
}
