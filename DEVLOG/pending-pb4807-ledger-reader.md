## kb/Work PB4807: the ledger's Pacing section is a second stale-reading reader

The operator found that ledger v325's Pacing section showed account 2 at 45 % (a reading from 2026-11-02) while `budget.py` read 88.2 % from the live anchor. The ledger's pacing and measured burn come from `ledger_plan.py`'s `measure_pacing` and `estimate_inputs`, which read the stored `readings.json`. That is the same root as PB4807 (a fresh reading is never stored), so both members are added to its Sites; no code changes here.
