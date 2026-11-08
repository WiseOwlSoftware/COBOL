## PB3631: the retake whose Attach fails while AbandonOpen holds unpublished record locks has a witness, and the arm is right

**Note:** kb/Work PB3631 (analysis, suspected wrong answer from the 2026-11-07 audit). **Verdict:** the arm is correct; a test-coverage gap, closed.

**Re-probe.** Instrumenting `KeyedConnector.RetakeKeptHandle` showed why no earlier test reached the arm: the OPEN's `Attach` carries the
kept record locks to its own endpoint (`CarryKeptHoldings`) only after the adoption's page reads, and every refusal of those reads leaves
`unpublished` empty (PB3497's witnesses, the R1-2 witness: the retake's Attach either succeeds or the list is empty). An INPUT OPEN
makes a medium read after the carry only when `SettleDeadOwnersOfTheOpen` finds a dead run unit's transaction; its settle through a handle
of its own is refused by the OPEN's exclusive handle (SHARING WITH NO OTHER), the OPEN answers '61', and the retake then runs.

**Witness.** `CommitRollbackRuntimeTests.A_reopen_refused_at_any_read_whose_retake_cannot_adopt_the_store_leaves_the_kept_record_locks_standing`
(Theory over both keyed organizations, without and with a dead owner D that wrote record 7): A READs record 1 under SHARING WITH ALL OTHER
and CLOSEs under commit control; the re-OPEN INPUT SHARING WITH NO OTHER meets a medium that refuses the k-th read and all later ones, for
every k up to the reads a healthy re-OPEN makes. For every k, whatever the OPEN answers, another connector of the same run unit and another
run unit READ record 1 '51' and record 2 '00' (§2023:9.1.16, §2023:9.1.13.8 1)), and after A's ROLLBACK (§2023:14.9.36.4 GR3) record 1 reads
'00' and the file is the seeded one. The sweep reached the arm 21 times (retake Attach failed, one unpublished lock). With
`PhysicalFileTable.Republish` removed the witness fails (B READ 1 '00'), so the arm is pinned. No runtime code changed.

**Self-review.** Architecture, duplication, performance: test-only change, no new mechanism. The sweep reuses `RefusingPageMedium` and
`DyingPageMedium`; the "refuse from the k-th read" shape is the one `IndexedEngineTests` uses for refused OPEN rows.
