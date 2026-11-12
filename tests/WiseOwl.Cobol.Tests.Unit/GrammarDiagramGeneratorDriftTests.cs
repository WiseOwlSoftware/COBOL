// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
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
/// The run happens under a HOSTILE temp directory (kb/Work PB5447): this test points the generator's <c>TMP</c>/<c>TEMP</c>
/// at a directory of its own and, from this process, opens every file that appears there for read-write with
/// read-write-delete sharing and holds it to the end of the run — what a real-time scanner or indexer can do to any
/// fresh file in <c>%TEMP%</c>. A generator that hands rr's output back through a file there and re-reads it
/// (<c>File.ReadAllText</c> asks for <c>FileShare.Read</c>, which a held write access refuses) exits 1 with "being
/// used by another process", however long it retries; the pipe handoff never names a file, so it runs clean.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class GrammarDiagramGeneratorDriftTests
{
    [Fact]
    public void Generator_RunsClean()
    {
        string script = TestRepo.Scripts("gen-grammar-diagrams.ps1");
        Assert.True(File.Exists(script), $"missing generator: {script}");

        string temp = ScratchDirectory.Create("grammar-diagram-held-");
        using var holder = new FreshFileHolder(temp);

        var psi = new ProcessStartInfo("pwsh", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Check")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = TestRepo.Root,
        };
        psi.Environment["TMP"] = temp;  // Path.GetTempPath reads TMP first, then TEMP
        psi.Environment["TEMP"] = temp;

        int exitCode;
        string stdout, stderr;
        using (var p = Process.Start(psi)!)
        {
            stdout = p.StandardOutput.ReadToEnd();
            stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(); // both streams are at EOF; no clock decides this test (kb/Work PB3022)
            exitCode = p.ExitCode;
        }
        int held = holder.Stop();
        ScratchDirectory.Remove(temp);

        Assert.True(exitCode == 0,
            $"gen-grammar-diagrams.ps1 -Check exited {exitCode} while this test held every fresh file under {temp}\n" +
            $"STDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        Assert.Contains("grammar-diagram note", stdout); // sanity: it reported a generation summary
        // The holder was live: the -Check run writes its notes under the temp directory, and each was taken and held.
        // Zero would mean the generator ignored TMP/TEMP, and the hostile-directory half of this test proved nothing.
        Assert.True(held > 0, $"no file appeared under {temp}: the generator did not write where this test held files");
    }

    /// <summary>
    /// Holds every file created under a directory, as a real-time scanner may: opens it for read-write with
    /// read-write-delete sharing as soon as its writer lets it (a writer that refused the share is waited out, it is
    /// never pre-empted) and keeps it open until <see cref="Stop"/>. A file deleted before it is taken is skipped.
    /// </summary>
    private sealed class FreshFileHolder : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly Lock _gate = new(); // a Take's open-and-add and Stop's release never interleave
        private readonly List<FileStream> _held = [];
        private bool _stopped;
        private int _heldCount;

        public FreshFileHolder(string directory)
        {
            _watcher = new FileSystemWatcher(directory) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName };
            _watcher.Created += (_, e) => Take(e.FullPath);
            _watcher.EnableRaisingEvents = true;
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
                        _held.Add(new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete));
                        return;
                    }
                    catch (FileNotFoundException) { return; }
                    catch (DirectoryNotFoundException) { return; }
                    catch (IOException) { } // its writer still refuses the share; take it when the writer closes
                }
                Thread.Yield();
            }
        }

        /// <summary>Stops taking files, releases every held one, and returns how many were held.</summary>
        public int Stop()
        {
            lock (_gate)
            {
                if (_stopped) return _heldCount;
                _stopped = true;
                _watcher.EnableRaisingEvents = false;
                _heldCount = _held.Count;
                foreach (var stream in _held) stream.Dispose();
                _held.Clear();
                return _heldCount;
            }
        }

        public void Dispose()
        {
            Stop();
            _watcher.Dispose();
        }
    }
}
