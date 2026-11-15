## Entry 3631 — 2026-11-14 17:30 PST — The shard weights regenerated, and the spread report now says STALE when the population outgrows them (kb/Work PB5552)

The nightly of 2026-11-14 went red on `shard-spread` at 1.54x (run 43603799163, Linux shard 2 at 445 s against a mean
of 288 s). PB5468 had predicted it. I downloaded the eight runs the report read and read every trx per class. A port
of `TestWeights.Deal` reproduces each run's assignment exactly. The weights file of 2026-11-04 did not name 112 of the
1,162 classes, and the deal places those at its 0.1 s default after every named class. They cost about 190 s a run,
and about 150 s of that landed on shard 2 in every run: `CommunicationCcvsTests` (92 s) and
`ParseRecoveryCascadeTests` (about 30 s). Filling in only those classes takes the worst simulated Linux spread from
1.40x to 1.24x. The full regeneration takes it to 1.14x (Windows: 1.47x to 1.11x), so the drift in named classes'
costs accounts for the rest.

`shard_weights.py generate` rewrote the file from the three newest merge-group runs: 1,163 classes, and 29 retired
classes dropped. The pattern was that a stale file gave no sign until the spread, which is noisy wall-clock time,
crossed 1.5x. So the report now also measures the blind work directly: the seconds spent in classes the file does not
name, over the mean shard's class seconds, with the heaviest such class named. Above 0.10 the report says STALE and
exits 1. The eight runs read 0.18 to 0.25 against the old file and 0.00 against the new one. A run from 2026-11-07
already read 0.17 and 0.32, a week before the spread went red. The self-test plants the STALE arms, and two mutations
each fail them.

