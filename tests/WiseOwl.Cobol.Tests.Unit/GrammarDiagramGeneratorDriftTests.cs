// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// "Generator runs clean" check for <c>scripts/gen-grammar-diagrams.ps1</c> — the Obsidian grammar railroad-diagram
/// layer (<c>kb/Grammar/</c>), which renders one syntax diagram per rule for each ANTLR parser fragment via the
/// vendored bottlecaps <c>tools/rr/rr.war</c>. Like <see cref="VaultReferenceGeneratorDriftTests"/>, the output is a
/// GITIGNORED build artifact, so this is NOT a committed-vs-regenerated comparison; it asserts the generator still
/// cleans every <c>.g4</c> fragment to valid EBNF and rr renders it with exit 0 — so a grammar edit that breaks the
/// cleaning pass (or a broken script/missing tool) fails CI rather than silently producing empty/garbled diagrams.
/// Requires <c>pwsh</c> and <c>java</c> on PATH (both already build prerequisites for the ANTLR generation).
/// </summary>
/// <remarks>
/// <para>The run happens under a HOSTILE temp directory (kb/Work PB5447): this test points the generator's temp directory
/// (<c>TMP</c>/<c>TEMP</c> on Windows, <c>TMPDIR</c> on Unix; <see cref="PointTempAt"/>) at a directory of its own and,
/// from this process, opens every regular file that appears there for read-write with read-write-delete sharing and
/// holds it to the end of the run — what a real-time scanner or indexer can do to any fresh file in <c>%TEMP%</c>. A
/// generator that hands rr's output back through a file there and re-reads it (<c>File.ReadAllText</c> asks for
/// <c>FileShare.Read</c>, which a held write access refuses on Windows) exits 1 with "being used by another process",
/// however long it retries; the pipe handoff never names a file, so it runs clean. On Unix .NET's sharing is an
/// advisory <c>flock</c> and a shared hold refuses no reader, so there the hostile half cannot bite and the test proves
/// the clean run and the two witnesses below.</para>
/// <para>⛔ NOTHING THIS TEST ASSERTS DEPENDS ON WHEN THE WATCHER'S EVENTS ARRIVE (kb/Work PB5463). Its first form asserted
/// <c>held &gt; 0</c>, the count of files the watcher's callbacks had taken when the child exited. Measured on Linux
/// (WSL, the instrumented copy): the first file under the directory is pwsh's own <c>dotnet-diagnostic-*</c> socket,
/// whose open fails with ENXIO, an <see cref="IOException"/> the holder treated as "its writer refuses the share" and
/// retried on the watcher's one event thread for the whole run; inotify read nothing more, the generator's notes were
/// never seen, and the count was the runtime's two <c>clr-debug-pipe-*</c> FIFOs, taken or missed in a race with pwsh's
/// exit. Two merge groups went red on it. So the two facts that make the hostile half non-vacuous are each read for
/// certain: the generator's own summary line names the directory it wrote its notes in, which must lie under the
/// directory this test named (<see cref="GeneratorOutputDirectory"/>); and the holder takes a probe file planted before
/// the child starts, which this test blocks on (<see cref="FreshFileHolder.ProveAsync"/>), so the holder is known live before
/// the generator writes anything.</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class GrammarDiagramGeneratorDriftTests
{
    [Fact]
    public async Task Generator_RunsClean()
    {
        string script = TestRepo.Scripts("gen-grammar-diagrams.ps1");
        Assert.True(File.Exists(script), $"missing generator: {script}");

        string temp = ScratchDirectory.Create("grammar-diagram-held-");
        using var holder = new FreshFileHolder(temp);
        await holder.ProveAsync(); // the holder took a fresh file under temp before the generator starts

        var psi = new ProcessStartInfo("pwsh", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Check")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = TestRepo.Root,
        };
        PointTempAt(psi, temp);

        int exitCode;
        string stdout, stderr;
        using (var p = Process.Start(psi)!)
        {
            Task<string> err = p.StandardError.ReadToEndAsync(); // drained beside stdout, so neither pipe can fill and stall the child
            stdout = await p.StandardOutput.ReadToEndAsync();
            stderr = await err;
            await p.WaitForExitAsync(); // both streams are at EOF; no clock decides this test (kb/Work PB3022)
            exitCode = p.ExitCode;
        }
        HolderReport held = holder.Stop();
        ScratchDirectory.Remove(temp);

        Assert.True(exitCode == 0,
            $"gen-grammar-diagrams.ps1 -Check exited {exitCode} while this test held every fresh file under {temp} ({held})\n" +
            $"STDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        Assert.True(held.Errors.Count == 0,
            $"the holder under {temp} failed (a watcher error or a take that threw), so it may have missed files the generator wrote ({held}):\n" +
            string.Join("\n", held.Errors));
        string? outDir = GeneratorOutputDirectory(stdout);
        Assert.True(outDir is not null, $"no generation summary line in the generator's output\nSTDOUT:\n{stdout}");
        // The generator wrote where this test held files: it reports the directory its -Check run wrote, and that
        // directory is under the temp directory this test named. Without this the hostile half would prove nothing.
        Assert.True(IsUnder(outDir!, temp),
            $"the generator wrote its notes into {outDir}, not under {temp}: it ignored the temp variables ({held})");
    }

    /// <summary>
    /// The directory the generator's summary line names: <c>generated &lt;n&gt; grammar-diagram note(s) into &lt;dir&gt;</c>,
    /// with at least one note. Null when no such line is printed.
    /// </summary>
    private static string? GeneratorOutputDirectory(string stdout)
    {
        Match m = Regex.Match(stdout, @"^generated (?<n>[1-9]\d*) grammar-diagram note\(s\) into (?<dir>.+?)\r?$", RegexOptions.Multiline);
        return m.Success ? m.Groups["dir"].Value : null;
    }

    private static bool IsUnder(string path, string directory)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return Path.GetFullPath(path).StartsWith(root, comparison);
    }

    /// <summary>
    /// Points the child's <see cref="Path.GetTempPath"/> at <paramref name="directory"/> on every platform the gate and
    /// the merge group run. The variables it reads differ by OS: on Windows <c>GetTempPath2</c> reads <c>TMP</c>, then
    /// <c>TEMP</c>; on Unix .NET reads <c>TMPDIR</c> alone and falls back to <c>/tmp/</c>. Setting only the Windows pair
    /// sends a Linux run to <c>/tmp</c>, where no file is held (kb/Work PB5447 review R2-1).
    /// </summary>
    private static void PointTempAt(ProcessStartInfo psi, string directory)
    {
        foreach (string name in new[] { "TMP", "TEMP", "TMPDIR" }) psi.Environment[name] = directory;
    }

    /// <summary>What a <see cref="FreshFileHolder"/> did, for the failure messages: how many files it held and skipped,
    /// and every error its watcher raised or a take threw.</summary>
    private sealed record HolderReport(int Held, int Skipped, IReadOnlyList<string> Errors)
    {
        public override string ToString() => $"held {Held} file(s), skipped {Skipped}, errors {Errors.Count}";
    }

    /// <summary>
    /// Holds every regular file created under a directory, as a real-time scanner may: opens it for read-write with
    /// read-write-delete sharing as soon as its writer lets it (on Windows a writer that refuses the share is waited out,
    /// it is never pre-empted) and keeps it open until <see cref="Stop"/>. A file deleted before it is taken is skipped, and so
    /// is an entry that is no regular file (a directory, or the socket and FIFOs the .NET runtime of the child creates
    /// in its temp directory on Unix).
    /// </summary>
    /// <remarks>Each created file is taken on a thread-pool work item of its own, never on the watcher's event thread:
    /// on Linux that one thread also reads inotify, so a take that waits stalls every later event, and a new
    /// subdirectory is watched only once its own event is read (kb/Work PB5463).</remarks>
    private sealed class FreshFileHolder : IDisposable
    {
        private const string ProbeName = "holder-probe";
        private readonly FileSystemWatcher _watcher;
        private readonly string _probe;
        private readonly TaskCompletionSource _probeHeld = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Lock _gate = new(); // a Take's open-and-add and Stop's release never interleave
        private readonly List<FileStream> _held = [];
        private readonly List<string> _errors = [];
        private bool _stopped;
        private int _skipped;
        private HolderReport? _report;

        public FreshFileHolder(string directory)
        {
            _probe = Path.Combine(directory, ProbeName);
            _watcher = new FileSystemWatcher(directory) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName };
            _watcher.Created += (_, e) => ThreadPool.QueueUserWorkItem(static s => s.holder.RunTake(s.path), (holder: this, path: e.FullPath), preferLocal: false);
            _watcher.Error += (_, e) => Fail("the watcher reported an error", e.GetException());
            _watcher.EnableRaisingEvents = true; // the watch on the directory is in place when this returns
        }

        /// <summary>
        /// Plants a fresh file under the directory and completes when the holder has taken it, so the caller that
        /// awaits it knows the hold is live before it starts the process it observes. It waits for that fact, not for a
        /// time: the file is created after the watch is in place, so its event is certain to come. Every way the probe can
        /// fail to be taken ends the wait with an exception naming the cause (kb/Work PB5463 review R1-1): a watcher
        /// error (<see cref="Fail"/>), a probe gone or unopenable when its event is handled, a take that threw, or the
        /// holder stopped first.
        /// </summary>
        public Task ProveAsync()
        {
            File.WriteAllBytes(_probe, []);
            return _probeHeld.Task;
        }

        /// <summary>Records a failure the test's verdict must carry (<see cref="HolderReport.Errors"/>) and ends a pending
        /// probe wait with it.</summary>
        private void Fail(string what, Exception cause)
        {
            lock (_gate) _errors.Add($"{what}: {cause}");
            _probeHeld.TrySetException(new InvalidOperationException($"the holder's probe was not taken: {what}", cause));
        }

        /// <summary>The thread-pool work item: an exception a take throws reaches the test's verdict through
        /// <see cref="Fail"/>; it never vanishes and never reaches the pool, which would end the test host.</summary>
        private void RunTake(string path)
        {
            try { Take(path); }
            catch (Exception ex) { Fail($"taking {path} threw", ex); }
        }

        private void Take(string path)
        {
            while (true)
            {
                lock (_gate)
                {
                    if (_stopped) return;
                    try
                    {
                        if (!File.Exists(path)) // deleted already, or a directory
                        {
                            _skipped++;
                            if (path == _probe) _probeHeld.TrySetException(new FileNotFoundException("the holder's probe was gone when its event was handled", path));
                            return;
                        }
                        _held.Add(new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete));
                        if (path == _probe) _probeHeld.TrySetResult();
                        return;
                    }
                    catch (IOException ex) when (IsSharingViolation(ex)) { } // its writer refuses the share; take it when the writer closes
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // Gone, or no regular file: FileNotFound/DirectoryNotFound, ENXIO for a socket, a directory's access refusal.
                        _skipped++;
                        if (path == _probe) _probeHeld.TrySetException(ex);
                        return;
                    }
                }
                Thread.Yield();
            }
        }

        /// <summary>A sharing or lock violation on Windows: the writer still has the file and will release it. On Unix
        /// an <see cref="IOException"/>'s HResult is the raw errno, so no value is tested there: a refused open is skipped,
        /// which costs nothing, since a hold refuses no reader on Unix (the remarks above).</summary>
        private static bool IsSharingViolation(IOException ex) =>
            OperatingSystem.IsWindows() && ex.HResult is unchecked((int)0x80070020) /* ERROR_SHARING_VIOLATION */
                                                     or unchecked((int)0x80070021) /* ERROR_LOCK_VIOLATION */;

        /// <summary>Stops taking files, releases every held one, and reports what the holder did.</summary>
        public HolderReport Stop()
        {
            lock (_gate)
            {
                if (_report is not null) return _report;
                _stopped = true;
                _watcher.EnableRaisingEvents = false;
                _report = new HolderReport(_held.Count, _skipped, [.. _errors]);
                _probeHeld.TrySetException(new InvalidOperationException("the holder was stopped before its probe was taken"));
                foreach (var stream in _held) stream.Dispose();
                _held.Clear();
                return _report;
            }
        }

        public void Dispose()
        {
            Stop();
            _watcher.Dispose();
        }
    }
}
