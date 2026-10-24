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

    /// <summary>How long one append waits, in all, for another writer to let go of the log.</summary>
    private static readonly TimeSpan AppendPatience = TimeSpan.FromSeconds(5);

    /// <summary>Serializes this host's appends: xUnit runs test classes in parallel, and each append holds the file
    /// alone.</summary>
    private static readonly Lock Appending = new();

    /// <summary>Append one line, and LOSE NONE (kb/Work PB3194): a test reads it back (<c>ScratchDirectoryTests</c>) and a
    /// reader counts it. This host's appends take turns under <see cref="Appending"/>; another process holding the file
    /// (a second test host, a reader) is waited for, up to <see cref="AppendPatience"/>. It never throws, since an
    /// instrument must not fail the test it watches: a line it still cannot write goes to standard error, marked lost,
    /// never into silence.</summary>
    public static void Record(string line)
    {
        string entry = $"{DateTime.UtcNow:O}\t{Environment.ProcessId}\t{line}{Environment.NewLine}";
        Exception? last = null;
        lock (Appending)
        {
            var waited = System.Diagnostics.Stopwatch.StartNew();
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    // A gate or CI step names a log in its results directory, which may not exist until the run writes its
                    // first result (dotnet test creates --results-directory when it writes the TRX, at the end).
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path))!);
                    using var log = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.Read | FileShare.Delete);
                    using var text = new StreamWriter(log, Encoding.UTF8);
                    text.Write(entry);
                    return;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    last = e;
                    if (waited.Elapsed >= AppendPatience) break;
                    Thread.Sleep(Math.Min(5 << Math.Min(attempt, 5), 100));
                }
            }
        }
        Console.Error.WriteLine($"HARNESS-LOG-LOST  {Path} :: {last!.GetType().Name}: {last.Message} :: {line}");
    }

    /// <summary>The log's text so far, read beside its writers (they share it for reading, and this reader shares it for
    /// writing, so neither refuses the other); empty when nothing has been recorded.</summary>
    public static string ReadAll()
    {
        if (!File.Exists(Path)) return "";
        using var log = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var text = new StreamReader(log, Encoding.UTF8);
        return text.ReadToEnd();
    }
}
