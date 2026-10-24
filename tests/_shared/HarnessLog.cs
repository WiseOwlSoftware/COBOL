// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Tests.Shared;

/// <summary>
/// THE test harness's observation log: one line per event a test run cannot show as a verdict but a reader must be able
/// to count afterwards (a retried or unobserved child process, <c>ProcessObserver</c>; a scratch directory that
/// could not be removed, <c>ScratchDirectory</c>). The local gate (<c>scripts/run_gate_legs.py</c>), the Linux
/// gate (<c>scripts/linux-gate.sh</c>) and every CI test step set <c>COBOLNET_HARNESS_LOG</c> to a file in their results
/// directory, which CI uploads with the TRX (kb/Work PB3108; <c>CiHarnessLogDriftTests</c>).
/// </summary>
internal static class HarnessLog
{
    /// <summary>The log file: <c>COBOLNET_HARNESS_LOG</c>, or one in the temp directory when no gate names one.</summary>
    public static string Path { get; } =
        Environment.GetEnvironmentVariable("COBOLNET_HARNESS_LOG")
        ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cobolnet-harness-observations.log");

    /// <summary>Say, on standard error and in this log, that a test's scratch <paramref name="path"/> could not be deleted
    /// and why, and what becomes of it (<paramref name="then"/>). THE one report of a failed test cleanup
    /// (<c>ScratchDirectory.Remove</c>, <c>FileHarness.TryDelete</c>): a delete failure is never swallowed
    /// (kb/Work PB3194; <c>ScratchDirectoryDriftTests</c>).</summary>
    public static void ReportLeft(string path, Exception failure, string then)
    {
        string line = $"SCRATCH-LEFT  {path} :: {failure.GetType().Name}: {failure.Message} :: {then}";
        Console.Error.WriteLine(line);
        Record(line);
    }

    /// <summary>Append one line. Best-effort and lock-tolerant: this is an instrument, and an instrument that can fail a
    /// test run is worse than one that occasionally drops a line.</summary>
    public static void Record(string line)
    {
        try
        {
            // A gate or CI step names a log in its results directory, which may not exist until the run writes its
            // first result (dotnet test creates --results-directory when it writes the TRX, at the end).
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path))!);
            File.AppendAllText(Path,
                $"{DateTime.UtcNow:O}\t{Environment.ProcessId}\t{line}{Environment.NewLine}", Encoding.UTF8);
        }
        catch (IOException) { /* concurrent append lost: acceptable for an instrument */ }
        catch (UnauthorizedAccessException) { /* same */ }
    }
}
