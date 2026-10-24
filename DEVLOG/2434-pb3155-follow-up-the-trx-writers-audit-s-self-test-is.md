## Entry 2434 — 2026-10-23 20:34 PDT — PB3155 follow-up: the trx writers audit's self-test is isolated from the caller's repository, and the plan file format lists its reds

**Why this entry is small.** PB3155 and PB3184 (a landing's Linux reds are tier 0a of the worktree's next gate) reached
main through their same-file successor's pull request (#474, PB3190's branch, entry 2441), which merged this branch at
an earlier head. Entry 2441 tells that story. This branch's own pull request (#467) had bounced, and two pieces of its
finisher's work were still not on main. They land here.

**The bounce.** PR #467 never entered the merge queue. CI's `audits` job (run 40058938921) failed
`scripts/scratch_repo.py --self-test` with two findings in the trx writers audit's self-test (`gate_plan.py`: INIT, a
self-test creating its repository with `git init`; SPAWN, a self-test starting git itself). PB3584's fixture audit
(#463) landed on main while this branch's landing gate ran, and `enqueue.py` merged it in after the gate, so no local
run saw the two together. The successor fixed the same findings on main with `ScratchRepo.init`. Main then moved on and
the pull request conflicted (merge state DIRTY) in that same self-test.

**What lands.**
- `gate_plan.py --self-test` calls `scratch_repo.isolate()` before the trx writers audit arm. The code under test
  (`audit_trx_writers`) runs `git ls-files` in the self-test's own process, and `isolate()` is the fixture's contract for
  that case: git's repository-selection variables (`GIT_DIR` and its kin) are dropped, so a hand run from inside a hook
  or a linked worktree audits the scratch repository and never the caller's. `self_tests.py` already scrubs them for
  the processes it starts; the hand run had no such protection. The merge keeps main's `ScratchRepo.init` form.
- DESIGN-test-build-ci.md section 3.14.2: the plan file format lists the plan's `"reds"` field and what it counts, one
  entry per tier-0a source (branch review lead R1-1).

**The local gate's gap (the CI invariant).** The red came from a merge of main made after the local gate, and both
sides were green alone. Under the owner's PB3612 contract no local gate covers that merge, and PB3612's one exception, a
textual merge in a file the branch touches, would not fire either: main changed `scratch_repo.py` and the branch changed
`gate_plan.py`. The finisher's report holds the note-ready paragraph: run the tree-only self-tests and audits whose
inputs the merge changed (`self_tests.py` reuses every other one, so here one 2-second self-test would have run) on
the merged tree before `enqueue.py` queues it.
