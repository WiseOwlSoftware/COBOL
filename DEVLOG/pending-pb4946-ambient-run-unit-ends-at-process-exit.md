## PB4946 the ambient run unit ends at process exit (§2023:14.6.11 2, §2023:9.1.18.5)

Re-probe: a C# host that activates a program through its generated `Call()` outside `RunUnit.Run` and returns from `Main`
left the program's open LINE SEQUENTIAL file unwritten (the new `HostRunUnitEndingTests.AnAmbientRunUnit_IsEndedByTheHostProcessExit_ItsOpenFilesClosed`
failed on the string compare with the fix disabled). Nothing subscribed to process exit, so DOC-A.1-167's "or at process exit
for the ambient run unit" was false.

Fix: `RunUnit` keeps a process-wide set of the run units that have opened a file and not yet ended (`_openRunUnits`; entered by
`FileRegistry.Opening`, which `OpenCore` raises before every OPEN; left by `Terminate`, so every existing boundary is unchanged).
`AppDomain.ProcessExit` runs `EndAtProcessExit`, which `Terminate`s whatever remains as a normal termination: the implicit CLOSE
of every open file, which commits first (§2023:9.1.18.5). A run unit enters only on its first OPEN, so a host's idle
ambient run units (one per async flow) are not pinned in memory. A close failure goes to standard error and does not stop the
other run units. The set is registered in `RunUnitStateDriftTests.ProcessLifetimeCollections`.

DOC-A.1-167's "Diverges today" sentence is replaced by the behaviour, with the new witness. Closes no inventory row (the note
claimed none).

Review round 1 (two blocking findings). R1-1: the set pinned every run unit that ever opened a file until exit. The registry now
tells its run unit when it starts and stops owing an ending (`FileRegistry.Owing`; owing = a file open, a retired/kept
transaction, or a standing transaction, `OwesAnEnding`); a CLOSE, COMMIT or ROLLBACK settles it (`SettleOwing`), which removes the
run unit from the set so a per-request ambient run unit that closed everything is collectable
(`AnAmbientRunUnit_ThatClosedEverythingItOpened_IsNotPinnedForTheLifeOfTheProcess`, a host that GCs a WeakReference; fails with the
removal disabled). R1-2: `AnAmbientRunUnit_ThatClosedItsFileUnderApplyCommit_IsCommittedAtTheHostProcessExit` covers the
§2023:9.1.18.5 commit of a file already closed under APPLY COMMIT (fails with the ProcessExit subscription removed).
