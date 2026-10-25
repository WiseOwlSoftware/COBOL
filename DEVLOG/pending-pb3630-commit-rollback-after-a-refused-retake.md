## PB3630 — COMMIT and ROLLBACK reach a kept file whose retake the medium refused

A CLOSE under commit control keeps the connector's handle, its Table 19 file lock, the transaction's liveness byte and the
record locks (§9.1.15, §14.9.6.4 GR9, §14.9.7.4 GR4). A re-OPEN that the share mode refuses gives the kept handle back to take
a new one, and when that OPEN is unsuccessful it takes the kept handle again (`KeyedConnector.RetakeKeptHandle`). PB3497 made
that retake keep the handle and its holdings even when the medium refuses the retake's read of the store, so the store ends up
with a writable handle attached and no file. The PB3497 witness measured the remaining wrong answer: a ROLLBACK (or a COMMIT)
run before the next OPEN answered status `30` with EC-I-O-PERMANENT-ERROR on a healthy medium, because `KeyedStore.Commit` and
`KeyedStore.Rollback` refuse a store that has no writable file.

Re-probe: the new theory `A_commit_or_rollback_after_a_refused_retake_reaches_the_file_the_close_kept` failed on the unfixed tree
as the note says (six of its eight rows: ROLLBACK and COMMIT answered `F`, and the file stayed locked).

Fix at the root: `KeyedStore.AdoptAgainForTransaction` adopts the file again through the store's writable handle, under the store
mutex the caller already holds, before COMMIT's promotion and ROLLBACK's restore. It does nothing while the store holds its file,
holds no transaction, has no writable handle (the refusal both statements already answer) or the medium holds no bytes. If the
medium still refuses the read, the statement answers `30` as any refused statement does (§9.1.13.6 item 1). `Open` and the new
step share one `Adopt`. Both callers that reach the store (`KeptFile` for a closed connector, `KeyedConnector` for an open one)
pass the holdings order the adoption needs. DESIGN-runtime-library.md 2.2.1.10 names the step.

Goldens: the theory runs both organizations, COMMIT and ROLLBACK, with a healthy medium and with the medium still refusing
(a refused COMMIT leaves the kept state for the ROLLBACK that follows, §14.6.13.1.3 item 8; a refused ROLLBACK ends it and
leaves the transaction to the dead-owner step, which another run unit's writable OPEN then runs). It asserts the record locks and
the file lock are gone, the changes are committed or restored, and nothing is left standing. Rows re-verdicted: none (the note
claims none).
