## PB3482: Windows carries Table 19 as columns, so a READ ONLY OPEN beside an INPUT opener that owes writes is admitted

PB3337 landed the runtime half of APPLY COMMIT and left one wrong answer on Windows, named in its own design paragraph: a
connector under commit control writes a record, is CLOSEd and re-OPENed INPUT, and its transaction still stands, so its
handle stays open for writing (it is the handle the COMMIT or ROLLBACK writes through). Another run unit's `OPEN SHARING
WITH READ ONLY INPUT` was then answered '61' until the COMMIT, where §14.9.27.4 Table 19 prints *Normal open* for the READ
ONLY INPUT and EXTEND / I-O rows against an existing ALL OTHER INPUT opener (`cite.py --check 14.9.27.4 "Normal open"` OK).
Linux answered '00'. The note's fix shape was "a posture in `FileLockPosture` that carries the owed-writes condition".

**Re-probe.** The witness written first (`CommitRollbackRuntimeTests.An_input_opener_that_owes_its_transactions_writes_admits_what_Table_19_admits`,
both organizations, B under READ ONLY INPUT and I-O, COMMIT and ROLLBACK, and the requests Table 19 refuses meeting B) went
red '61' with the fix disabled, as the note said.

**The estimate was wrong, and the root is wider than the note's three files.** A Windows share mode refuses by ACCESS and
`FileShare.Read`, the base posture of SHARING WITH READ ONLY, refuses every writer. No posture on B's side can say "this
writer is only the transaction's" unless something tells B that Table 19 admitted the request, and on Windows nothing could:
`RunUnitFileLock.HostOfThisProcess` was null there, so the table was carried by share modes alone. Widening B's share mode
always would have admitted a later I-O or OUTPUT opener the table refuses, because the share mode was the only thing
refusing it. So the fix is a structure, not a branch:

1. **`HandleColumnLockHost`** (new, with `WindowsByteRangeLocks` extracted from `HandleRegionLocks` so one `LockFileEx`
   expression serves both): Windows publishes its Table 19 column. Windows locks belong to a handle and conflict with every
   other handle, the asker's own included, so one shared byte per column (the Linux shape) cannot answer "does any holder
   OTHER than me hold this column". A holder opens a read handle of its own and holds exclusively one byte, chosen at random,
   of its column's 2^32-byte range (`HostRegionLocks.ColumnRangeStart`, from 2^62 + 2^61 + 2^60); it tests another column with
   a shared request over the whole range (released at once) and its own column with two requests around its own byte. It
   publishes first and tests second, as the other two hosts do. `SHARING WITH NO OTHER` publishes nothing (its handle is
   `FileShare.None`, which refuses the read handle and every other open), and a holder that meets such a handle is refused.
2. **`FileLockPosture.WhenOwedWritesAreAdmitted`**: the second posture. READ ONLY becomes `FileShare.ReadWrite`; NO OTHER and
   ALL OTHER have nothing to yield. `FileLockPostureDriftTests.EveryNormalOpenCellBesideAnInputOpenerThatOwesWritesIsAdmittedOnceTheReadOnlyPostureYields`
   proves over the 35 printed cells that this is exactly the READ ONLY requests and changes no other.
3. **`FileConnector.Open` is two attempts** (`OpenOnce`): the second only when the first was refused '61' by the host with the
   request's column HELD, on a host whose share modes are mandatory, and another run unit's INPUT opener holds its column
   (`RunUnitFileLock.AnotherInputOpenerHolds`: only an INPUT opener can owe writes). The first condition says Table 19
   admitted the request, the last that a WiseOwl COBOL connector is there to be admitted. A program outside the protocol that
   holds the file for writing, with no INPUT opener beside it, still refuses the request: the first version of the gate
   omitted the last condition and would have admitted it (`AReadOnlyOpenBesideAnOutsideWriterIsRefusedWhereTheShareModeRefusesIt`
   went red when the condition was removed, and an unobservable range answers "not held" for this question and "held" for
   the establishment, so an unknown never admits).

**What changed for Windows.** Every OPEN of a non-NO OTHER connector now takes a second read handle on the file and about
seven `LockFileEx` calls. `RunUnitFileLockDriftTests` (773 cases, every sharing and open mode pair through two registries)
and the new `HandleColumnLockHostDriftTests` (every pair on the host alone, release, retake, the race, contention, 40
holders, the INPUT-opener question, a refusing `FileShare.None` handle, absent / directory / device paths; the cases run on
Windows and a forced host elsewhere is `Unavailable`) hold it to the table. The doc sites that said Windows had no column
host (`DESIGN-runtime-library.md` 2.1 and 2.2.1.10 As built (3), `CONFORMANCE.md` DOC-A.1-75, `COBOLNET_FILES_DESIGN.md`, the
`IColumnLockHost` / `RunUnitFileLock` / `FileLockPosture` remarks) are current, with the determination about outside programs.

**Self-review** (silent-failure, test-analysis, comment-accuracy, type-design lenses). Found and fixed: the tri-state of the
range test (an unobservable range was "held", right for the establishment and wrong for the question that lets a share mode
yield); publication retries that could all fall inside one tester's microsecond (yield, then 1 ms pauses, 64 attempts,
`ContendedPublishersAndTesters_AreOnlyEverHeldOrRefused`); a comment that claimed a Windows lock behaviour no test pins
(softened to what is relied on). Mutations run: second attempt disabled, the witness red; INPUT-opener condition removed,
the outside-writer test red. Siblings: the other arm of the dispatch is the sequential organization, which never owes writes
(no APPLY COMMIT on it yet, PB3340); the second attempt is organization-agnostic and `RunUnitFileLockDriftTests` covers it.
PB3483 (the kept-handle give-back on any IOException in `KeyedConnector.Lock`) is a different mechanism of the same
cluster and was left alone.

**Gates.** Windows: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261021T183116Z-6cd78e`;
Conformance 10,850/18,047, Unit 34,263/35,147, Characterization 5/36 in leg 1). Linux (`--legs unit,conformance`):
`=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built; HEAD 31b7cf56d) ===`. The full Unit assembly run
once on Windows by hand before the final refinement: 35,174 of 35,176, the two reds being `ExternalCorpusPopulationDriftTests`
(the external corpora are not in a fresh worktree). Registered: `kb/Work/PB3482.md` landed, `closes_rows: []` with its reason
(it claims no inventory row).
