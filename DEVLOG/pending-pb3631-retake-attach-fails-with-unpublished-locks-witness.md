## PB3631: the retake-Attach-fails arm has a swept witness, and finding it showed a re-OPEN answering '61' beside nothing

**Note:** kb/Work PB3631 (analysis, suspected wrong answer from the 2026-11-07 audit). **Verdict:** the arm itself is correct; the witness that
reached it exposed a different wrong answer in the same step, fixed here.

**Re-probe.** Instrumenting `KeyedConnector.RetakeKeptHandle` showed why no earlier test reached the arm: the OPEN's `Attach` carries the
kept record locks to its own endpoint (`CarryKeptHoldings`) only after the adoption's page reads, and every refusal of those reads leaves
`unpublished` empty (PB3497's witnesses, the R1-2 witness). An INPUT OPEN makes a medium read after the carry only when
`SettleDeadOwnersOfTheOpen` finds a dead run unit's transaction.

**The wrong answer behind it (branch review R1-1).** That settle went through a scratch handle opened beside the OPEN's own. A re-OPEN
INPUT SHARING WITH NO OTHER of a file its own CLOSE under commit control kept (SHARING WITH ALL OTHER) had given the kept handle back for the
share-mode swap, so its own exclusive handle refused the scratch handle and the OPEN answered '61' although no other file connector had the
file open (§2023:9.1.13.9 answers '61' only for another one) and a dead run unit's uncommitted write (§2023:9.1.18.5) stood unsettled. Relative and
indexed alike. The first version of the witness pinned that '61' as expected.

**Fix.** `SettleDeadOwnersOfTheOpen` keeps the beside-the-kept-handle settle for every kept handle still on the file (adopted, or beside the
OPEN's own handle; the second review's R1-1 found the first version narrowed it to adopted ones only, and the ALL OTHER re-OPEN rows now witness the
widened form); every other OPEN (no kept state, or one whose kept handle was given back) gives its own handle back first and takes it again, as the OPEN with no kept state always did. For a kept
state the record-lock ids on the OPEN's endpoint are taken off it (`PhysicalFileTable.Unpublish`) before the handle goes and published through
the handle that takes its place; if that retake is refused they wait in `_unpublishedBySettle`, and `AbandonOpen` publishes them through the kept
handle it retakes, so the refused OPEN still leaves the locks standing (§2023:14.9.27.4 GR25, §2023:14.9.6.4 GR9). Design doc
DESIGN-runtime-library.md 2.2.1.10 updated.

**Witnesses.** `CommitRollbackRuntimeReplacedFileTests.A_reopen_that_gave_the_kept_handle_back_settles_a_dead_owner_and_succeeds` (both
organizations: '00', record 7 absent, ROLLBACK leaves the seeded file) and
`CommitRollbackRuntimeReplacedFileTests.A_reopen_refused_at_any_read_whose_retake_cannot_adopt_the_store_leaves_the_kept_record_locks_standing`
(refuse the k-th read and all later ones, every k, both organizations, with and without the dead owner; a healthy re-OPEN answers '00').
Review R1-2: the sweep now asserts the arm is reached, through `KeyedConnector.RetakesThatLostTheAdoptionHoldingLocks` (the dead-owner sweep
must reach it). Mutation: not merging `_unpublishedBySettle` in `AbandonOpen` fails the dead-owner rows (B READ 1 '00' for '51'). Review R1-3: the
note named the wrong class. Review R1-4: the `§2023:9.1.13.8 1)` form is dropped from the new text (cite.py refuses it).

**Self-review.** Architecture: the settle's handle swap is one rule in one place, `SettleDeadOwnersOfTheOpen`. Duplication: the unpublish/republish
pair is the one `AbandonOpen`/`RetakeKeptHandle` use. Performance: the settle runs only when a dead owner stands.
