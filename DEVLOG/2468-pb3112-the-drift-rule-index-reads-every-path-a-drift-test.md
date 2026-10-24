## Entry 2468 — 2026-10-24 12:22 PDT — PB3112: the drift-rule index reads every path a drift test spells, so a grammar edit runs the five grammar drift tests at the Windows gate; The inert lane: one classification of what no test reads, proved by an inotify watch on every Linux test run (PB2878)

### PB3112: the drift-rule index reads every path a drift test spells, so a grammar edit runs the five grammar drift tests at the Windows gate

**Re-probe.** The note said the tier-0c cost cap keeps about 270 Sweep cases out of Windows leg 1, so a tree-wide rule
goes red first at the landing's Linux gate. Two landings had already closed that for the cases it named: tier 0h runs the
trait guard at every gate, and tier 0d (PB3531) runs every drift test the drift-rule index says governs a changed file,
whatever it costs. PB3247 then taught the index to follow helper classes (`SuiteManifest`), which is what the previous
implementer's report named as the remaining mechanism (w1129g's `ConstantNameLiteralPositionDriftTests`). I replayed the
35 Linux-red landing heads the reports record against today's index (`replay.py`): every drift-rule red is governed except
a lock race, a harness timeout, an apphost rebuild (PB3529) and `SpecTraceabilityInventoryDriftTests` (PB3131).

**What was still open.** The same shape one level down. The index read a call's path with a regex over arguments that held
no parenthesis, so `TestRepo.Src(Path.Combine("Cobol.Net.Frontend", "Grammar"))`, `Path.Combine(TestRepo.Root, GrammarDir)`
(a class constant), `Path.Combine("src", ..)` and a wrapper whose callers spell the path (`Read("src", "Cobol.Net.Compiler",
"X.cs")`) named nothing. 71 of the 391 drift tests had no read the index could place; 62 gained at least one when the
reading was fixed. Five scan the grammar (`ArithmeticOperandRoleDriftTests`, `SendingOperandDriftTests`,
`ReceivingOperandFormDriftTests`, `LevelNumberArmDriftTests`, `ClassConditionTableDriftTests`) and governed no grammar
file, so a grammar edit's red could first appear at the landing's Linux gate. `SourceEncodingDriftTests` read
`TestRepo.At(tree)` from a theory's parameter and governed nothing.

**The fix, at the root.** `scripts/spec/drift_rules.py` now reads a path with ONE evaluator (`path_expr`, `path_join`):
string literals, `TestRepo.Root`, class constants (`const string`, `static readonly string`; a local or a parameter is
another method's name and is never read, which a first draft got wrong: `string script = TestRepo.Scripts("arch", ..)` in
one method doubled the prefix of `TestRepo.Scripts("arch", script)` in another), collection expressions, nested
`Path.Combine` and `TestRepo.X` calls, with balanced parentheses, up to the first argument it cannot spell. A method that
wraps one read is read at its callers (`Sources.reads`), and its own directory read is replaced by theirs unless some use
of the wrapper (a method group, `nameof`) spells none; a method that only passes arguments on (`Go("--check")`) is no
wrapper. `drift_rules.py --check` now refuses a drift test that reads the repo and spells no path (`blind`), so the index
cannot silently miss a whole class again; `SourceEncodingDriftTests` and `GateLegDriftTests` spell their trees with
literals. No hand table: a new reader is followed the day it is written.

**Sweep traits.** The exact reading moves the `Sweep` trait for four classes, and `HostWitnessTraitDriftTests` holds it
both ways. `LevelNumberArmDriftTests` (it enumerates `src/Cobol.Net.Compiler`) and `SourceEncodingDriftTests` (`src`,
`tests`) carry it; `CitationAuditSelfTestDriftTests`, `FixedFormRecordDriftTests` and `Gr4GroupDecisionDriftTests` read
only the files they name and lose it.

**Cost.** Tier-0d recorded seconds, before to after, from the shared timings store (contention-inflated): a grammar edit
74 to 78 classes, 476 to 581 s (`SendingOperandDriftTests` 76 s and `ArithmeticOperandRoleDriftTests` 24 s join); a compiler
emitter edit 130 to 128 classes, 357 to 346 s; a binder edit 404 to 404 s; a golden-adding, test-editing,
script-editing or `docs/CONFORMANCE.md` diff is unchanged. Recorded in DESIGN-test-build-ci.md section 3.14.2.

