## landing_measure: a re-enqueue after a re-review no longer writes a record its own validator refuses (PB3710)

Mechanism. `record_enqueued` bounded `stage_times` by this call's enqueue time, while `patch_record` keeps the record's FIRST `enqueued`. Sequence: enqueued 09:00 with CLEAN 08:40, a bounce, a review finisher, CLEAN again at 10:00, re-enqueued 10:10. The new CLEAN (10:00) passed the 10:10 bound, `patch_record` kept `enqueued` 09:00, and `validate` refused enqueued-before-review_clean; `queue()` printed "landing record was NOT written" and the update was lost.

Re-probe. `record` with that sequence raised the refusal on this build.

Fix. `stage_times` takes the pull request's existing record (`record_enqueued` reads it) and judges the order against what `patch_record` will hold: the first `enqueued`, the earliest `done`, and the `review_clean` already carried (the dispatched bound uses it too). The re-review, which follows the first enqueue, stays out of the record; the first CLEAN stands. Sibling arm in the same overlay: `patch_record` let a later `dispatched` (a finisher's) replace the first; it now moves only earlier, as `done` does. DESIGN-test-build-ci.md section 3.17.5 states both.

Test. The self-test stage arm runs the sequence through `record_enqueued` (first record, a second CLEAN verdict appended, a re-enqueue), plus the `stage_times` bound and the dispatch arm; withholding `existing` makes the arm fail. No ISO rule governs this, so no golden and no inventory row.
