## Entry 2588 — 2026-10-25 17:02 PDT — landing_measure: a re-enqueue after a re-review no longer writes a record its own validator refuses (PB3710)

Mechanism. `record_enqueued` bounded `stage_times` by this call's enqueue time, while `patch_record` keeps the record's FIRST `enqueued`. Sequence: enqueued 09:00 with CLEAN 08:40, a bounce, a review finisher, CLEAN again at 10:00, re-enqueued 10:10. The new CLEAN (10:00) passed the 10:10 bound, `patch_record` kept `enqueued` 09:00, and `validate` refused enqueued-before-review_clean; `queue()` printed "landing record was NOT written" and the update was lost.

Re-probe. `record` with that sequence raised the refusal on this build.

Fix. `stage_times` takes the pull request's existing record (`record_enqueued` reads it) and judges the order against what `patch_record` will hold: the first `enqueued`, the earliest `done`, and the `review_clean` already carried (the dispatched bound uses it too). The re-review, which follows the first enqueue, stays out of the record; the first CLEAN stands. Sibling arm in the same overlay: `patch_record` let a later `dispatched` (a finisher's) replace the first; it now moves only earlier, as `done` does. DESIGN-test-build-ci.md section 3.17.5 states both.

Review round 1 (blocking R1-1). The sibling arm for DONE was missed: `record_enqueued` still bounded `done_times` by this call's time, so a branch first enqueued with no DONE report, bounced and finished afterwards, then re-enqueued, wrote `done` 10:00 after the kept `enqueued` 09:40 and `validate` refused the record. `done_times` is now bounded by the record's first `enqueued` (a finish after it is no DONE of this landing, unrecorded like the late CLEAN), and the self-test stage arm carries the b170 sequence (it fails with the old bound, checked). DESIGN 3.17.5 says so.

Test. The self-test stage arm runs the sequence through `record_enqueued` (first record, a second CLEAN verdict appended, a re-enqueue), plus the `stage_times` bound and the dispatch arm; withholding `existing` makes the arm fail. No ISO rule governs this, so no golden and no inventory row.

