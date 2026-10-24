// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Tests.Shared;

/// <summary>
/// THE test harness's observation log: one line per event a test run cannot show as a verdict but a reader must be able
/// to count afterwards (a retried or unobserved child process, <c>ProcessObserver</c>; a scratch directory that
/// could not be removed, <c>ScratchDirectory</c>). The local gate (<c>scripts/run_gate_legs.py</c>) and every CI test
/// step set <c>COBOLNET_HARNESS_LOG</c> to a file in their results directory, which CI uploads with the TRX (kb/Work
/// PB3108; <c>CiHarnessLogDriftTests</c>). The Linux gate (<c>scripts/linux-gate.sh</c>) sets it to a file on the Linux
/// filesystem and copies that into its results directory as it ends: the log is read beside its writers
/// (<see cref="ReadAll"/>), and on a Windows drive mounted over 9P (WSL's <c>/mnt/e</c>) an append beside a reader lost
/// lines and the read failed with ENODATA (kb/Work PB3194).
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

    /// <summary>How many times one append tries before it reports the line lost. The pauses between tries double from 5 ms
    /// to 100 ms, so another writer's hold is waited for about five seconds in all: a count of tries, never a clock
    /// reading (kb/Work PB1590).</summary>
    private const int AppendAttempts = 60;

    /// <summary>Serializes this host's appends: xUnit runs test classes in parallel, and each append holds the file
    /// alone.</summary>
    private static readonly Lock Appending = new();

    /// <summary>Append one line, and LOSE NONE (kb/Work PB3194): a test reads it back (<c>ScratchDirectoryTests</c>) and a
    /// reader counts it. This host's appends take turns under <see cref="Appending"/>; another process holding the file
    /// (a second test host, a reader) is waited for, up to <see cref="AppendAttempts"/> tries. It never throws, since an
    /// instrument must not fail the test it watches: a line it still cannot write goes to standard error, marked lost,
    /// never into silence.</summary>
    public static void Record(string line)
    {
        string entry = $"{DateTime.UtcNow:O}\t{Environment.ProcessId}\t{line}{Environment.NewLine}";
        Exception? last = null;
        lock (Appending)
        {
            for (int attempt = 1; ; attempt++)
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
                    if (attempt == AppendAttempts) break;
                    Thread.Sleep(Math.Min(5 << Math.Min(attempt - 1, 5), 100));
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
