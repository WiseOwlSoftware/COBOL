## PB3497, PB3501: a refused retake keeps the kept handle's holdings, and the two-attempt re-OPEN over a kept handle is witnessed

Two review leads on the keyed connectors' re-OPEN under commit control, both a missing witness in
`CommitRollbackRuntimeTests`; one of them showed a defect.

**PB3497, the not-retaken arm of `KeyedConnector.RetakeKeptHandle`.** The note said the arm needed a host race. It does not: the
give-back of a kept handle detaches the store's last handle, so the store has no file, and the retake's `Attach` reads the
store's pages again; a medium that refuses reads after the OPEN's own refusal (the PB3483 witness's `RefusingPageMedium`, kept
armed through `AbandonOpen`) fails that attach. Measured before the fix (Windows, both keyed organizations): an APPLY COMMIT
connector READs record 1, REWRITEs record 7, is CLOSEd; its re-OPEN I-O under SHARING WITH NO OTHER takes a handle the kept share
mode refuses, the kept handle is given back, the medium refuses the adoption ('30'), the retake fails, and the kept state has no
handle. Another run unit then READs record 1 with '00' (the record lock the CLOSE kept, §14.9.6.4 GR9 / §9.1.16, is
unpublished) and its writable OPEN rolls the live transaction back (the liveness byte is free; record 7 reads '23'): a wrong
answer, not only a missing pin.

Fix: the attach's two host-level steps, the liveness byte moved to the new handle and the record locks rehomed to its endpoint,
read no page, so they are one method (`CarryKeptHoldings`) and `RetakeKeptHandle` runs it whether or not the adoption of the
store succeeded; the handle stays in the kept state either way. A retake the HOST refuses (the window the design names) still
leaves the kept state without its handle. `HeldByAnotherHandle`, the OPEN OUTPUT probe, is the retake's other caller and gets the
same fix; design 2.2.1.10 says so. Witnesses (both organizations, red before and green after):
`A_reopen_whose_kept_handle_cannot_adopt_the_store_again_leaves_what_the_close_kept_standing` and
`A_reopen_output_refused_by_another_run_unit_with_the_medium_refusing_the_retake_keeps_the_kept_state`.

**PB3501, the two-attempt `FileConnector.Open` over a kept record-lock endpoint.** The behaviour was already right; the case was
missing. `A_two_attempt_reopen_over_a_kept_handle_keeps_the_record_locks_it_was_closed_with` builds the lead's sequence (run unit
A owes writes through an INPUT re-OPEN; run unit B under commit control and SHARING WITH READ ONLY OPENs I-O beside it, READs
record 2, REWRITEs record 3, CLOSEs, re-OPENs INPUT) and counts the host's column requests with `ColumnRequestLog` (replace,
restore, replace on a host with mandatory share modes, one elsewhere). A third run unit meets '51' on both records and '00' on an
untouched one; B's COMMIT frees them. The lead's expected value is refined: record 3 answers '51' by the transaction's holding
alone, so record 2 (only READ) witnesses the record-lock endpoint, and the third run unit must take part in record locking.
Mutation-checked (the rehome removed; the retake removed).

Rows re-verdicted: none (both notes claim no inventory row). Found and not fixed: after the refused retake the kept state has a
handle but its store has no file, so the program's own ROLLBACK or COMMIT before the connector's next OPEN is refused '30' though
the medium answers again; the transaction itself is safe. Registered as a lead.
