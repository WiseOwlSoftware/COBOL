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
(COMMIT and ROLLBACK arms, RELATIVE and INDEXED) and the golden `cases/14.9.7-COMMIT/pb3541_deactivated_clause_releases_its_file`,
RED on a mutant that skips the release. DESIGN-runtime-library.md 2.2.1.10's compiler-half paragraph states the release.
