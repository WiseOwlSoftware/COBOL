## PB3726: the pipeline-scripts restructure designed

The owner asked on 2026-10-25 why so many changes touch the same scripts, and handed the planning and design of the
restructure to a Mythos model, the implementation to Opus (kb/Work PB3726). This landing is the design and the plan,
and no script changes.

What was measured first: 209 of the week's 1,240 non-merge commits touched `scripts/`, concentrated in five files
(`plan_wave.py` 42 + 49 on its test file, `prune_worktrees.py` 34, `run_gate_legs.py` 30, `enqueue.py` 25,
`land_lane.py` 24) whose hottest function in every case was the one in-file `self_test()` (90, 66, 41, 35 hunks;
`plan()` 64); `docs/CONFORMANCE.md` took 93 commits, 50 of its hunks in §4 and 40 in §3; the self-tests take 152 s
(prune), 179 s (enqueue), 64 s (gate driver) and 24 s (lane) because each builds one expensive world; the
conformance record is cited about 3,600 times by section and anchor.

`docs/rearchitecture/DESIGN-orchestrator-loop.md` §16 now carries the design: three rules (one job per module, one case
per function in a world-per-file layout, no shims) each paired with a drift test; the module map of each script
naming the job each module owns, what moves there and every importer re-pointed; the `scripts/selftests/` layout with
its one collector (`selftest_cases.py`), discovered by the self-test runner's existing rule (b) and refused as an
in-file case list by `SelfTestCaseLayoutDriftTests`; the `docs/CONFORMANCE.md` split into sources under
`docs/conformance/` with the file itself a rendered view and its citations resolving unchanged by stable anchors; the
keyed driver for `PublicAPI.Unshipped.txt`; the import DAG; the order and the behavior-neutrality oracle per step.
`DESIGN-test-build-ci.md` §3.14.10 and §3.17.4 point at it in one sentence each.

The plan is twelve notes, PB3727-PB3738, cluster PB3726, tiers 1-11, each claiming its files whole: PB3637 (the UTF-8
switch, re-clustered from PB1957) and PB3727 (the PublicAPI driver) first; the plan_wave split (PB3728) beside the
CONFORMANCE split (PB3738); then the case layout with plan_wave's cases (PB3729); then, for prune_worktrees, the gate
driver, enqueue and the lane in turn, cases out (PB3730, PB3732, PB3734, PB3736) before the split by job (PB3731,
PB3733, PB3735, PB3737). One owner question is recorded in PB3738: whether the §7/§8 register tables stay keyed tables
or become one file per row.
