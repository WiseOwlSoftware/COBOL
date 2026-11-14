## Sibling sweep (worker w20262023a, 2026-11-13)

`grep -rln FileSystemWatcher tests src scripts tools`: two files.

- `tests/WiseOwl.Cobol.Tests.Unit/GrammarDiagramGeneratorDriftTests.cs`: this note's fix.
- `tests/WiseOwl.Cobol.Tests.Unit/DirectoryNameWatch.cs` (used by `RegionLocksDriftTests.TheLastHoldersCloseRenamesTheLockDirectoryAwayRatherThanDeletingItInPlace`
  and `SidecarColumnLockHostDriftTests.TheLastHoldersCloseRenamesTheLockDirectoryAwayAndTheNextOpenIsGranted`). It does
  assert on watcher reports, but it asserts their ORDER (a lock directory renamed away rather than deleted in place), which
  enumeration cannot see, so this fix's mechanism does not apply. It watches one parent directory's subdirectory names
  (`NotifyFilters.DirectoryName`, no subtree) with a 64 KB buffer, for a CLOSE that makes a handful of reports, and an
  overflow there raises `HarnessNonObservationException` ("the directory watcher lost reports"), not a verdict on the order.
  Its wait is bounded by `ObservationBudget.UnitMs`. Not changed here: its overflow margin is several hundred reports, and
  whether a non-observation reddens a gate is that seam's own design (kb/Work PB3109). No red of either test has been
  seen in the landing record this note's author read.
