## Entry 2459 — 2026-10-24 05:52 PDT — File the wave 20261028 review leads: PB3630, PB3631, PB3632

The clerk filed three leads from the PB3497 and PB3518 reports as kb/Work notes. PB3630 is a defect: after a refused retake the
kept state has a handle but no file, so ROLLBACK and COMMIT answer status 30 on a healthy medium (measured by the PB3497
implementer). PB3631 is a test-coverage note for the retake arm where Attach fails with unpublished record-lock ids. PB3632 is the
sidecar host's exhausted publication answering Unavailable without testing refused columns. No code changed.

