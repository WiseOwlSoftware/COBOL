## Entry 2701 — 2026-10-28 12:20 PDT — The impact plan keeps a method-level reach beside a project-wide file, skips an unseen test file, and survives a prune race

Three defects of the gate's impact-map planner (`scripts/spec/impacted_tests.py`, `scripts/spec/impact_store.py`,
`scripts/gate_plan.py`), fixed together because they share the planner (kb/Work PB3806, PB3991, PB3807; cluster PB1957).
Process tooling only: no ISO rule governs them and no inventory row is claimed.

**PB3991 (the one that mattered).** PB3869's fix bounced in the merge queue on 12 CCVS85 NOT-only SIZE ERROR programs the
local leg 1 never ran. c8afacb68 had fixed one cause (a declaration hunk now keeps the method hunks' direct hits). The
next branch's review found the second: that branch also bumped `Directory.Build.props`, a structural input, which adds an
`every_tier1` reason, and `impacted_tests.tiers` answered every_tier1 by returning tier 1 for every test, erasing the
method-level reach of the emitter change beside it. `gate_plan.tier_of` already puts every case in tier 1 for that
reason, so the rule was written twice and the copy in `tiers` threw information away. Re-probe: `Directory.Build.props`
plus `A.cs` gave `{M1: 1, M2: 1, M3: 1}`. Fix: `tiers` reports what the change reaches whatever the reasons say (the
override is deleted, the plan keeps its own), and `plan_assembly` ranks tier 1 by that reach (`KeyGroup.rank`: 1 direct,
2 widened, 3 none) before cost, in the budget loop and in the leg order. A plan with no reach to rank by (a grammar change
alone) is unchanged: every unit has rank 3 (a key the map does not hold included: review R1-2) and the order stays
cheapest first. Review R1-1 found the budget loop still ended on the first group that did not fit, sound only while the
order was by cost alone: a reached case dearer than the remaining budget left leg 1 with no tier-1 case at all. The loop
now skips a group that does not fit and goes on, and a self-test arm pins it (as one for R1-2 does). Pins: `impacted_tests --self-test` (a
props file beside a mapped file keeps the file's tiers; a method-level hit stays tier 1) and `gate_plan --self-test` (a
dear reached case leads leg 1 ahead of 100 cheaper unreached ones under every_tier1).

**PB3806.** `reach_cs` ran the drift arm before the `TEST_PROJECTS` guard, so a test file added on main after the map's
commit (in `drift`, unknown to the map) and edited by the branch put every test in tier 1. Re-probe: the reason "differs
between the map's commit and the base, and the map has never seen it". Fix: the guard for a test-project file with no
recorded ranges is the first statement, one condition, one place; its tests are tier 0a through `reach_test_file`. A
source file in the same position is still unattributable (pinned both ways).

**PB3807.** `impact_store.maps` stat'ed every globbed map with no guard, so a map the lane worker pruned between the glob
and the stat raised `FileNotFoundError` out of `nearest` and `find_map`, and the gate died planning. Fix: one helper,
`mtime` (None when the path is gone), used by `maps`; `stale` builds on it for `prune`'s temporary files and
`clean_work`'s directories. Sweep: the other glob-then-stat pairs in `scripts/spec/impact_*.py`,
`record_impact_map.py` and `scripts/orchestrator/impact_lane.py` are these four; the remaining globs (`plan.json` in
`impact_lane.measure`, the recorder's own raw directory, the test-source `rglob`s) read the file under `except OSError`
or are the recorder's own files. Pins: `impact_store --self-test` removes a globbed map, a work directory and a
temporary file between the glob and the stat, and `impacted_tests --self-test` plans through a store whose only map
vanishes (unaimed, with the cause printed). Both go through one fault, `impact_store.vanished_after_the_glob`.

**The merge-queue bounce (PR #734, run 40754265548).** The first pins patched `Path.stat` alone and read `exists()` on
the vanished path inside the patch. CI's Windows self-test job then ran Python 3.12, where `Path.exists`, `is_file` and
`is_dir` call `Path.stat` (3.12 `pathlib.py`: `exists` calls `self.stat(...)`); 3.14 and later call `os.path.exists`.
So the arm's own read hit the stub, and two arms failed on 3.12 only (`tidy skips a work directory …`, `prune skips a
temporary file …`: `exists()` False). Reproduced with uv's CPython 3.12.16 on this host. Fix: the fault makes a path
read gone through all five calls (`stat`, `lstat`, `exists`, `is_file`, `is_dir`), so every Python sees the same thing,
and the arms read what the code left on disk only after the block. A new arm pins that the calls agree on the
running Python (`mtime`, `exists`, `is_file`, its directory's `is_dir`). The two self-tests are green on 3.12.16 and 3.15.2. Sweep: of the other self-test stubs on a pathlib
or os method (`Path.open` in `prune_worktrees.py` and `review_findings.py`, `Path.rename` in `queue_facts.py`,
`Path.glob` in `test_plan_wave.py`, `os.replace` in `sharedfile.py`), none routes differently between 3.12 and 3.15:
`read_text` and `write_*` call `self.open` in both, `rename` and `replace` call `os`, and `load_reports` calls `glob`,
not `rglob` (3.15 implements `rglob` through `glob`, 3.12 does not). The CI side of the gap, CI running a different Python
from the hosts, was closed on main by PB4007 (the toolchain manifest's Python 3.15.2 in every job).

Docs: DESIGN-test-build-ci section 3.13 and the `gate_plan.py` docstring state the rule; DOC_INDEX line updated.

