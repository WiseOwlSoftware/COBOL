## Entry 2699 — 2026-10-28 11:51 PDT — PB3825: a branch that changes a new driver's files is refused (a new merge driver lands alone, mechanically)

PB3727's landing added the `cobol-keyed-publicapi` driver and had sorted both PublicAPI baselines in the same branch. The
landing merges main in through MAIN's registry, which had no such driver, so every later main change to a baseline was a
line-merge conflict and the pull request bounced. The design said "a new driver lands alone" and nothing enforced it
(`merge_drivers.py` did not contain the phrase or any check for it).

Re-probe: the rule had no code. Fix: `merge_drivers.new_driver_files` reads origin/main's `.gitattributes`; a DRIVERS pattern
it does not give its driver is new, and a file `merge-base(HEAD, origin/main)..HEAD` changed that such a pattern governs, whose
bytes are not main's, is a finding that names the file, the driver and `git checkout origin/main -- <file>`. `--check` reports
it and `MergeDriverDriftTests.TheDriverRegistry_AgreesWithGitattributes` runs `--check` in every gate. A checkout with no
origin/main (CI's) or no merge base prints `NOTE ... NOT CHECKED` with the reason and does not claim a pass. Five self-test
arms cover the refusal, the byte-for-byte pass, a driver main already assigns, a main that moved since the cut, and the
missing ref. Replaying the check with the commit before PB3727 as main flags both baselines of both runtime projects and the
ABI pin, the bounce's exact shape. DESIGN-orchestrator-loop.md 16.9 and DESIGN-test-build-ci.md 3.17.2 name the check.
No inventory rows (process tooling).

Branch review round 1. (R1-1, blocking) The Linux half of the gate read a different main: a `--shared` clone's `origin/main` is the
source repository's local `main`, which nothing advances, so the test leg's `--check` in the clone could judge a branch differently
from the Windows half. `linux-gate.sh`'s `pin_origin_main` sets both clones' `origin/main` to the tree's sha (deleting it where the
tree has none, so the check says NOT CHECKED); three self-test checks cover the stale ref, the pin and the absent ref. (R1-2, lead)
A pattern is NEW only when the merge base's attributes did not give it that driver either, so a branch cut before main re-pointed a
driver is no longer refused as if it added one; a sixth `new driver:` self-test arm covers it.

