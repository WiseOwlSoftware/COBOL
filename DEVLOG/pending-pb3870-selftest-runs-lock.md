## PB3870: the gate self-test read runs.lock as a run directory on Linux; it now lists runs through run_dirs

CI's merge-group run for PR #689 (PB3868) failed in `audits`: `run_gate_legs.py --self-test` raised NotADirectoryError on
`runs/runs.lock/verdict.json`. Three self-test arms took `next(iterdir())` of a run root as its run directory, but the
root also holds the `runs.lock` file that numbers runs. NTFS lists the run first, and on this box's WSL ext4 the name
hashes happened to as well (six passing runs of main's version), while CI's runner listed the lock first. The arms now
read through `run_dirs`, the one listing of a run root that the gate itself uses. A sweep found no other first-entry
`iterdir` under `scripts/` or the harness. kb/Work PB3870.
