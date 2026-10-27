## Entry 2644 — 2026-10-26 18:17 PDT — PB3860 filed and added to the pre-fuzz set: MEAN's quotient scale follows the receiver

PB3839's review finisher left a lead: FUNCTION MEAN computes its quotient at a scale chosen from the receiver
(`MeanScaled`: max(receiver scale, argument scale + 1, 6)), the mechanism PB3839 removed from MAX and MIN. Re-run on the
reviewed PB3839 compiler, MEAN of 1E-8 and nineteen zeros (5E-10) stores +0.00 under `COMPUTE R2 ROUNDED MODE
AWAY-FROM-ZERO`, where +0.01 is owed, and `DISPLAY FUNCTION MEAN(...)` shows 0.000000000; the wide receivers come out
right. The owner added it to the pre-fuzz campaign (PB3852, tier 2) at about 18:07 PDT, so the night's run waits for it.

PB3858 is filed beside it: two public brent-tools evals, `devlog-commit-hook` and `devlog-insert-entry`, score 0.00
with the devlog skill on the unsplit v1.22.0 plugin (PB3788's measurement); their expected outcomes were revised on
2026-10-11 and never re-run, and no note owned them.

