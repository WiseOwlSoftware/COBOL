## Entry 2674 — 2026-10-27 15:40 PDT — PB3972 files PB3838's tier 5, and PB3838 waits on the owner for tier 6

The Mythos lane handed PB3838's tier 5 to the operator (mailbox 20261027-150609, the owner: "implement the plan"). The
morning triage of every differential-fuzz night ran from tooling in that session's TEMP scratchpad, copied and
retargeted by hand each night, and its GNUCOBOL-SIDE verdicts filed nothing. PB3972 records the gap and the fix: a design
for the fuzz harness, which no doc covers today; one tool in the tree that takes the run directory and the judge as
arguments, with its triage Workflow beside it; and a GNUCOBOL-SIDE verdict that writes an `upstream` note to PB3838's
bar. Night 6's tooling is preserved at `E:/COBOL-coord/scratch/op/fuzz-triage-src/`.

PB3838 itself now has status `owner`. Tiers 1 to 4 are on main (#696, #705), tier 5 is PB3972, and the rest is tier 6,
the filing upstream, which the owner approves text by text. While PB3838 stayed `open`, `plan_wave.py --cluster PB3838`
planned the lead itself a fourth time, ahead of PB3972.

