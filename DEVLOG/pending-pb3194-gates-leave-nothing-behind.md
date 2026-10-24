## PB3194: the Linux gate and the script self-tests leave nothing behind

The owner asked why the Linux filesystem exhaustion was not fixed at its root when it first happened. PB3194 had been
open since 2026-10-15: `scripts/linux-gate.sh` cloned every gated tree into `~/linux-gate/<tree>` (about 1 GB and
80,000 inodes once built) with a `<tree>.guard-tmp` beside it, and removed them only when the same tree was gated
again. Worktrees come and go, so the clones piled up: 345 filled the WSL disk on 2026-10-15, 430 exhausted its inodes
on 2026-10-19 and 394 did it again on 2026-10-22, and each time every Linux gate on the host ended NOT RUN until
someone deleted clones by hand. The re-probe tonight found 42 entries and 15 % of the inodes in use.

One mechanism in `linux-gate.sh` now owns the gate's footprint. Each gate works in its own directory,
`~/linux-gate/<tree>.<pid>/`, holding the clone, the guard leg's scratch and an `owner` file (pid and start time). The
EXIT trap removes it after any verdict, and `on_signal` handles HUP, INT and TERM by ending the legs' process trees
first and then removing it. The one build now runs in the background, because bash defers a trap until a foreground
command returns. The guard's forensics go to `TestResults/linux-gate/guard-forensics`, so nothing worth keeping lives
in the clone. A gate killed outright runs no trap, so every gate first reaps: a directory whose owner has ended goes at
once, and a pre-mechanism one goes when it is this tree's or older than 3 hours. Then the gate refuses with
`NOT RUN [disk]` below 1,000,000 free inodes or 20 GB free. `disk` is a new ENVIRONMENT cause in
`run_gate_legs.NOT_RUN_CAUSES`, so a full disk is reported before any leg starts instead of showing up as thousands of
failed tests. The self-test drives every exit path on real directories and processes (status 0, 1 and 2, and a TERM
with a background leg and its child), the reaping rules and both bounds. Each of eight mutations (no EXIT trap, no
signal trap, no kill, no descendant walk, either bound, no liveness test, no age rule) turns it red under WSL. The
branch's own Linux gate reaped 16 leftovers, ran every leg GREEN and removed its own directory: 42 entries became 26,
inodes in use fell from 15 % to 10 %, and used space from 45 G to 36 G.

The sibling sweep (every script that clones or makes scratch under WSL or `%TEMP%`) found the same leak on Windows.
Three self-tests never removed their scratch. The rest called `shutil.rmtree(..., ignore_errors=True)`, which on
Windows cannot delete the read-only files git writes its objects as, and says nothing about it. `E:\Temp` held 7,359
such directories (0.75 GB). `scripts/scratch_repo.py` gains `scratch_dir`, which removes the directory at process exit
however the process ends, and `remove_tree`, which makes a refused entry writable, removes it again and prints what
still cannot go. Its audit gains a SCRATCH arm, run by `SelfTestFixtureDriftTests`, and its 42 findings across 24 files
are converted. Three hand-written read-only walks became redundant and were deleted.
`gnucobol_differential.py`'s per-run work directory and `member_index.py --record`'s are removed now too. All 25
touched self-tests pass with a private temp directory that is empty afterwards. The 7,359 leftovers were removed.
