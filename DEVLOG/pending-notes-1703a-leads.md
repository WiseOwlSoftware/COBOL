## The w20261703a review leads, filed

The branch review of w20261703a (PB3707, PB3717, PB3724) left four leads. R1-2 (self-tests on the real clock) is PB3834,
and R1-3 (the Sites paragraph) was fixed on the branch. The owner ordered the rest filed (2026-10-26). R1-4 is PB3835:
the landing lane's exit-12 wait does not read a gate that is STARTING, so it relaunches a landing into the same GATE
BUSY until the gate takes its lock; the fix makes the wait and the landing read one predicate. R1-5, a stale comment
above `SLOT_DRIVER` in `enqueue.py` that still says every landing row takes the shared cap of 3, joins PB3804, the note
for doc text that names rules the code no longer has.
