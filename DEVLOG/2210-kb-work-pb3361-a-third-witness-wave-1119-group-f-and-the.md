## Entry 2210 — 2026-10-18 08:21 PDT — kb/Work PB3361: a third witness (wave 1119 group F), and the trend points the wave unit's ledger render left in the main checkout (kb/Work PB2426)

Wave 1119 (campaign PB3353) returned eight groups. Seven came back reviewed CLEAN. Group F (PB1154) did not, and the
cause was not its work: its review finisher returned DONE with a GREEN leg-1 gate, but it wrote its `report` field as a
sentence instead of a path. `wf_rolling_wave.js#finishedReport` passed that sentence to `plan_wave.py --finished`, which
could not read it, so the wave counted F as SPLIT and never ran the round-2 review on the tip `c246ad70b`. The report at
the canonical path `w1119f-PB1154-report.md` passes the same test (exit 0, `FINISHED status=DONE`). This is PB3361's
third witness in three waves (1115 E, 1117 F, 1119 F), and its note now records it. The fix PB3361 already names still
stands: compute the report path from the args, the same rule `make_dispatch_specs.py` uses, and stop reading the
agent's free text for it.

The unit's ledger render (`gen_ledger.py`, from the main checkout fast-forwarded to `da2f3f8e9`) appended 210 lines of
trend points to `docs/rearchitecture/evidence/ledger-trend.json` there. This branch commits them, so the main checkout
is clean again. PB2426 stays open: until it is fixed, every render dirties the main checkout.

