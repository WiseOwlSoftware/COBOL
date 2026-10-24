## PB3584: every script self-test builds its scratch git repositories through one fixture, scripts/scratch_repo.py

**What was wrong.** Fifteen script self-tests created their own scratch git repositories and seven carried a git
helper of their own (devlog.py `sh`, prune_worktrees.py `g`, rendered_views.py `git`, run_gate_legs.py's two,
enqueue.py `sh`, landing_check.py `sh`, test_plan_wave.py's `hgit`), each with its own identity, its own checking (some
raised with git's message, some with none, some never checked) and its own environment handling. A change to how a
self-test drives git reached one helper and left the others behind: run by hand outside scripts/self_tests.py (whose
private global config turns automatic maintenance off), the prune_worktrees.py self-test started 74 background
`git maintenance` processes in the repository it was still building.

**Re-probe.** The new fixture's audit, run on the base tree (5e20e0801), found 65 places in 19 files: the note's list
less self_test_inputs.py (its `"init"` names a git subcommand, there is no scratch repository) and less linux-gate.sh
(shell; it clones), plus five siblings the note had not listed: scripts/arch/file_census_notes.py, member_index.py,
r2_subsystems.py, scripts/spec/audit_evidence_supersession.py and gen_ledger.py. Two more copies of the fixture's rules
sat beside them: gate_slot.drop_git_local_env (also imported by run_gate_legs.py) and self_tests.py's own
`_git_local_env_vars`/`_scrubbed_env` and `GIT_IDENTITY`/`GIT_NO_AUTO_MAINTENANCE`.

**The fix.** `scripts/scratch_repo.py` is the one place a self-test's git process starts. `ScratchRepo.init`/`clone`
append the identity, `maintenance.auto=false`, `gc.auto=0` and no commit signing to the new repository's own config
file (no extra git process), so the code under test's git commands read them too, whoever runs the self-test.
`origin`, `worktree`, `write` and `commit` (with a `when` that dates a commit) cover every repository shape the
self-tests build; `git` raises ScratchRepoError with git's message, `run` is the unchecked form for a command whose
failure is the case; every command runs without git's repository-selection variables, and `isolate()` drops them from
a process whose code under test runs git itself. All 19 files moved onto it, every local helper deleted (no forwarder),
gate_slot.drop_git_local_env and the runner's copies deleted; the runner's private global config is now written from
the fixture's constants through `config_text`, so the two cannot drift. test_checkpoint.py and test_ledger_state.py
also stop leaking their temp directories. No self-test builds the same repository for more than one case, so the
note's build-once-and-copy suggestion had no site.

**The guard.** `scratch_repo.py --audit` reads every self-test's syntax tree (a function named for a self-test, a
standalone `test_*.py`) for SPAWN (a `subprocess` call whose argv is a git argv or a name bound to one), INIT (a
repository created around the fixture, also in top-level functions only self-test code reaches) and HELPER (a nested
helper over the module's own git runner); it is GREEN on the tree. The new `SelfTestFixtureDriftTests` (Unit, tree
sweep) runs the audit, holds the fixture self-test's arms from source, adds a text net for `"git"` and `"init"` on one
line, and holds `--local-env-vars`, the identity and the maintenance tuple to the fixture alone.
`SelfTestDiscoveryDriftTests.LiveTreeReaders` and live_tools.py's docstring name the fourth reader of `ROOTS`;
DESIGN-test-build-ci.md §3.14.10 item 4 and the Linux gate's guard paragraph describe the fixture.

**Self-review.** Every converted self-test was run on its own and passed (devlog 45/45, landing_check 45/45, enqueue,
prune_worktrees, gate_slot 11 arms, run_gate_legs 57 arms, self_tests, land_lane, plan_wave 338/338, line_endings,
rendered_views, status_guard, checkpoint, ledger_state, member_index, r2_subsystems, file_census_notes,
audit_evidence_supersession, gen_ledger). The first audit draft flagged data tuples (`("git", "commit", …)` as an
expected value) and the subject's own git functions only a self-test calls (sharedfile.second_copies,
landing_measure.git_runner); the SPAWN arm now reads only a `subprocess` call's argv, and top-level functions reached
only from self-test code are read by the INIT arm alone. A rename pass that turned `run(` into the fixture call also
caught `subprocess.run(` once (line_endings.py); found by a grep for the doubled name and fixed before the commit.
semgrep verify: no count moved. kb/Work PB3582 is unblocked (its blocker landed).

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 10,351/18,325 · Unit 34,347/35,652 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) … run 000002-20261024T003804Z ===`; the four `SelfTestFixtureDriftTests` facts passed in leg 1, and the self-test phase was GREEN (46 ran, 50 reused). The first gate (run 000001) was RED on CITATIONS: the drift test cited the design section as `§3.14.10`, which the code-citation audit reads as an ISO clause; it now says "section 3.14.10".
