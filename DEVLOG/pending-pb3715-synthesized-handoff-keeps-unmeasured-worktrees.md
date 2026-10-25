## PB3715 — the synthesized handoff no longer drops a worktree git could not measure

`checkpoint.py synthesize` builds a dead unit's handoff from the CURRENT worktrees, and kept only those with
`t["ahead_of_main"] or t["uncommitted"]`. PB3250 had made `worktrees()` report `None` for a count git could not read ("unknown,
never a clean 0"), and `None` is falsy, so the one worktree whose state was unknown (a corrupt index, a refused directory, a
dubious-ownership refusal), the one whose work a crash may have left uncommitted, vanished from `branches_pending`.

**Re-probe.** `scripts/orchestrator/test_checkpoint.py` gained a case on the unreadable linked worktree it already builds for
PB3250 (its `.git` pointer is garbage, so git returns neither a status nor an ahead count). With the old filter put back the
self-test failed five checks: `branches_pending` was `['committed', 'dirty']` instead of `['broken', 'committed', 'dirty']`,
and `holds_work` read `(None, 0)`, `(0, None)` and `(None, None)` as clean.

**Fix.** Two named predicates in `checkpoint.py`: `unmeasured(tree)` (either field is `None`) and `holds_work(tree)`
(`unmeasured`, or a non-zero ahead or uncommitted count). `synthesize` filters through `holds_work`, so the unknown arm is
tested by name instead of through truthiness. The handoff schema allows no extra key on a `branches_pending` entry, so the
mark goes in the summary: `N worktree(s) hold work or could not be measured (see checkpoint.json); git could not measure
<branches>`. `DESIGN-orchestrator-loop.md` section 5.1 states the rule.

**Sibling sweep.** `worktrees()` has two readers, `build` (writes the rows raw into `checkpoint.json`) and `synthesize`. A grep
of `scripts/` for `["uncommitted"]`, `ahead_of_main`, `.get("ahead` and `isdigit() else None` found no other truthiness test on
these fields (`landing_check.py` and `plan_wave.py` carry an unrelated `uncommitted` file set; `next_unit.py` reads only the
`status` of a pending entry).

**Tests.** `test_checkpoint.py`: the unreadable worktree is in `branches_pending` and named in the summary; the clean one is
not; `holds_work` and `unmeasured` over (0,0), (2,0), (0,3), (None,0), (0,None), (None,None).

**Register.** PB3715 flipped to landed; it claims no inventory row, so `closes_rows` stays empty with its reason.
