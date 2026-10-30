## Entry 2805 — 2026-10-30 13:29 PDT — The landing pipeline's single-point bottlenecks, measured and filed as one campaign (PB4147)

The owner asked to fix every single-point bottleneck (2026-10-30). The landing lane's attempt logs (821) and
`landing_measure.py summary` since 2026-10-28 (112 landings) show where landings wait.

- **One shared file serializes the merge queue (PB4147, the campaign lead).** The merge queue merges without the
  repository's merge drivers, so `enqueue.py` admits one open pull request per driver-merged file (kb/Work PB3642).
  `conformance-suite/wiseowl/diagnostics.json`, the one JSON object that holds all 3,055 WiseOwl refusals, caused 61 of
  the roughly 79 holds, because almost every fix branch adds a refusal. The fix is the DEVLOG's: one file per entry, so
  different refusals never share a file and need no driver.
- **The traceability inventory is the second (PB4148): 10 holds.** Same cause, same principle.
- **The merge queue's build width (PB3773, now in the campaign):** `max_entries_to_build` is 5, sized for the Free plan's
  20 concurrent jobs; the organization has 60.

Measured alongside: review CLEAN to enqueued has a median of 7 min and a p90 of 58. Enqueued to merged has a median of
21 min and a p90 of 61, against the owner's 30- and 15-minute bars (PB3638). Local gates waited a mean 3.8 min for one
of the 3 gate slots (147 gates, 20 over 10 min). The cap is the owner's (PB2514). The conformance suite's partition
count is PB4146, which the Mythos lane is running.

