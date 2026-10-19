## Entry 2245 — 2026-10-19 00:39 PDT — File the enqueue unit's review leads as PB3416-PB3419 and correct the keyed host-failure predicate in the files design

Filed four kb/Work notes from the enqueue unit's leads, each re-read on origin/main 0eba56a75. PB3416 (low, runtime/io): the
REWRITE and DELETE guards `target.Length > 0 && c.PermanentErrorInEffect is null` in `FileRegistry.cs` can never change an
outcome because `FileConnector.MutationTargetRecordId` already returns "" while the permanent error replays; the rule is
written twice (sequenced beside PB2749's lock-governance extraction). PB3417 (low, test-coverage): DESIGN-runtime-library
2.2.1.11 states the ledger path copy is at most 9 pages at R2's checkpoint, and `KeyedStorePerformanceWitnessTests` only
prints `mostLedgerPages`. PB3418 (medium, test-coverage): GR-14.9.51.4-12's keyed half (`KeyedConnector.TakeFileLock`
asking `AdmitsAnotherRunUnit`) is witnessed only by a source-text case outside the row's test-ref, with two stale comments
(`StoreCoherenceTests.cs:37-40`, `FileLockPosture.cs:96-111`). PB3419 (low, orchestrator): `orchestrate.ps1:194` still says
the landing lane's tick stops on STOP-UNIT, which stops being true once PB3402 lands; the note says to fix it only then.

Doc correction: `docs/COBOLNET_FILES_DESIGN.md` listed `OutOfMemoryException` in `KeyedConnector.IsHostStoreFailure`'s
predicate; the code is `IOException or UnauthorizedAccessException` (the OOM reason left with the whole-store model,
PB3335), so the doc no longer names it.

Where the leads came from: the branch reviews of PR #268 (PB3336 with PB3334 and PB3335 landed through it) and of
`notes-pb3381-leads-pb3380-landed-20261018`. No code changed.

