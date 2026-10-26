## The loop records an impact map for every main commit it adopts, and a map serves a base file by file (PB3663)

The owner decided on 2026-10-24 that the loop records an impact map automatically for every main commit it adopts, so that
leg 1 of a gate runs what a change reaches instead of the cheapest slice of everything. Under batched gating (PB2515) leg 1
is the only local run; without a map it holds the change's own tests, the reds, the host witnesses, the sweeps and the drift
tests, and after those the cheapest cases of the whole population. PB3666's bounce was the evidence: two goldens its change
reaches sat in leg 2 and went red only in the merge queue.

**Re-probe (fresh build at 7d769bcc1).** No map existed and all 72 gate plans on the machine read `map none`. The first real
recording through the existing recorder took 13.6 minutes and wrote a 25.2 MB map (the design said about 5 MB) of 57,270
tests, and it ended `=== IMPACT MAP: FAILED ===`: the recorder's own watchdog listed eight `UniversalCrossingShapeDriftTests`
theory rows as never recorded, because `--list-tests` truncates a long display name with a marker and xunit's own name, which
the recorder reads, is the full one. Run by an automatic lane, that watchdog would have failed every recording. The recorder
also wrote the map non-atomically, so a gate planning while it finished could open half a gzip.

**One store module.** `scripts/spec/impact_store.py` now owns what the recorder, the lookup and the new lane each carried a
copy of: where the store is, the schema, the bitset codec, which paths are documentation, which map serves a base, and how the
store is bounded. The duplicates in `record_impact_map.py` and `impacted_tests.py` are deleted.

**Recorded at adoption.** `adopt_main.adopt` asks `scripts/orchestrator/impact_lane.py request` once, for the commit it adopted
or found current (never for a kept outcome or a dry run; a lane that raises cannot hide the adoption or the supervisor's
restart trigger). The lane starts one detached worker that runs the recorder at BelowNormal inside the recorder's own gate slot
and coalesces to the newest requested commit; a commit with a map, one a map serves in documentation only, and one whose own
recording already failed are skipped. A recording that does not end RECORDED (the FAILED line, no verdict, a recorder that
would not start, a worker that died or raised) posts to the operator's mailbox once per commit and rides every later adoption
line until one succeeds. After a RECORDED verdict the worker prunes the store to the newest 4 maps (100 MB, sized from the
measured 25.2 MB) plus the map serving each live worktree's base. `impact_lane.py measure` counts the gate plans a map served.
Proven end to end: the lane recorded a trial store's map for 315f9eb98 (RECORDED, 867.9 s, watchdog green).

**The staleness rule would have defeated it, so it is replaced (rule 5).** The lookup used a map only at the base or at an
ancestor that differs from it in documentation only; any other ancestor was STALE and put every test in tier 1. Main moved 44
landings in the 5.9 hours after the replay's map was recorded. `impact_store.nearest` now chooses the map whose tree differs
from the base's in the fewest source files, whatever its age or ancestry; a change to a file the two trees share is attributed
by line range, a change to a file that differs between them is taken at the file level (`impacted_tests.drifted_change`), and a
file new since the map puts every test in tier 1 with that reason. A map named with `--map` is served the same way, and a map
file that cannot be read leaves the plan unaimed with the cause printed.

**Measured.** Replaying the 44 landings after that map: all 44 read a map (1 exact, 43 nearest; the old rule: 1), and 18 of
the 22 that changed code were attributed (the other 4: three grammar, project or generator inputs, one new file). The replay
also found three rules that fell back for no reason a test can see, fixed here: the `PublicAPI.*.txt` baselines and
`RuntimeAbi.pin` are data a test may read, not "a non-C# input under src/" (two landings, and PB3666's own change); and a
file-level change is unattributable for running only outside every test's context only when no entry of the file was
reached by a test (two landings editing a file that holds a finalizer). PB3666's own change, replayed with the planner,
now reads 6,642 of 18,832 Conformance tests in tier 1 and leg 1 holds 1,935 cases with both bounced goldens in it, where it
had read everything as tier 1 and held 12,638. A change to code every test runs still reaches nearly everything (17,515 of
18,580 for one landing) and leg 1 runs the cheapest slice of that, which is the planner's budget, not the map's.

**Docs.** CLAUDE.md Testing, the gate skill, DESIGN-test-build-ci 3.13, 3.14.2 and 3.14.8, DESIGN-orchestrator-loop 4.10.1,
DOC_INDEX and the plan's gate paragraph state the rule; `ImpactMapLaneDriftTests` holds the retired phrases out and pins every
arm of the three new self-tests (`impact_lane.py`, `impact_store.py`, `record_impact_map.py`) and the one trigger.

**Rows.** None: a process decision, no inventory row claimed or closed.

**Self-review.** Silent-failure pass on the diff: the lane's three swallow points are loud (the adoption line, the worker
log, the mailbox); a worker that raises is a FAILED recording of the commit it held, not a silently dead worker; a plan file
`measure` cannot read is counted and said; a map `prune` cannot remove is reported busy. The first draft let a lane exception
escape `adopt` after the checkout had moved, which would have hidden the supervisor's restart trigger; fixed and tested.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` at head 4d1a4e348, run 000003-20261026T041742Z:
Conformance 8,346/18,832, Unit 36,641/38,371, Characterization 7/36 cases ran, `Linux half (WSL clone of HEAD) GREEN`; the four
`ImpactMapLaneDriftTests` and the six `ImpactedTestsDriftTests` Passed in `leg-1-Unit.trx`. Two earlier reds, both this change's
own: the line-endings audit (five self-test writers lacked `newline=`) and `HostWitnessTraitDriftTests` (the new drift test reads
the tree and so carries the `Sweep` trait the drift-rule index names it for). The gate's own plan read `map none`: the lane
that this change adds had never run.
