## Entry 3571 — 2026-11-13 17:43 PST — PB5468: the shard-spread report measures only the population the weights deal

The nightly of 2026-11-13 went red on `shard-spread` at 1.64x (run 43430492045, Windows, shard 4 at 162 s against a
98 s mean). I downloaded the eight runs the report read and read every trx. In that run the Windows leg ran Unit alone:
the store ref held passes for Conformance and Characterization (PB5235). `TestWeights.Deal` balances each shard's sum
over all three assemblies in one pass, so Unit's share alone is uneven by design. Simulating the deal over the committed
weights gives 543 s on every Windows shard in total but 1.50x within Unit, with every weight exact. No regeneration can
fix that spread, so the red named a remedy that does not exist.

`scripts/shard_weights.py report` now records the test assemblies each shard's trx define. It leaves out, and names, an
OS of a run whose shards ran fewer than every assembly the weights file deals. A weights file that deals an assembly no
shard of the window ran is UNMEASURED, so the report cannot go blind. A window that read shard runs and measured
none of them is UNMEASURED too, as is a run whose shard artifacts hold no trx (review R1-1). Only a window with no
shard run is NOT MEASURED. The 1.5x threshold is unchanged. The self-test
replays run 43430492045: 1.33x WITHIN over the dealt three (Linux decides), 1.65x OVER over Unit alone. With the partial
branch removed, it fails.

Before: the nightly's `shard-spread` was red at 1.64x OVER. After: the same eight runs measure at most 1.48x (Linux,
run 43417801455), which is WITHIN. Linux's full spread has risen from 1.11x to 1.48x, with shard 2 slowest in every run:
the 2026-11-04 weights are aging. The note records that, and records the deal's per-assembly imbalance, which does not
lengthen the merge group today.

