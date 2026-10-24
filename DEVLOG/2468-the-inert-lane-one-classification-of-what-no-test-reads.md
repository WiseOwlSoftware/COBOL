## Entry 2468 — 2026-10-24 12:22 PDT — The inert lane: one classification of what no test reads, proved by an inotify watch on every Linux test run (PB2878)

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

