## The remaining stages, split between the two lanes to run in parallel (2026-11-10)

Owner, 2026-11-10 ~22:20 PST, after stage 1 landed (PR #1484): "spread this work to you and operator using non-mythos agents
to get it done as soon as practical. Do what can be done in parallel." The Mythos session controls; every worker is Opus or
Sonnet ([[controller-mythos-workers-cheaper]] in the session's memory).

The split follows the FILES each stage edits, because two stages that edit one file cannot run at once.

| Note | Stage | What it does | Lane | Edits | Starts |
|---|---|---|---|---|---|
| PB5225-PB5227 | 3, first half | self-tests reuse their PASS: hermetic fixtures, the clock proof keyed narrowly, line endings by changed path | Mythos | `scripts/self_tests.py`, `self_test_inputs.py`, `self_test_clock.py`, `line_endings.py`, `gate.py` (the audits table) | in flight, branch `selftest-cost` |
| PB5230 | 2, first half | the repository-drift tests leave Unit for their own assembly and target; Unit's inputs are declared narrowly | Mythos | `tests/` projects, `scripts/build_targets.json`, `gate.py` (the assembly list), one CI leg | now, branch `build-stage2-drift` |
| PB5231 | 2, second half | the conformance suite's generator is a target; Conformance is keyed on the generator's OUTPUT, so a records-only change that leaves the suite unchanged re-runs neither | Mythos | `scripts/buildgraph.py` (targets with outputs and dependencies), `build_targets.json`, `conformance-suite/harness/` | now, branch `build-stage2-generator` |
| PB5232 | 3, second half | each self-test is a target with declared seconds and its own timeout; the runner reports progress as it goes (PB5222) | Mythos | `scripts/self_tests.py` | after `selftest-cost` lands (same file) |
| PB5233 | 4 | `build.py check / why / land` replaces `build-local.ps1`, the gate's orchestration and `enqueue.py land` | Mythos designs now; built after stages 2 and 3 land, by whichever lane is free | `scripts/gate.py`, `enqueue.py`, `build-local.ps1`, every doc and skill that names them | design now |
| PB5234 | 5 | the guard scripts: measure each (what it proves, its seconds, who runs it, what already proves the same), then delete or replace | Operator | `scripts/guard*.sh`, the tests that read them, their CI steps | now |
| PB5235 | 6, and stage 1's CI proof | CI and the nightly run the same graph: a CI job computes each target's key and accepts a pass recorded on the owner's machine (owner 2026-11-10); a shard fails when a test opens a tracked path outside its target's declared inputs | Operator | `.github/workflows/`, `scripts/buildgraph.py` (the store's transport only) | now |
| PB5229 | - | `budget.py` anchors on the status line's reading whatever its age | Mythos | `scripts/orchestrator/budget.py` | now, branch `fix-budget-anchor` |

**Shared files, and who yields.** `scripts/buildgraph.py`: PB5231 owns the graph (targets, keys, dependencies); PB5235 adds
only how a store record travels to CI and reads PB5231's branch before it starts on that file. `.github/workflows/`:
PB5230 adds one test leg and lands first; PB5235 merges main after it. `scripts/gate.py`: PB5230 changes the assembly list
only; PB5233 restructures it last.

**Each stage lands on its own**, fast-tracked (gate, land, review in parallel, fix forward), and states its measured before
and after in its DEVLOG entry. A stage that finds its estimate wrong corrects this table by a new addendum.
