## Entry 2618 — 2026-10-26 11:06 PDT — PB3834 filed: PB3707's self-tests read the real clock

The branch review of w20261703a (PB3707, PB3717, PB3724) left a low lead, R1-2: the new STARTING-state self-test arms
in `enqueue.py` and `run_gate_legs.py` name their run directories from `datetime.now()` and assert inside the real
60-second start-up window, so a loaded host can turn them red with no regression. The owner ordered it filed as a
defect (2026-10-26). PB3834 records it, blocked on PB3707's landing, with the fix shape: inject the clock through the
readers that already take `now`, and sweep every self-test in `scripts/` for the same pattern.

