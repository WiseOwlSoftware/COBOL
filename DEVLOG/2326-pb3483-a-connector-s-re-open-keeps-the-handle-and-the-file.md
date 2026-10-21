## Entry 2326 — 2026-10-21 11:58 PDT — PB3483 - a connector's re-OPEN keeps the handle and the file lock its CLOSE kept under commit control, whatever it meets

**Mechanism.** A CLOSE of a connector under APPLY COMMIT keeps the handle (the §9.1.15 file lock, the transaction's liveness
byte) and the Table 19 column lock until the COMMIT or ROLLBACK (§14.9.7.4 GR4), and an unsuccessful OPEN leaves the file
unaffected (§14.9.27.4 GR25). `KeyedConnector.Lock` gave the kept handle away on ANY `IOException` to take the one a
different share mode needed, and nothing put it back. Reproduced on Windows: A wrote record 7 under ALL OTHER and was CLOSEd,
B opened I-O beside it, A's re-OPEN under READ ONLY was refused '61' (B refuses the new share mode, and so does A's own kept
handle), and a third run unit's writable OPEN then rolled A's live transaction back, because the liveness byte had gone with
the handle (its REWRITE of record 7 answered '23'). The file lock was gone the same way. The column lock had the same shape
one level up: the re-OPEN gave the kept column back, took its own, and took the kept one again when unsuccessful, two instants
in which another run unit's OPEN against the kept column was admitted.

**Fix, two arms.**
- The handle. The give-back happens only for a sharing refusal (`HostFile.IsSharingRefusal`); any other `IOException` leaves
  the kept handle where it is. The handle given away is recorded, and `AbandonOpen` takes it again for EVERY unsuccessful
  ending after the swap (`KeyedConnector.RetakeKeptHandle`: the same access and share mode, attached to the store as an
  OPEN attaches its own, the liveness byte held again). The sibling sweep found the second ending: a refused adoption after a
  successful swap ('30') lost the kept handle the same way. The instant between the give-back and the retake stays, because a
  handle that refuses the new one cannot be beside it on a host whose share modes are per handle; the design names it.
- The column lock. `RunUnitFileLock.Amend` replaces the kept lock on its own hold: `IColumnLockHost.Establish` takes the
  hold it `supersedes`; the OFD host adds the new byte to the same description (the test never reports the asker's own
  locks), the sidecar host publishes a second holder file and skips both names in the test, and the old column goes only when
  the request is admitted. A refusal changes nothing. `RunUnitFileLock.Revert` publishes the old column again, untested, when
  the OPEN is unsuccessful. `RetakeKeptFileLock` and `RunUnitFileLock.Retake` are deleted, with their `FileConnector` calls.

**Found in the same files.** `OPEN OUTPUT` of a connector whose CLOSE kept its handle answered '61' on Windows: the OUTPUT
probe (`HostFile.IsHeldByAnother`) counted the connector's own kept handle as "another file connector" (§9.1.13.9 1) e)).
`FileConnector.HeldByAnotherHandle` is the override point; the keyed connector gives the handle back for the question and
takes it again when another handle does hold the file. PB3481 R3-2 (the retake witness cannot fail) is answered: the
witness now re-OPENs INPUT under ALL OTHER and asks a READ ONLY I-O OPEN, which only the restored column refuses on a host
that carries the column lock.

**Goldens.** None: the facility is unreachable from source until PB3338, so the registry calls are the witnesses (all in
Unit). `CommitRollbackRuntimeTests`: `A_reopen_refused_by_another_run_unit_keeps_the_kept_handle_and_its_transaction` and
`An_unsuccessful_reopen_after_the_handle_swap_gives_the_kept_handle_back` (both fail with the retake removed, '23' for '51'),
`A_reopen_output_is_not_refused_by_the_handle_its_own_close_kept`, `A_reopen_output_refused_by_another_run_unit_keeps_the_kept_state`,
and the reshaped `An_unsuccessful_reopen_leaves_the_kept_file_lock_standing`. `RunUnitFileLockAmendTests`, both column hosts:
a racing run unit is sent in at the entry of every establishment, and the control shows it gets in once the lock is given back.

**Rows.** None claimed, none closed (`closes_rows: []` with its reason). Not fixed here, registered in the report: DELETE FILE
after a CLOSE under commit control is refused '62' by the connector's own kept handle (`FileRegistry.cs`, the
`HostFile.IsHeldByAnother` arm of the DELETE FILE retry loop), which needs a decision on what DELETE FILE does to a standing
transaction.

