## Register: PB3326 folded into PB3130, two more PB3124 instances, the ledger trend points

PB3326 (47 doubled `/// <summary>` blocks under `src/`) was the same mechanism as the older open PB3130 (two `<summary>` elements in one `///` block). PB3326 is now `status: retired` with `closes_rows: []` and a `closes_rows_reason` naming PB3130. PB3130 carries PB3326's measurement and site list verbatim, plus wave 1106 A's finisher measurement (about 56 stacked-summary runs on main, scanner `stacked_summaries.py`).

PB3124 gained a dated paragraph recording two more records that `landing_measure.py complete` refused: PR #139 (2 bounce causes against 1 bounce) and PR #162 (1 bounce cause against 0 bounces). Both records stay incomplete until the defect is fixed.

`gen_ledger.py` was run and appended new points to `docs/rearchitecture/evidence/ledger-trend.json`.
