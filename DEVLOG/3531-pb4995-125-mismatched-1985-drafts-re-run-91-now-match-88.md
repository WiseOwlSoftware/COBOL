## Entry 3531 — 2026-11-12 12:00 PST — PB4995: 125 mismatched 1985 drafts re-run, 91 now match, 88 placed and 93 test-refs recorded (1985 GAP 443, unchanged)

A locator found 125 open 1985 rows whose only draft had last run MISMATCH: 95 drafts from the audit run and 30 confirmation redrafts. Many 1985 defect fixes had landed since those runs. `run_drafts.py` judged the 125 drafts, unchanged, on origin/main 5237495315 (assemblies dbff09b2a8fc) and reported 91 MATCH and 34 MISMATCH. The drafts ran from a copy of their own, so the shared `state-route` run record was not overwritten.

Three of the 91 rules already had a test, written by a defect fix. `place_witnesses.py` placed the other 88 as DRAFTED records: 17 output tests and 71 refusals, in each rule's own `cases/<clause>@1985-<Name>/` folder. No draft's expectation was changed. Each refusal's diagnostic at its violating line was read, and each names the rule's own condition or the edition gate for the construct.

`record_verdicts.py --edition 1985` recorded 93 witness-only `test-ref`s: the 88 new tests and five tests that defect fixes had placed without recording one (`1985-SR-XII.2.3-6`, `1985-SR-XII.3.3-1`, `1985-GR-XII.2.4-12`, `1985-SR-XIV.3.2.3-4`, `1985-SR-XIV.3.3.3-4`). The 1985 GAP stays 443, because a witness alone closes no row.

The 34 drafts that still mismatch were not adjudicated. 23 have an open note: 22 notes name the rule and PB5067 covers the other. 11 have no note, and one of those, `1985-SR-XIII.3.9.3-6`, is a possible under-rejection. The lists are in `E:/COBOL-coord/scratch/rerun-85g/` and in `kb/Work/PB4995/2026-11-12-rerun-85g.md`.

