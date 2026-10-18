## Ledger trend: commit 38 trend points that the enqueue units' renders left in the main checkout (kb/Work PB2426)

The enqueue unit's step 7 renders the ledger from the main checkout, and `gen_ledger.py` appends the trend points it
measures to the tracked `docs/rearchitecture/evidence/ledger-trend.json` in that checkout. No landing of the unit's
own carried the file at 04:26 or 04:41 PDT, so the loop's supervisor saw a dirty tree after each and started a `resume`
unit whose only finding was that file. The 04:50 render re-appended the same 38 points: a3ffe848 (2026-10-17, GAP 56)
to c774a175 (2026-10-18, GAP 66). This branch commits them unchanged, as 765de453c did for the 2026-10-17 points, so
the main checkout's copy matches main once it lands.

kb/Work PB2426 records the recurrence and its measured cost, about one wasted unit per render that no landing carries.
Its fix is still open: either the series moves to the coordination directory, or the publish commits its own points.
