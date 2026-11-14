## PB5478: the grammar-diagram test's file holder finds files by enumeration, so a watcher overflow loses nothing

`GrammarDiagramGeneratorDriftTests.Generator_RunsClean` went red twice on 2026-11-13 on Windows, once in a local gate and
once in merge-group run 43550577247 (pull request #1607), with `InternalBufferOverflowException` from the watcher of its
`FreshFileHolder`. PB5463 had made any watcher error fail the test, because a dropped report meant a file the holder never
held.

Measured: the generator's run makes 184 file events (162 are java's `jar_cache*.tmp`), about 15 KB of reports against the
watcher's 8 KB default buffer. The watcher's callback runs on a thread-pool thread, which a loaded Unit population delays.
Reproduced on demand by stalling the watcher's event thread during a 1,000-file burst: the old holder was red 5 times in 5.

The holder now lists the held tree on a scan thread of its own and takes each file it has not met. A report or an
overflow only wakes the scan; an overflow is counted, not failed, and every other watcher error still fails. The new
`Holder_HoldsEveryFile_WhenItsWatcherOverflows` forces the overflow and leaves it the only wake-up: it passed 10 times in
10. Removing the overflow's wake makes it hang rather than pass. The pre-PB5447 generator still turns `Generator_RunsClean`
red ("being used by another process"). The note's suggested bigger buffer was not used, because it only raises the load at
which the overflow happens. Sibling: `DirectoryNameWatch` asserts report order, which enumeration cannot replace. It is
listed in `kb/Work/PB5478/2026-11-13-sibling-sweep.md`.
