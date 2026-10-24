## The inert lane: one classification of what no test reads, proved by an inotify watch on every Linux test run (PB2878, part 1)

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
LandingPipelineDriftTests' `Current` sweep reads every `kb/` page outside `kb/Work/` and `.obsidian/`, and the
self-tests initialize the skills submodule. So INERT is `DEVLOG/` alone today, and the classification the Windows
deletion run suggested (`kb/` but `kb/Work/`, `.obsidian/`, the pin) would have been unsound; the watch is what found it.

**What it does not yet cover.** A notes landing changes `kb/Work/`, which tests read, so it is still the full lane, and
since the regex is gone CI now runs the matrix for it, which PB3603 shows is correct. PB2878 stays open for part 2:
the register drift tests' cross-checks become an audit (`work.py check`, the register's one reader) and
LandingPipelineDriftTests' sweep sets `kb/` aside as its siblings do, after which `kb/` joins INERT and a notes landing
runs the audits only.
