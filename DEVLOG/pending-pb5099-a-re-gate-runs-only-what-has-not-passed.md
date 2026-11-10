## PB5099: a re-gate of a measured code state runs only what has not passed

**Mechanism.** The gate's reuse rule answered a re-gate only from a GREEN record at the same tree and base. Wave 20261972's
gate of tree fc15c9ace5 went RED on one harness non-observation (`ArchReviewFleetDriftTests.EachFleetInstrument_PassesItsSelfTest`)
among about 63,000 cases, and the re-gate at the same tree ran all 63,000 again for 58 minutes. Mythos hit the sibling case: a
GREEN code-lane run, then a review fix that touched only `*.md` and `kb/`, re-ran everything. THE RULE of 2026-10-31 asks that
every case ran green at the pushed head under one toolchain, not in one run.

**Re-probe.** Reproduced by reading both runs' `verdict.json` (tree = head_tree = tree_end) and by the code: `Gate.reusable`
admits only exit 0, and nothing carried a pass from a RED run. Built the solution (0/0) and probed the new hook on the real hosts.

**Fix.** `Gate.carry` composes with earlier runs of the same code state: the same tree, or one that differs only in paths
`inert_lane.py` calls inert (`Host.code_state_diff`), under the same toolchain, full lane, tree unchanged while the run ran. A GREEN
earlier run answers with the audits alone (`GREEN — COMPOSED`, no corpus fetch, slot, build or host); a RED one's passes are
carried (`test_population.passed_cases`): the gate lists the population, writes the names the passes do not cover to
`<run>/only-<assembly>.txt`, and the host's retry hook, `GateOnly` in `tests/_shared/GateLegs.cs` (`WISEOWL_COBOL_GATE_ONLY`, beside
the order and shard hooks), runs exactly those cases. `test_population.check(carried=)` composes the rest and keeps NEVER RAN
honest (a carried name the retry repeats is judged by the retry; a carry the listing never printed is dropped). The record
names `composed_from` and `carried`; `gate.measuring_runs` is the one resolution of the runs that hold a verdict's evidence, and
the landing lane's merge-queue red classification reads through it; `perf_baseline` refuses a composed record and a composed
gate writes no growth sample. A tree that changed in any path a test may read runs everything. Design: DESIGN-test-build-ci.md
§3.14.12 item 5; the gate skill says it.

**Evidence.** On the built hosts the hook ran exactly the named cases of Characterization and Unit, including names with escaped
backslashes and quotes, `···`-truncated arguments and a `file_census_notes.py` theory row, and the run's trx definitions equalled
the file's names. Self-tests: gate.py 88 arms (13 new: a RED re-gate runs only the red case, still RED, a third gate composes, a
changed code path runs all, another toolchain and a tree that moved while running contribute nothing, a GREEN run plus an inert-only
commit composes with no host, composed GREEN reused, a chain of inert commits, audits RED, a RED run plus an inert commit), 
test_population.py 42 arms (3 new), land_lane.py (a composed record read through both runs), perf_baseline.py, and the C# unit test
`Only_RunsExactlyTheNamedCases_AndRefusesAFileThatNamesNone`.

**Not changed.** The slot wait before a retry (1,448 s in the note) belongs to the gate cap. The old arm that expected another base
to force a re-run of the same tree now expects COMPOSED: a base only decides the lane, and a landing's merged main is another tree.

## Review round 1

R1-1 (blocking, fixed): a host that went RED with every trx case Passed (cleanup failure, crash after the last result) left no case to retry, and the composition turned its RED into GREEN. `Gate.carry` now records `red_hosts` and `_retry` runs such an assembly whole again when the carried passes leave nothing, carrying none of its passes. R1-2 (lead, fixed in the same file): `_prune` never deletes a run the verdict composes with (`composed_from`). Two new gate self-test arms (90 total).
