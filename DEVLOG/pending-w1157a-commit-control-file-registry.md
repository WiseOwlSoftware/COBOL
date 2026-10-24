## Commit control on the file registry: a deactivated clause releases its files (PB3541)

**PB3541 (wave 1157, group A).** A file connector put under commit control by an APPLY COMMIT clause stayed under it after
the clause was deactivated. Re-probed on 07bfc3606: a main program whose clause names only WS-X calls a subprogram whose
clause names the EXTERNAL RELATIVE file RELF, CANCELs it and COMMITs, so §14.9.7.4 GR3 (*"The COMMIT statement also
deactivates any APPLY COMMIT clauses in exited initial programs and canceled runtime elements"*) deactivates the
subprogram's clause; a third program, with no clause, then writes record 1 of RELF, and the main program's ROLLBACK undid
it (`RECORD 1 ABSENT`). §14.9.36.4 GR3 restores only *"files and data-items referenced in active APPLY COMMIT clauses"*, and
§12.4.6.3.4 GR5 makes a canceled element's clause active only until that deactivation. The mechanism: `TransactionControl`
dropped the retired participant and nothing undid `FileRegistry.RegisterCommitControl`, which has no inverse, so the
connector kept `CommitControlled` (its WRITEs journaled into the run unit's transaction, §14.6.13.1.3 item 8 still due on its
fatal statuses) and the implicit AUTOMATIC posture of §9.1.16.

The fix: each participant keeps the file connectors it put under commit control, and `TransactionControl.DeactivateRetired`,
the one deactivation (a successful COMMIT and every ROLLBACK), takes each file a deactivated clause named that no remaining
active clause names out of commit control through the new `FileRegistry.ReleaseCommitControl`, which clears the flag and puts
back the file control entry's own lock posture (the registration now records it; a displaced connector forgets it with its
share). A transaction whose rollback the medium refused stays owed: the registry finds it by its open transaction id, not by
the flag. Witnesses: `TransactionControlTests.A_file_whose_last_clause_is_deactivated_is_no_rollbacks_and_no_item_8s`
(COMMIT and ROLLBACK arms, RELATIVE and INDEXED), `A_deactivated_clause_gives_the_file_its_declared_lock_posture_back` (the
entry's MANUAL posture back, RED on a mutant that keeps the AUTOMATIC share), the two refused arms
`A_refused_rollback_that_deactivates_the_clause_leaves_the_files_transaction_owed` and
`A_refused_commit_deactivates_nothing_and_item_8_restores_the_file_then_deactivates_the_clause` (a refused COMMIT leaves F
under commit control, so §14.6.13.1.3 item 8's ROLLBACK restores it and only then deactivates the clause; RED on a mutant
that runs the deactivation before the refusal check, branch review R1-1), and the golden
`cases/14.9.7-COMMIT/pb3541_deactivated_clause_releases_its_file`, RED on a mutant that skips the release.
DESIGN-runtime-library.md 2.2.1.10's compiler-half paragraph states the release.

**PB3492 re-probed and settled; PB3340 is not in this landing.** PB3340 (the sequential journal, steps 1-4 with its
witnesses, docs and verdict batch) is implemented on its own branch, `w1157a-pb3340-sequential-journal`, and the loop's branch `worktree-wf_f6f89a51-99e-1` carries a second, partial implementation;
the two are reconciled before either lands. On this branch's build a RELATIVE
file under APPLY COMMIT answers '62' to a DELETE FILE after its CLOSE (its own kept handle read as another holder's by
`HostFile.IsHeldByAnother`), and the re-probe found a sibling with the same root: an OPEN OUTPUT of an existing keyed file
after a COMMIT, then a ROLLBACK, leaves the file empty (`RECORD 1 ABSENT`), because `KeyedConnector.Recreate` ends the kept
transaction when the creation lands. The owner question PB3492 carried is answered by the standard: the NOTE under §14.9.10.4
GR15 says such files *"can still be deleted, however if they are, then in the event of a rollback they will be restored"*, and
GR15's conflict is a file *"locked by another file connector"*. DESIGN-runtime-library.md 2.2.2.14 now settles the shape: the
keyed organizations' two capture verbs ride PB3340's journal (the before file copied through the kept handle, the `K`
record, then the creation or the removal; ROLLBACK restores the before file and rolls back the run unit's transaction inside
it), and it records the one primitive the journal design assumes and the runtime lacks, a directory sync, for the
`IHostMedium` seam to own.
