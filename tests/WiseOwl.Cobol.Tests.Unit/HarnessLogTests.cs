// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// <c>tests/_shared/HarnessLog.cs</c> loses no line (kb/Work PB3194). Its appends were "best-effort": one that met another
/// append on the file was dropped, so <c>ScratchDirectory.Remove</c>'s report of a directory it could not remove vanished
/// when a parallel test class recorded at the same moment (the gate at 8c4eccfe5, run 000005: the RETRY lines of the
/// process-observation tests landed beside it, the SCRATCH-LEFT line did not, and
/// <c>ScratchDirectoryTests.Remove_ReportsWhatItCannotRemove_AndReturns</c> was red). The log's file system must
/// keep that promise too: the Linux gate at c89014b6b wrote the log onto WSL's 9P mount of the Windows tree, where this
/// test's reader failed with ENODATA, so the gate now writes it on the Linux file system (<c>linux-gate.sh</c>'s
/// <c>test_leg</c>).
/// </summary>
public sealed class HarnessLogTests
{
    /// <summary>Many threads append at once, a reader reads beside them, and every line is in the log afterwards.</summary>
    [Fact]
    public async Task ConcurrentAppends_AreAllRecorded_WithAReaderBeside()
    {
        string run = Guid.NewGuid().ToString("N");
        const int writers = 32, each = 25;
        using var done = new CancellationTokenSource();
        var reader = Task.Run(() =>
        {
            while (!done.IsCancellationRequested) _ = HarnessLog.ReadAll();
        });
        Parallel.For(0, writers, new ParallelOptions { MaxDegreeOfParallelism = writers }, w =>
        {
            for (int i = 0; i < each; i++) HarnessLog.Record($"HARNESS-LOG-TEST  {run} {w}.{i}");
        });
        done.Cancel();
        await reader;

        string log = HarnessLog.ReadAll();
        var missing = Enumerable.Range(0, writers)
            .SelectMany(w => Enumerable.Range(0, each).Select(i => $"{run} {w}.{i}"))
            .Where(token => !log.Contains($"HARNESS-LOG-TEST  {token}{Environment.NewLine}", StringComparison.Ordinal))
            .ToList();
        Assert.True(missing.Count == 0, $"{missing.Count} of {writers * each} lines lost, e.g. {string.Join(", ", missing.Take(5))}");
    }
}
