## Entry 3005 — 2026-11-02 11:51 PST — kb/Work PB4452: a shared file holds no work, only an edit to the same member does

The owner made this the top priority on 2026-11-02 ("Fixing the planner to allow this should be the top priority. And
preempt everything else as it unblocks so much"; "Non overlapping edits to the same file typically will merge without
conflict"). `plan_wave.py --cluster PB2293 --from-budget --dry-run` held all 22 open campaign steps behind
`src/WiseOwl.Cobol.Compiler/Binding/BinderDriver.cs`, which the two running slice branches never edit.

**Cause.** `inflight.slice_reservation` reserved DESIGN-external-repository §12's file set (`work.slice_file_set`) WHOLE
against every restructuring wave, as an in-flight owner of its own. A restructuring note's computed set names every
caller of what it moves, so nearly every binder extract shared a file with the reservation. `inflight.collision`, which
already reads members (PB3670), never saw one. Three more places put a whole file in front of work: the register's
`r3_program_problems` made an R3 note whose sites meet the §12 set wait on an open slice; five notes' prose and
`blocked_by` (PB2296, PB2398, PB2401, PB2403, PB4319) said so; and `landing_check.py` checks 1 and 2 stopped a landing
on a shared file.

**The rule now (`inflight.hold`).** RUNNING work holds a group only where a member of one matches a member of the other
(`source_units.meet`) on a file both hold. A file only shared holds nothing, and neither does a file either side claims
whole (an index-found caller, a site named whole, a diff the unit reader could not read). `collision` stays the rule of
GROUPING (one step per file within a wave, `check_practices.py` O2) and of JOINING a FINISHED branch (PB4396), where
meeting on the file is what makes one step. Git's merge at landing is the authority on a conflict this rule cannot
foresee, and a finisher repairs a real one.

**Changes.** Deleted: `slice_reservation`, `SLICE_OWNER`, `InFlight.reserves`, `classify_inflight`'s reservation arm,
`work.slice_file_set`, `open_slices`, `restructuring`, `RESTRUCTURING_PROGRAM`, the register's slice-wait check and the
§12 reader behind it (about 170 lines of `work.py`). Added: `hold` and a `rule` argument on `collision_hits`.
`plan_wave.plan_partition` holds a group by `hold` against running work and still joins a finished branch by
`collision`. Landing check 1 (an R3 landing's diff outside its declared set) is removed: it enforced whole-file
partition against the planner's own estimate, and a file outside the estimate that nobody else edits merges. Check 2
now stops the later of two branches only when their ACTUAL edits meet by member, reading the units each diff touches
with the planner's own reader (`inflight.inflight_units`, the landing as one more owner). It was not deleted: exit 3 and
the lane's retry-when-free machinery (`land_lane.landing_check_free`) still need a stop that clears when the other
branch lands, and checks 0 and 0b stay. Re-derived `blocked_by`: PB2296 drops PB2103 and PB2104, PB2398 drops PB2104 and
PB2403 drops PB2104 (their only reason was the file partition; PB2403 keeps it through PB2402 and PB2401). PB2401 keeps
PB2104 and PB4319 keeps it too: slice 8 lands Origin's source case, whose `Ctx` PB2401 moves, and adds `Skeleton` and
`Expansion` to the `RepositoryRecord` family PB4319 moves, so each needs the slice's RESULT. Their prose holds are
restated.

**Pins.** `test_plan_wave.py` pins that two groups on different members of one file run side by side, that a group on
the same member is held with `file#member` named, that a whole claim on either side holds nothing, that a finished
branch on the file is still continued, and that no reservation exists. `landing_check.py --self-test` plants the same
pair at the landing (46 cases): a fix-lane edit to another member lands, a dispatch that recorded no members and a
renamed file claim the file whole and stop nothing. Verified: `plan_wave.py --cluster PB2293 --from-budget --dry-run`
now plans a group of eleven notes (PB2296, PB2398, PB2352-PB2360) and holds none on BinderDriver.cs.

**Left.** About 85 more census-written notes carry the sentence "dispatched only when its file set collides with no
in-flight train and no open external-repository slice" in their Extraction order paragraph. The planner no longer reads
it; the notes are history.

**Review round 1 (finisher).** The reviewer found three agent-facing statements that still prescribed the deleted
whole-file holds (the architecture-review skill's partition paragraph and landing-check sentence, design 8.7's "Kept:"
list and the namespace-flip sentence); all restated to the member rule. Two leads in files this branch changes:
`inflight_units` now warns on stderr when a git failure leaves every shared file unmeasured (a whole file holds
nothing, so the failure must not be silent; pinned in test_plan_wave), and `file_set_collisions`, which only the tests
called, is deleted (the tests use a local helper over `collision_hits`); the member-hold wait text reads
"same-member collision with in-flight work".

