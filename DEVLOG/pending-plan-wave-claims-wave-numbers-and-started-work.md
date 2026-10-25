## The wave planner: started work in every lane, claims that meet on any file, data instead of prose, and one allocator for wave numbers (PB3699, PB3118, PB3696, PB3695, PB3712, PB3700)

Group C of wave 20261107: six process-tooling defects with one root, `scripts/orchestrator/plan_wave.py` and the three
scripts it shares its rules with. Each was re-probed on this build first; one (PB3118) turned out to be half fixed
already.

**PB3699, `--drain` plans no finisher for a SPLIT group (MAJOR).** A fixture of the note's shape (two reports whose
open `process_only` leads sit in no cluster, both on unlanded branches) planned the FINDINGS-stamped one (PB3150's arm)
and listed the SPLIT one's branch as a judgment row, in both modes. The fix lane's clusters skip `process_only` notes,
so a campaign's groups cut short by a fleet stop (wave 20261031's A, C and D) had no lead. `plan` now makes every
still-open note a SPLIT, NOT STARTED or gate-ended report answers a lead when its branch is not on main, and
`branch_lead_notes` reads an unfinished report's lead from its own branch. A landed note, a landed branch and a branch
with no report stay out. The dry-run plans of the real register differ from before only by a worktree created
meanwhile.

**PB3118, the survey's promise.** The planner half is PB3150's. What still differed was the survey: a covered FINDINGS
stamp on a report older than `--stale-hours` read "never landed: land it or record why not", ahead of the finisher text,
and the landing refuses a FINDINGS branch. `prune_worktrees.findings_open` is now the one statement of a covered stamp,
read by `finisher_due` and by the stale arm. The survey's self-test ages one FINDINGS row to 30 h and plans the same rows
with no cluster holding the lead: every DECISION row that promises a finisher is checked against the planner on a real
git history. Restoring the old arm turns it red.

**PB3696, in-wave chaining compared the primary file only.** Two groups on different members of one primary file that
both claim a third file whole ran side by side, and so did two groups on different primary files sharing a secondary
file; the in-flight check holds the same pair. The in-wave rule now asks `collision` of every placed group (`on_file` is
gone). The sibling arm was a second copy of the rule: `check_practices` O2 compared groups on their primary file with its
own `members_meet`; it is `claims_meet_without_successor` now, over `plan_wave.collision` and each group's `file_set`
and `member_set`.

**PB3695, the partition summary parsed prose.** Every parenthesised token of a hold reason counted as a colliding file,
so branch names and note ids (`worktree wf_x (worktree-wf_x)`, `dispatched w9a (PB547)`) ranked among the most-named
files. `collision_hits` returns (owner, hit); the prose is rendered from it; `plan` returns `collided`; the record keeps
`collided_on` beside the prose; the summary counts it and sorts ties by name.

**PB3712, a deferred link named a head that may not run.** Under the live one-link-per-chain rule, a chain whose head
the budget or the group cap cut still told its links they followed a group "which this wave runs", and the third link
said so of the deferred second. The reason is chosen in the budget loop, where the predecessor's fate is known.

**PB3700, a wave number for a hand dispatch.** `alloc.py` has a `wave` kind: truth is the highest wave or train number in
a report's name, the dispatch ledger or the newest DEVLOG labels (`next_wave`'s rule, moved into the allocator and
`next_wave` deleted), reserved in `alloc.json` like the others. `plan_wave --wave next` peeks it while it plans and
reserves it once the specs check GREEN, as for the codes; a hand dispatch runs `alloc.py wave` before it names its report
(`w20261104a-PB3690` and the loop's wave 20261104 shared a number). So that `alloc` stays a leaf of `coord`, the
coordination directory's two file formats moved there: `REPORT_NAME`, `DISPATCH_LEDGER` and `ledger_groups`, with every
caller changed and no alias left.

**Self-review.** The silent-failure lens found two things fixed in the change: the summary's tie order depended on
insertion order (now by name), and `landing_measure`'s self-test kept an `import plan_wave` its only use of which had
moved (removed). The test lens added the control for each new rule (the pair that shares nothing still runs side by
side; a landed note and a landed branch still get no finisher) and the in-flight parity check for PB3696. The comment lens
corrected the docstrings that said "primary file" and "same-file rule".

**Rows.** None: no ISO rule, no inventory row; all six notes close none. **Tests.** `test_plan_wave.py` (442 checks),
`test_alloc.py` (31), `prune_worktrees.py --self-test`, and the landing_check, landing_measure, progress, coord and
turns_by_group_size self-tests that import what moved.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `000001-20261025T174341Z`, Conformance
8,271/18,516 · Unit 36,897/38,235 · Characterization 36/36 cases ran, Linux half in a WSL clone of the head GREEN, 55
self-tests ran and 56 reused their recorded PASS). The change adds no golden: it touches no compiler or runtime path.
