## Entry 2492 — 2026-10-24 17:28 PDT — The branch survey and the planner read one rule for who carries a branch, and one commit a report describes (PB3476, PB3474, PB3652, PB3653)

Four open notes of the PB1957 process campaign shared one root: `scripts/prune_worktrees.py`, `scripts/orchestrator/plan_wave.py`
and `scripts/orchestrator/enqueue.py` each answered a question the other already answered, so the branch survey, the
planner and the landing lane could disagree about the same branch. Each note was re-probed on a scratch repository before
it was touched; none had stopped reproducing.

**PB3476, the commit a report describes.** A DONE report whose header said `HEAD: see the trailing commit` parsed to
`head = None`, so the survey read a branch reviewed CLEAN at its tip as "describes no commit of the branch", IN FLIGHT and
then a DECISION, and the landing lane never saw it (w1136c; the same shape on `worktree-wf_36fbfc86-472-20`). The review
stamp is the stronger statement: the reviewer stamps the head it reviewed. `plan_wave.parse_report` is now the one owner of
the commit a report describes: its header's HEAD field, else the head its stamp names. A report with neither still
describes no commit and stays a DECISION; one with a sha keeps it. The survey self-test's `no-head` fixture, which had
pinned the old behaviour (DECISION), is now WAITING TO LAND, and `no-head-bare` carries the case that stays a decision.
No refusal at the stamp step: with the stamp as anchor a head-less DONE report is stampable, and a refusal would have cost
a finisher cycle for a header the system now reads correctly.

**PB3474, the never-work rule.** `classify()` filtered the files a branch adds with its own copy of the rule, and
that copy carried a `tools/` exclusion `coord.NEVER_WORK` lacks. The exclusion was written for the claude-skills
submodule, a gitlink that is never an added path of a branch cut from main, and it hid every added file of the tracked
`tools/ArchCensus`, `tools/impact` and `tools/rr`: a branch adding only those read CHECK with no file named. Decided:
`tools/` is product tooling, not never-work. `classify()` now calls `coord.is_work`. The sweep found a second copy,
`impacted_tests.changed_files` (`p != "STATUS.md"`, so the harness's edited settings file counted as a change of the
worktree), and routed it the same way; `enqueue.py`'s docstring now names `coord.NEVER_WORK` instead of copying two of its
members. A source scan in `test_plan_wave.py` fails on a hand-written never-work test anywhere under `scripts/` or
`.claude/skills` (planted shapes of both old copies included). `scripts/linux-gate.sh` keeps a shell copy (a cosmetic NOTE
counting the settings file as an untested change); that is a lead in the report.

**PB3652, a carried SPLIT branch is no decision.** The survey listed a SPLIT branch another branch had cherry-picked as a
DECISION row until the carrier landed, although the planner plans no finisher for it. The probe found the hole wider than
the note: `contained()` skipped every DECISION row that has a report that is not FINDINGS, so a SPLIT branch that is an
ANCESTOR of a finished carrier stayed a DECISION too. `contained(rows, graph, reports, work_dir)` now admits an
unfinished (SPLIT, NOT STARTED) row and, after ancestry, reads the planner's replay rule through
`prune_worktrees.replay_view`, the one function that builds `replays`' inputs for both readers; `plan_wave.branch_replays`
calls it too, and a scan fails on any other caller of `replays`. A replayed row reads IN FLIGHT naming its carrier (and is
not `owned`). A partly replayed branch, one replayed by an ABANDONED branch, a FINDINGS row and a finished unreviewed row
stay as they were. The new fixtures are red with the old `contained`, which I ran.

**PB3653, a lock holds a branch's unit.** A hand-started agent on a SPLIT branch leaves no dispatch-ledger entry, and
`owned` folds the lock, the ledger and a recent touch into one set, so the planner's finisher arm could not hold on the
lock alone. `prune_worktrees.locked(rows)` is the lock-only half, `plan(locked=...)` reads it in the PB2974 branch-unit
hold loop (the dispatch hold and the lock hold are one loop, units keyed by `unit_of(branch)`), and a touch alone still
holds nothing. The residual the note recorded without a state is a real one: two different SPLIT reports about one branch
planned two finishers over it. `finisher_by_rep` now merges them into the newest UNFINISHED report's group (branch review R1-1: the first form merged into the newest report whatever its status, so a newer DONE report about another lead made the finisher's pred say "returned DONE" and name the wrong report; a self-test fixture covers it); the self-test is red on
the old code.

Docs: DESIGN-orchestrator-loop.md (the survey's coverage, the planner's branch unit, the replay rule), the module
docstrings of `prune_worktrees.py` and `plan_wave.py`. All 107 script self-tests ran green here (`self_tests.py`).
Self-review against the silent-failure, test and comment lenses: a stale stamp can only make a head-less report read
"commits since the report's HEAD" (a DECISION), never a false WAITING TO LAND; `survey_replays` returns nothing (no
carrier) when no row has a report or the register is missing, which is the safe side; one comment I had written claimed the
old `tools/` filter read LANDED, which the probe showed was CHECK, and I corrected it.

