## Entry 3111 — 2026-11-04 15:10 PST — A bounce on a cancelled merge-group run leaves its landing record open for good (PB4655)

The enqueue unit's step 6b printed a refusal for PR #1101 from `landing_measure.py complete`. Its record correctly holds
one bounce with cause `infrastructure`, but `complete` counts only `failure` and `timed_out` runs, so the bounce on
cancelled run 41779240040 reads as 0 and `record` refuses the patch. The record never gains its merge time, and every
later `complete` repeats the refusal and exits 2. This files the note; the fix is not in this change.