**The timing instances.** The note's third to fifth instances (a load-skewed sample, a leg-2 case never re-timed) decided
whether a sweep the diff reaches ran at the Windows gate. With tier 0d they decide only the order of a sweep the diff cannot
reach, so the timings store needs no change.

**Witnesses.** `drift_rules.py --self-test` fires four planted classes (a nested call, a constant, a bare tree-root
`Path.Combine`, a wrapper and its method-group use, and a blind test); `impacted_tests.py --self-test` names the grammar
tests for `CobolData.g4`, `ClassConditionTableDriftTests` for `ClassConditionModel.cs` and `FixedFormRecordDriftTests` for an
emitter file against the real index. No golden: this is test-gate tooling.

**Review round 1.** The reviewer found that a wrapper's caller contributed EVERY argument to the wrapper's path, including
the ones bound to parameters the body's path never carries: `CitationAuditSelfTestDriftTests.AssertDeclares(string script,
params string[] cases)` was indexed as reading `scripts/spec/cite.py/fires EMPTY-NEEDLE (check)/..`, so it lost its
governance of `cite.py`. `Sources.reads` now records which parameters the read's path carries and takes only those
arguments at a caller (a `params` parameter takes the rest); `cite.py` governs the class again, and the probe for index
paths that exist nowhere finds none from a wrapper. A constant that names a later one no longer reads as ambiguous when a
pass refines its own earlier partial value (the reviewer's lead): `Sources.constants` repeats passes to a fixed point and
flags only a name declared twice to different values in one pass. Both have planted arms in `drift_rules.py --self-test`,
and `impacted_tests.py --self-test` holds `cite.py -> CitationAuditSelfTestDriftTests` against the real index.

**Landing note.** This branch merges the PB3155 follow-up (`worktree-wf_0e51b14b-ced-6`, entry 2434), which was reviewed
CLEAN and has not landed; it lands first or with this one.

### The inert lane: one classification of what no test reads, proved by an inotify watch on every Linux test run (PB2878)

kb/Work PB2878 (owner 2026-10-09: "Should this require a full CI run? I would think not") asked for a path-classified
fast lane: a change confined to paths no test reads runs only the audits, in CI and locally, from one classification.

**Measured first.** Of the 300 landings on main from 2026-10-16 to 10-23, 137 changed only paths CI's doc-only regex
named (107 of them only `DEVLOG/` and `kb/Work/`), and each still paid the Windows gate (one sampled notes landing:
865 s wall, of which audits 150 s, slot wait 288 s, build 41 s, legs 54 s, post-build self-tests 307 s) and the
landing's four Linux legs. The regex was not a classification. Deleting each candidate directory and running the three
assemblies on Windows showed `docs/` read by twenty-odd classes (CONFORMANCE.md, the rule catalog,
VERSION_CHANGE_REFERENCE.md, and every doc through SortRuntimeDocShapeDriftTests and GateLegDriftTests), `kb/Work/` by
the register drift tests, `README.md`, `.claude/` and the root `*.md` files by others. So CI skipped the matrix for
changes that tests read, the hole PB3603 hit.

**Mechanism.**
- `scripts/inert_lane.py` is THE classification: INERT entries (each naming who reads it, never a test) and READ entries
  under them; a change is inert when its paths (`git diff --no-renames`, so a code file renamed into an inert
  directory shows its old name) are all inert. CI's `changes` job (its path regex deleted), the local gate
  (`run_gate_legs.py`, `Host.lane`, after the audits) and the landing's Linux gate (`linux-gate.sh`) each call it; an
  inert change runs the audits and stops GREEN with no slot, build or leg.
- The proof is an inotify watch (`inert_lane.py watch`, through ctypes): every Linux `dotnet test` step of CI and the
  Linux gate's test legs run under it, and a test that opens an inert file or lists an inert directory, directly or
  through a script, is RED naming the path. The Linux gate's self-tests and guard moved to an AUDIT clone with its own
  build, so audit reads never reach the watch (and the guard's own CLI build no longer runs in the clone whose CLI the
  legs run, kb/Work PB3529).
- The conflict-marker sweep, which read every tracked file and so made every path a test input, became an audit
  (`scripts/conflict_markers.py --check`, kb/Work PB735's rule unchanged) in the gate's AUDITS, CI's `audits` job and
  battery phase -1; `ConflictMarkerDriftTests` is deleted. It now also runs on a change whose matrix CI skips.
- `InertLaneDriftTests` holds the wiring: the `changes` job classifies through the module and holds no path regex,
  every Linux `dotnet test` line runs under the watch, both gates classify through the module, and every entry names a
  tracked path. `inert_lane.py --self-test` fires the classification and the watch (a read, a listing, a READ path,
  the command's own exit code).

**The watch's first run corrected the list.** The first whole Linux population under the watch (all legs otherwise
GREEN: unit 35,842, conformance 18,327, characterization 36, guard 362 MATCH) was RED on 86 inert paths:
LandingPipelineDriftTests' `Current` sweep reads every `kb/` page outside `kb/Work/` and `.obsidian/`, the register
drift tests read every `kb/Work/` note, and the self-tests initialize the skills submodule. So the first cut made
`DEVLOG/` alone inert, and the classification the Windows deletion run suggested (`kb/` but `kb/Work/`, `.obsidian/`, the
pin) would have been unsound; the watch is what found it.

**Part 2: the vault becomes inert.** Notes landings are the commonest kind (107 of the 137), so the readers of `kb/`
were removed rather than listed:
- The two register drift tests (`ClosesRowsBackLinkDriftTests`, `DefectiveRowCoverageDriftTests`) read every note through
  `tests/_shared/WorkRegister.cs`, a C# twin of `work.py`'s frontmatter reader that the parity fixture, a Python process
  run from a test and `work.py parity` existed to keep in step (kb/Work PB875 is what their disagreement cost). Their
  checks are now `scripts/spec/work_inventory.py`, called by `work.py check`: every defective-verdict row claimed by a
  live note, every `inventory_rows` claim naming a real rule, every landed `kind: defect` note naming the rows it closed
  or saying why none, every row so named closed in the inventory, a population floor on each, and a `--self-test`
  that drives the same predicates with a register built to break each (the fabricated-register arms of the deleted
  tests, ported). `Schema.defective` in `inventory_schema.py` derives "defective" from the schema's `resolves` flags.
  They are now audits: the gate's `WORK REGISTER` audit runs them before the slot and CI's `audits` job on every event,
  so a `kb/`-only change runs them exactly as any other. The C# twin, both tests, `TraceabilityInventory.cs` (their
  only user), `TestRepo.Kb` and `work.py parity` are deleted; the fixture stays, renamed
  `work-frontmatter-cases.json`, as `work.py --self-test`'s cases for the one remaining reader.
- `LandingPipelineDriftTests.Current` (the retired-landing-script name sweep) sets `kb/` and `.obsidian/` aside; so do
  the other three tree sweeps that read every tracked file (LandingPipelineDriftTests.NotNarrative,
  LandingLaneDriftTests.Current, LandingMeasureDriftTests.NotNarrative: the last three lacked `.obsidian/`). No file of
  either named a retired script or the retired wording (measured by grep).
- `RenderedViewsDriftTests` is deleted. It only ran `rendered_views.py --tree`, whose rule (c) reads the head of every
  tracked `kb/` note, and the same rule is `rendered_views.py --check`: the gate's `RENDERED VIEWS` audit and a step of
  CI's `audits` job, on every event. The `--tree` mode, whose one caller it was, went with it. This was the
  second watch run's finding: the first with the register tests gone still opened 60 vault paths (57 notes from this
  sweep, 3 `.obsidian/` files from the sweeps above).
- `EcRaiseSiteRegistryDriftTests` no longer reads a note: it asked whether the note that built each activation-boundary
  name contained the text `status: landed`, a substring test (kb/Work PB3295: an open note quoting the text passed) beside
  the real evidence, that a raise site stages the name. The notes are named in the comment.
- `kb/` and `.obsidian/` join `inert_lane.CLASSIFICATION`. Two merge consequences of part 1 landing after the self-test
  fixture (kb/Work PB3584): `conflict_markers.py` and `inert_lane.py`'s self-tests now drive their scratch repositories
  through `ScratchRepo`; `drift_rules.py`'s self-test stood on `TestRepo.Kb` and now plants `TestRepo.Nist`.

**Rows and notes.** No inventory row. PB2878 lands; PB720 (`work.py check` never read `inventory_rows`; its loader kept
the quotes, fixed by PB875) and PB3295 (the substring status read) are closed by the same change; PB265 and PB1729 name
the deleted tests in their sites and stay open (the YAML-validity question and the transient access-denied read,
which `conflict_markers.py` now meets in the audit phase).

