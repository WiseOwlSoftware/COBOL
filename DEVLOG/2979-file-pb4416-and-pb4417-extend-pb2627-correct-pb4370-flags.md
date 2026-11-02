## Entry 2979 — 2026-11-01 21:10 PST — File PB4416 and PB4417, extend PB2627, correct PB4370 flags

Filed PB4416 (method automatic data roots an access path on a CLR name, from the PB4380 report lead and its R1-1 review lead) and PB4417 (suspected RANGE rounding hazard behind the unbounded-argument early return, from the PB4370 report R1-1 lead). Extended PB2627 with the bind-time RuntimeMethod choice and the FunctionValueLane key space. Corrected PB4370 frontmatter to crashes true, process_only false, since its landing fixed a fatal EC-SIZE-OVERFLOW. Notes only; no code changed.


It also files PB4418: landing_measure.py complete re-counts bounces as failed merge-group runs only, so a conflict removal that the record already counts as a bounce makes complete refuse PR #963 on every run.
