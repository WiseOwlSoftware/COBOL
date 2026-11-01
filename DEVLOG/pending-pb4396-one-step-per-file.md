## PB4396: one step per file: the notes whose claims meet are ONE group sized by turns, a finished branch is continued, an overflow chain lands once

The owner, 2026-11-01: "I continue to fail to understand why we do not do all work operating on a file in one step.
PB4395 edits the same files as steps 8 and 11, then do all three together", and, offered the choice, "One step per
file". Three earlier decisions had combined into one file's work running as several steps: the 4-note group cap
(kb/Work PB3704, 2026-10-25, "so a group lands within the hour"), the same-file successor (`after:`, kb/Work PB3670,
MANDATORY-PRACTICES O2) and "land the clean links as they come" (kb/Work PB3381, 2026-10-18). A file with more than four
open notes became a chain of groups, and each link built, gated, reviewed and landed the same files again, its fresh
implementer re-orienting on them. The campaign planner did the same with `blocked_by`, and so did the operator by hand
(PB4395 was filed `blocked_by` the in-flight steps 8 and 11 on the same files).

**Re-probe.** On the merged base (origin/main 1d089b67e with 38cad1b62), test_plan_wave.py's own fixtures showed it:
check 4 asserted that a second cluster on Binder.cs is that file's successor `D2` (after `D`), the golden carried the
`D2` group, and PB3528's two SPLIT branches sharing a file were planned as a chain `A`, `A2`.

**The fix, one rule in each of four places.**
- The group size is the turns one implementer holds: `clusters.notes_cap` (with `predicted_turns` and `turn_budget`),
  from model_rules.json `wave.turns_per_group` 65 and `wave.turns_per_note` 20.5 (turns_by_group_size.py's fit) under
  `wave.turn_budget_fraction` 0.75 of the implementer's turn cap (its one home, `coord.agent_turn_cap`): 11 notes today.
  `wave.max_notes_per_group` is retired, and every reader moved (the planner, `run_fix_clusters`, `campaign_clusters`,
  the ledger's estimate and its page).
- `plan_partition` absorbs a group whose claim meets a placed group's (`inflight.collision`, unchanged) into it, a
  finisher's notes, branches and instruction included (`Group.absorb`), and unites the groups one claim meets, while the
  result fits; past that the group keeps an `overflow` whose notes wait, naming the group whose branch the next wave
  continues. A FINISHED in-flight branch it meets is continued (`continues`; a blocker riding on it counts as answered).
  Only a dependency on a group whose claim it does not meet still makes a successor. `plan_fill` no longer absorbs by
  primary file: the partition's rule is the one mechanism. Letters are assigned once every group is placed.
- `continues` and `overflow` go into groups.json and the dispatch ledger (`make_dispatch_specs.record`,
  `dispatch_ledger.record_dispatch`), and `prune_worktrees.chain_links` (over `chain_records`) reads a WAITING TO LAND
  branch that a live dispatch continues, or whose dispatch left a pending overflow, IN FLIGHT (`Row.chain`, excluded
  from `owned`), before the PB3693 landing holds run: the chain lands once, with its last link. A dispatch's branch is
  found by its report's slug AND lead (the self-test's fixtures reuse a slug, and found the first lookup ambiguous).
- `check_practices.claims_meet` (which replaced `claims_meet_without_successor`) refuses two groups whose claims meet,
  with no `after:` exception. MANDATORY-PRACTICES O2, rolling-wave.md, the workstream SKILL.md, DESIGN-orchestrator-loop
  section 9 and DESIGN-test-build-ci state the rule; the continuation's pred says its branch and the continued ones
  land once, the DEVLOG stamp folding their pending entries into one.

**Tests.** test_plan_wave.py: the base fixture's Binder.cs work is one group (PB2, PB5, PB14), the golden regenerated
(the `D2` group gone, `continues` in every finisher's JSON); a new block 9d plants seven notes on one file (ONE group under
the live rules; at a 3-note size one group and an overflow of four, each reason naming group A's branch, the overflow in
groups.json and the spec body, and its reason when the budget cuts the group); PB3528, PB4352, PB3746, PB3653 and PB3170
(c) read two meeting finishers as one finisher continuing both; PB3712's deferred-link reasons move to a dependency
chain over three files; a groups file with `after:` between two meeting groups is refused. prune_worktrees' self-test
plants a ledger (through `record_dispatch`) and reads its chain records, and surveys the CLEAN `wpred` IN FLIGHT both as
a continued branch and as an overflow's head while `wsucc` is no longer held behind it. No ISO rule governs this, and the
note claims no inventory row.

**Self-review.** One mechanism per job: the absorption by primary file in `plan_fill` was a second copy of the meeting
rule and is gone; the group size is read from one function by every reader; `Group` is a mutable dataclass with no hash,
so the partition dedupes by identity (`distinct`). Left as is: an overflow note planned in another dispatch releases the
held branch (`chain_records`' pending rule), so a record cannot hold a branch for ever; `plan_wave.py --release` drops one.
