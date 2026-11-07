## PB4888: the fix round's time is in its gates; the owner approved two levers

The operator split the ten review fix rounds of wave 20261887 into phases (scratch `throughput/breakdown_fix_cycle.py`).
A round took a median of 40 minutes from FINDINGS to the next review. The gates took 86 % of that: 13.7 minutes waiting
for a slot and 23.1 minutes building and testing. The finisher's own work took 4.7 minutes. Half of the 22 gate launches
were RED reruns.

The owner approved both levers on 2026-11-07, and kb/Work PB4888 now records them:
1. A review finisher runs the targeted `dotnet test --filter` before it launches the gate.
2. A new gate class lets a fix round's re-gate go ahead of a new group's first gate. The cap stays at three.

The note also lists the records the next measurement needs: when a gate took its slot, the test time of a RED run, and
gate logs that a later gate does not overwrite. No code changes in this entry.
