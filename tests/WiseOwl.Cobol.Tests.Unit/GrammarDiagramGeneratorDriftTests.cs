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
/// <para>⛔ WHICH FILES ARE HELD DOES NOT DEPEND ON THE WATCHER DELIVERING EVERY REPORT EITHER (kb/Work PB5478). A
/// <see cref="FileSystemWatcher"/> is lossy by contract, and on a loaded Windows host its 8 KB buffer overflowed during the
/// generator's burst of about 184 file events. The holder finds files by ENUMERATING the directory tree; a report, or the
/// overflow that stands for the reports it dropped, only wakes that enumeration (<see cref="FreshFileHolder"/>), and
/// <see cref="Holder_HoldsEveryFile_WhenItsWatcherOverflows"/> forces the overflow.</para>
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
    /// ⛔ A WATCHER'S BUFFER OVERFLOW LOSES NO FILE AND FAILS NOTHING (kb/Work PB5478). A <see cref="FileSystemWatcher"/> is
    /// lossy by contract: when its event thread falls behind (a loaded host, where the thread pool that runs its callback
    /// is busy), the kernel's buffer fills, its reports are dropped and it raises <see cref="InternalBufferOverflowException"/>.
    /// The generator's run makes about 184 file events (162 of them java's <c>jar_cache*.tmp</c>), about 15 KB of reports
    /// against the default 8 KB buffer, so a stall of a few seconds overflowed it twice on 2026-11-13. This test makes the
    /// overflow certain instead of likely: it stalls the watcher's event thread (the hook runs on it) while it writes a
    /// burst of files whose reports need several times the buffer, plants a second probe while the thread is still
    /// stalled, and then releases the thread. On Windows the hook also drops every report of the burst and the second
    /// probe that reached the watcher before the overflow, so the ONLY thing left to wake the holder is the overflow
    /// itself, as when a generator's last files fall in the dropped tail. Every file must be held, the overflow must be
    /// counted, and no error may be recorded. Nothing here waits on a clock: the second probe is proved only once a scan
    /// that listed it has ended, and that scan listed every file of the burst.
    /// </summary>
    [Fact]
    public async Task Holder_HoldsEveryFile_WhenItsWatcherOverflows()
    {
        const int burst = 1000; // 1000 reports of a 12-character name are about 36 KB, 4.4 times the 8 KB default buffer
        string temp = ScratchDirectory.Create("grammar-diagram-overflow-");
        using var stall = new ManualResetEventSlim(initialState: false);
        int stalled = 0;
        bool afterFirstProbe = false;
        HolderReport held;
        using (var holder = new FreshFileHolder(temp, onEvent: path =>
               {
                   if (!Volatile.Read(ref afterFirstProbe)) return true;
                   if (Interlocked.Exchange(ref stalled, 1) == 0)
                       stall.Wait(); // the watcher's event thread, stalled as a loaded host stalls it
                   // Linux's inotify queue (16,384 by default) holds the burst, so nothing overflows there and the reports
                   // must wake the holder; on Windows the 8 KB buffer is certain to overflow (asserted below).
                   return !OperatingSystem.IsWindows();
               }))
        {
            await holder.ProveAsync();
            Volatile.Write(ref afterFirstProbe, true);
            for (int i = 0; i < burst; i++) File.WriteAllBytes(Path.Combine(temp, $"burst-{i:D6}"), []);
            Task proved = holder.ProveAsync(); // planted while the event thread is stalled
            stall.Set();
            await proved;
            held = holder.Stop();
        }
        ScratchDirectory.Remove(temp);

        Assert.True(held.Errors.Count == 0, $"the holder failed ({held}):\n" + string.Join("\n", held.Errors));
        Assert.Equal(burst + 2, held.Held); // the burst and both probes
        // On Windows the stalled thread is certain to overflow the 8 KB buffer (measured: every one of 35 stalled bursts), and
        // the overflow was the only wake-up the second probe had. Linux's inotify queue holds the burst, so there it is
        // delivered late rather than dropped and the reports themselves wake the holder.
        if (OperatingSystem.IsWindows()) Assert.True(held.Overflows > 0, $"the watcher did not overflow, so this test proved nothing ({held})");
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


    /// <summary>What a <see cref="FreshFileHolder"/> did, for the failure messages: how many files it held and skipped, how
    /// many times its watcher overflowed (each one a wake-up, never an error: kb/Work PB5478), and every error its watcher
    /// raised, its scan met or a take threw.</summary>
    private sealed record HolderReport(int Held, int Skipped, int Overflows, IReadOnlyList<string> Errors)
    {
        public override string ToString() => $"held {Held} file(s), skipped {Skipped}, watcher overflows {Overflows}, errors {Errors.Count}";
    }

    /// <summary>
    /// Holds every regular file created under a directory, as a real-time scanner may: opens it for read-write with
    /// read-write-delete sharing as soon as its writer lets it (on Windows a writer that refuses the share is waited out,
    /// it is never pre-empted) and keeps it open until <see cref="Stop"/>. A file deleted before it is taken is skipped, and so
    /// is an entry that is no regular file (the socket and FIFOs the .NET runtime of the child creates in its temp
    /// directory on Unix).
    /// </summary>
    /// <remarks>
    /// <para>⛔ THE FILES ARE FOUND BY ENUMERATION; THE WATCHER ONLY SAYS WHEN TO LOOK (kb/Work PB5478). A
    /// <see cref="FileSystemWatcher"/> is lossy by contract: its event thread is a thread-pool thread on Windows, a loaded
    /// host delays it, the kernel's buffer fills and its reports are DROPPED, and what is left is one
    /// <see cref="InternalBufferOverflowException"/>. Measured: the generator's run makes about 184 file events (162 of them
    /// java's <c>jar_cache*.tmp</c>), about 15 KB of reports against the default 8 KB buffer, and a loaded gate and a
    /// merge-group shard each overflowed it on 2026-11-13. A holder that took the files its reports named therefore missed
    /// the files whose reports were dropped, and PB5463 rightly made that a red. So no report names a file here: every report
    /// (a create or rename of a file or directory) and every overflow only WAKES the scan thread, which lists the whole
    /// tree and takes each file it has not met before. A dropped report costs nothing, because the overflow that stands for
    /// it wakes the same scan; only a watcher error other than an overflow (it stopped watching) fails the test.</para>
    /// <para>The scan runs on a thread of its own, never on the watcher's event thread (on Linux that one thread also reads
    /// inotify, and a stalled take there stalled every later event: kb/Work PB5463), and a file whose writer still refuses
    /// the share is waited out on a thread-pool work item of its own, so one slow writer never delays the scan. The wake-up
    /// is a monitor, not a disposable wait handle, so a report the watcher raises while it is being disposed cannot throw
    /// on its event thread.</para>
    /// </remarks>
    private sealed class FreshFileHolder : IDisposable
    {
        private const string ProbePrefix = "holder-probe-";
        private static readonly EnumerationOptions EveryFile = new() { RecurseSubdirectories = true, AttributesToSkip = 0, IgnoreInaccessible = true };
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private readonly string _directory;
        private readonly Func<string, bool>? _onEvent;
        private readonly FileSystemWatcher _watcher;
        private readonly Thread _scanner;
        private readonly object _wakeGate = new(); // a Monitor's Wait/Pulse, which a System.Threading.Lock has not
        private bool _wakeRequested;
        private readonly Lock _gate = new(); // a take's open-and-add, the scan's bookkeeping and Stop's release never interleave
        private readonly HashSet<string> _met = new(PathComparer); // every path a scan has listed: held, being taken, or skipped
        private readonly Dictionary<string, Probe> _probes = new(PathComparer);
        private readonly List<FileStream> _held = [];
        private readonly List<string> _errors = [];
        private bool _stopped;
        private int _skipped, _overflows, _probeCount;
        private HolderReport? _report;

        /// <summary>A probe planted by <see cref="ProveAsync"/>: proved once it is held AND a scan that listed it has ended. When
        /// no scan was already part-way through its listing as the probe was written, that scan listed every file written
        /// before the probe too.</summary>
        private sealed class Probe
        {
            public readonly TaskCompletionSource Proved = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool Held, Listed;
        }

        /// <param name="directory">The directory whose tree is held.</param>
        /// <param name="onEvent">Runs on the watcher's event thread before each report is handled, and returns false to drop
        /// the report as an overflow does. A test of the holder stalls that thread and drops reports through it, as a loaded
        /// host does (<see cref="Holder_HoldsEveryFile_WhenItsWatcherOverflows"/>).</param>
        public FreshFileHolder(string directory, Func<string, bool>? onEvent = null)
        {
            _directory = directory;
            _onEvent = onEvent;
            _scanner = new Thread(ScanUntilStopped) { IsBackground = true, Name = "FreshFileHolder scan" };
            _scanner.Start();
            _watcher = new FileSystemWatcher(directory)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
            };
            _watcher.Created += (_, e) => Report(e.FullPath);
            _watcher.Renamed += (_, e) => Report(e.FullPath);
            _watcher.Error += (_, e) =>
            {
                Exception cause = e.GetException();
                if (cause is InternalBufferOverflowException)
                {
                    lock (_gate) _overflows++;
                    Wake(); // the reports it dropped named files the scan will list
                }
                else Fail("the watcher reported an error", cause);
            };
            _watcher.EnableRaisingEvents = true; // the watch on the directory is in place when this returns
        }

        private void Report(string path)
        {
            if (_onEvent is null || _onEvent(path)) Wake();
        }

        private void Wake()
        {
            lock (_wakeGate)
            {
                _wakeRequested = true;
                Monitor.Pulse(_wakeGate);
            }
        }

        /// <summary>
        /// Plants a fresh file under the directory and completes when the holder has taken it and a scan that listed it has
        /// ended, so the caller knows the hold is live before it goes on; when nothing woke a scan while the caller wrote its
        /// files (<see cref="Holder_HoldsEveryFile_WhenItsWatcherOverflows"/> stalls every wake), every file written before
        /// the call has been met as well. It waits for that fact, not for a time: the file is created after the watch is in place, so its report,
        /// or the overflow that dropped it, is certain to wake a scan. Every way the probe can fail to be taken ends the wait
        /// with an exception naming the cause (kb/Work PB5463 review R1-1): a watcher error (<see cref="Fail"/>), a probe
        /// gone or unopenable when it is taken, a scan or a take that threw, or the holder stopped first.
        /// </summary>
        public Task ProveAsync()
        {
            var probe = new Probe();
            string path;
            lock (_gate)
            {
                if (_stopped) return Task.FromException(new InvalidOperationException("the holder was stopped before its probe was planted"));
                path = Path.Combine(_directory, ProbePrefix + ++_probeCount);
                _probes.Add(path, probe);
            }
            File.WriteAllBytes(path, []);
            return probe.Proved.Task;
        }

        /// <summary>Records a failure the test's verdict must carry (<see cref="HolderReport.Errors"/>) and ends every pending
        /// probe wait with it.</summary>
        private void Fail(string what, Exception cause)
        {
            lock (_gate)
            {
                _errors.Add($"{what}: {cause}");
                foreach (Probe probe in _probes.Values)
                    probe.Proved.TrySetException(new InvalidOperationException($"the holder's probe was not taken: {what}", cause));
            }
        }

        private void ScanUntilStopped()
        {
            try
            {
                while (true)
                {
                    lock (_wakeGate)
                    {
                        while (!_wakeRequested) Monitor.Wait(_wakeGate);
                        _wakeRequested = false; // reports that arrive while this scan runs wake the next one
                    }
                    lock (_gate) if (_stopped) return;
                    Scan();
                }
            }
            catch (Exception ex) { Fail("the holder's scan threw", ex); } // reaches the verdict; never ends the test host
        }

        /// <summary>One pass: lists the whole tree, takes each file it has not met, then marks every probe it met as listed.</summary>
        private void Scan()
        {
            var listed = new List<string>();
            bool complete = true;
            try
            {
                foreach (string path in Directory.EnumerateFiles(_directory, "*", EveryFile)) listed.Add(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && Directory.Exists(_directory))
            {
                complete = false; // a subdirectory went while it was listed: take what was listed and list the tree again
            }
            foreach (string path in listed)
            {
                lock (_gate)
                {
                    if (_stopped) return;
                    if (!_met.Add(path)) continue;
                }
                if (!TryTake(path))
                    ThreadPool.QueueUserWorkItem(static s => s.holder.TakeWhenShared(s.path), (holder: this, path), preferLocal: false);
            }
            if (!complete)
            {
                Wake();
                return;
            }
            lock (_gate)
            {
                foreach ((string path, Probe probe) in _probes)
                    if (_met.Contains(path))
                    {
                        probe.Listed = true;
                        if (probe.Held) probe.Proved.TrySetResult();
                    }
            }
        }

        /// <summary>The thread-pool work item for a file whose writer refused the share: retakes it until the writer
        /// closes it. An exception reaches the test's verdict through <see cref="Fail"/>; it never vanishes and never
        /// reaches the pool, which would end the test host.</summary>
        private void TakeWhenShared(string path)
        {
            try
            {
                while (!TryTake(path)) Thread.Yield();
            }
            catch (Exception ex) { Fail($"taking {path} threw", ex); }
        }

        /// <summary>Takes <paramref name="path"/> once: true when it is held, skipped or the holder has stopped; false when its
        /// writer still refuses the share.</summary>
        private bool TryTake(string path)
        {
            lock (_gate)
            {
                if (_stopped) return true;
                _probes.TryGetValue(path, out Probe? probe);
                try
                {
                    _held.Add(new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete));
                    if (probe is not null)
                    {
                        probe.Held = true;
                        if (probe.Listed) probe.Proved.TrySetResult();
                    }
                    return true;
                }
                catch (IOException ex) when (IsSharingViolation(ex))
                {
                    return false; // its writer refuses the share; take it when the writer closes
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Gone (FileNotFound, DirectoryNotFound) or no regular file (ENXIO for a socket).
                    _skipped++;
                    probe?.Proved.TrySetException(new InvalidOperationException($"the holder's probe {path} could not be taken", ex));
                    return true;
                }
            }
        }

        /// <summary>A sharing or lock violation on Windows: the writer still has the file and will release it. On Unix
        /// an <see cref="IOException"/>'s HResult is the raw errno, so no value is tested there: a refused open is skipped,
        /// which costs nothing, since a hold refuses no reader on Unix (the remarks above).</summary>
        private static bool IsSharingViolation(IOException ex) =>
            OperatingSystem.IsWindows() && ex.HResult is unchecked((int)0x80070020) /* ERROR_SHARING_VIOLATION */
                                                     or unchecked((int)0x80070021) /* ERROR_LOCK_VIOLATION */;

        /// <summary>Stops taking files, releases every held one, ends the scan thread, and reports what the holder did.</summary>
        public HolderReport Stop()
        {
            HolderReport report;
            lock (_gate)
            {
                if (_report is not null) return _report;
                _stopped = true;
                _watcher.EnableRaisingEvents = false;
                _report = report = new HolderReport(_held.Count, _skipped, _overflows, [.. _errors]);
                foreach (Probe probe in _probes.Values)
                    probe.Proved.TrySetException(new InvalidOperationException("the holder was stopped before its probe was taken"));
                foreach (var stream in _held) stream.Dispose();
                _held.Clear();
            }
            Wake();
            _scanner.Join(); // it returns at its next look at _stopped: no clock
            return report;
        }

        public void Dispose()
        {
            Stop();
            _watcher.Dispose();
        }
    }
}
