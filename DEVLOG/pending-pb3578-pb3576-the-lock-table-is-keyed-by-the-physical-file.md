## PB3578, PB3576: the run unit's lock table is keyed by the physical file, and a kept state gives all its record locks back

Group A3 (wave 20261024), the keyed connector's kept-state root. Two notes, one root: what a CLOSE under commit control keeps (the handle, the file lock, the
record locks, the transaction) belongs to a PHYSICAL FILE, and the run unit's tables named that file by its path or by the registry's publication of it.

**PB3578 (re-probed).** `KeyedConnector.ReleaseKept`, called when a kept state ends inside the connector, released only the record locks the kept endpoint
published (`PhysicalFileTable.ReleaseThrough`); the registry's by-name release (`ReleaseKeptOf`) covered the locks no host published (PB3480, PB3484), but the
connector cannot call it, it does not know its registry name. Reproduced on Windows through the one ending arm the host lets it reach: on a host whose region
locks answer unavailable for record bytes, a commit-controlled connector READs record 2, is CLOSEd (the lock stays, §14.9.6.4 GR9) and re-OPENed OUTPUT
(`Recreate` ends the kept state): `RecordLocksHeldBy("F")` stayed 1 for a connector with nothing left to release it. Fix at the root: `KeptFile.Release` is the ONE
giving back of a kept state and releases ALL its connector's locks by `KeptFile.LockOwner` (the name the lock table holds them under, handed to the connector
with the physical-file state at every OPEN: `FileConnector.AssociatePhysical(state, lockOwner, locksRecords)`), then the handle, the file lock and the
attachment. The registry's duplicate by-name release of a retired state is gone.

**PB3576 (re-probed on Linux).** `PhysicalFileTable` held ONE `State` per path, so after a file was removed or renamed away outside the run unit a second
connector opening the path met the old file's Table 19 entries, file lock and record locks ('61', '51'), and PB3533 had to END (not retire) a renamed-away file's
kept state, leaving its transaction for the next writable OPEN's rollback. Fix: the table is keyed by the physical file's host identity. `State.Identity`
(`PhysicalFileIdentity`, learned from a connector's open handle by `PhysicalFileTable.Learn`); `For(path, HostFile.IdentityAt(path))`, asked at the arbitration of
every OPEN before the handle exists, finds the state BY the identity under whatever name, and by the path only for an absent file or where the host cannot tell.
A connector's record statements use the state its last OPEN arbitrated (`FileConnector.Physical`, `FileRegistry.PhysicalOf`), never a path lookup, and the run
unit's record-lock ceiling counts every state. With the tables apart, the kept state of a file the path no longer names RETIRES instead of ending
(`FileConnector.RetireWhatWasKeptOfAnotherFile(presence)` → `KeyedConnector.RetireKept` → its `KeptSink`, the one hand-over `FileRegistry.Retire` uses too):
the renamed file's change is COMMITted (§14.9.7.4 GR3), not abandoned. The two `ReleaseKept()` calls of the absent OPTIONAL arms are gone, the hook having handed
the state over first, so a refused handle leaves it whole (§14.9.27.4 GR25). Sibling swept in the same file (I9): `FileRegistry.OpenByAnotherConnector`
(DELETE FILE's '62', §9.1.13.9 2) compared connectors' PATHS; it asks the file's state, so a connector open on a file since replaced no longer refuses the DELETE FILE
of the new one. Recorded determination (DESIGN-runtime-library.md 2.2.1.10): on the sidecar host (macOS), where column locks are published by path, the retired
state's column file stays published until the COMMIT.

**The estimate was too small.** The note said "key `PhysicalFileTable` by identity". Doing it exposed that every statement path looked its state up by path, that
the test-surface lock calls reach connectors never opened, that a state must be findable under its file's new name (the renamed file is reached again), and that
the DELETE FILE check was a second path comparison. DESIGN-runtime-library.md 2.2.1.7 and 2.2.1.10 say so.

**Re-probe of the Linux witnesses.** A temporary commit restored the path-only behaviour (`For` ignoring the identity; the hook ending the kept state); the Linux unit
leg then failed the new `PhysicalFileTableIdentityTests` (3) and `A_retired_kept_states_unpublished_record_locks_belong_to_its_file_and_end_with_the_commit`, among the
PB3533 witnesses re-stated for the retire, before a test hung and the run was stopped. With the fix the Linux unit leg's 35.8k tests passed except the two PB3493
witnesses whose assertion described the old ending (re-stated: the transaction stands in the registry's retired state until the ROLLBACK, `RetiredTransactionOf`).

**Tests (I11).** `PhysicalFileTableIdentityTests` (every host); `CommitRollbackRuntimeTests`: `An_open_output_over_the_kept_file_gives_back_the_record_locks_the_host_could_not_publish`
(Windows and Linux), `Two_connectors_on_a_path_whose_file_was_replaced_meet_none_of_each_others_locks`, `A_delete_file_of_a_path_whose_file_was_replaced_...`,
`A_retired_kept_states_unpublished_record_locks_belong_to_its_file_...` (Linux, `how` removed/renamed, both organizations, SHARING ALL OTHER and NO OTHER), and the
PB3533/PB3493 witnesses re-stated for the retire: the replaced file's change COMMITted, the refusals ('39', '61', the refused handle and attach) leaving the retired
state as the CLOSE left it. Notes: kb/Work/PB3578 and PB3576 landed (`closes_rows: []`: runtime defects with no inventory row); PB3579's names updated.

**Gate.** `TestResults/build-local/000001-20261024T011042Z`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run:
Conformance 8,764 · Unit 953 · Characterization 31 … Conformance 9,563/18,327 · Unit 34,873/35,826 · Characterization 5/36 cases ran`; the 31 added tests are `Passed` in `leg-1-Unit.trx`.
`=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built; HEAD d78a163f7) ===` (unit 35,861, conformance 18,327; two earlier runs of the same head were RED
only with "No space left on device" while other fleets filled the WSL disk, which also turned 22 `CobolFileLockTests` red). `python scripts/semgrep/verify.py`: PASS.
