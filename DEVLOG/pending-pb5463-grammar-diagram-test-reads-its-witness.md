## PB5463: the grammar-diagram test reads its liveness witness instead of counting what a watcher caught

`GrammarDiagramGeneratorDriftTests.Generator_RunsClean` bounced two landings out of the merge queue on Linux (#1580,
run 43327928785; #1590, run 43412496532), each time with `no file appeared under /tmp/...: the generator did not write
where this test held files` after the generator had exited 0. PB5447 had rewritten the test to run the generator under
a hostile temp directory and assert `held > 0`, the number of files a `FileSystemWatcher` callback held when the child
exited. The first fix of the test acted on a lead nobody had measured, so this one measured first.

**Reproduced.** A WSL clone of the branch, built Release, ran the unchanged test 40 times (4 copies at once beside 32
busy-loop workers on 32 CPUs): 1 red, with the merge group's exact message.

**Measured.** An instrumented throwaway copy of the test logged every watcher event and every take. The first file under
the directory is pwsh's own `dotnet-diagnostic-<pid>-<n>-socket`. Opening a socket fails with ENXIO, an `IOException`, and
the holder read every `IOException` as "the writer still refuses the share" and retried it on the watcher's one event
thread for the whole run (22 to 24 s). On Linux that thread also reads inotify, so nothing more was read: the
generator's subdirectory was never watched and none of its notes was ever seen. When pwsh exited, the socket's take
ended in FileNotFound and the queued events for the runtime's two `clr-debug-pipe-*` FIFOs were taken in the same
millisecond, just before pwsh removed them. `held` was 4 in both instrumented runs, and every one of the 4 was a runtime
FIFO. The liveness witness was satisfied by the .NET runtime's debug pipes, and it went red when pwsh removed them first.

**The fix.** Nothing the test asserts now depends on when the watcher's events arrive. The witness that the generator
wrote where the test held files is the generator's own summary line, `generated <n> grammar-diagram note(s) into <dir>`,
whose directory must lie under the test's temp directory. The holder proves itself live before the child starts: it
takes a probe file planted after the watch is in place, and the test awaits that take. Each created file is taken on a
thread-pool work item, never on the event thread; only a Windows sharing or lock violation is waited out; a file that
is gone, a directory or a socket is skipped; a watcher `Error` fails the test. On Unix a shared hold refuses no reader
(.NET's share modes are an advisory `flock` there), so the hostile half bites on Windows only, and the class remarks
now say so.

**Proved.** After the fix: 0 reds in 60 runs on Linux under the same load (4 copies at once, 32 busy workers). The pre-PB5447 generator passes the fixed test on Linux (1 run), as the advisory `flock` predicts. On Windows the fixed test passed against the
current generator, and failed against the pre-PB5447 generator (`git show 2352eff5df:scripts/gen-grammar-diagrams.ps1`,
swapped in uncommitted): `exited 1 while this test held every fresh file ... (held 23 file(s), skipped 2, watcher
errors 0)`, `ReadAllText` of `ccf-<guid>\CobolWords.md` "being used by another process".

**Sibling sweep.** Every test under `tests/` that reads an asynchronous observer of another process: `DirectoryNameWatch`
(used by `RegionLocksDriftTests` and `SidecarColumnLockHostDriftTests`) waits for the deletion report it needs, treats a
watcher `Error` or a report that never comes as a non-observation, and watches one directory with no subdirectories, so
it is not this defect. `LiveProcess`'s `ErrorDataReceived` stderr is read after the parameterless `WaitForExit`, which
waits for the asynchronous readers to reach end of file. No other test uses `FileSystemWatcher`.

**The second defect** of the input, that the same test case reddening two landings is surfaced by nothing, is filed as
kb/Work PB5464: `landing_measure` records each red's cases only inside the free-text `why`, its `summary` counts reds by
class, and the session probe reads none of it. The fix is a schema field, two views and a probe line in three tooling
files, which is a design of its own.
