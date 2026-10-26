## PB3837: the code takes the product's name, WiseOwl.Cobol; the earlier names are retired outside history

The owner, 2026-10-26: the product is no longer named for its first name, so the assemblies, namespaces and every
ongoing use of that name go, with root name `WiseOwl.Cobol`; and the legacy byte engine's product name "should appear
nowhere except possibly in history" (kb/Work PB3837, decision).

**Measured first, on the base (`ec9bda522`).** Outside DEVLOG/, adjudication/ and the frozen evidence: 24,806
`Cobol.Net`, 15,662 `CobolNet`, 1,684 upper-case and 189 lower-case spellings of the compiler's first name, 446 of its
dotted product spelling, and 4,249 of the legacy engine's name (3,154 of them the kb tag), in 5,173 files; 30 tracked
paths (the six src projects, the five test projects, the solution, the 18 `docs/<prefix>_*.md` design documents).

**The mechanism, the PB3575 pattern.** `scripts/rename_retired_names.py` is the one re-runnable, idempotent rename: it
moves every tracked path that spells a retired name (`git mv` a directory at a time; into a directory a merge already
created, a file at a time), then rewrites the bytes of every tracked file through one ordered rule table, applying
`rename_diagnostic_prefix.py`'s rules first, so a branch older than both renames converts in one run. `EXCEPT` sets
aside the history (DEVLOG/, adjudication/, the frozen evidence, the legacy archive pointer that names the tree at its
tag), the two decision notes and the two rename scripts, and the per-machine `.claude/settings.local.json`; a
`retired-witness:` mark keeps its spelling. Its `--check` is the RETIRED NAMES audit: the gate driver's AUDITS, CI's
`audits` job, the battery, and the landing's CHECKS on the merged head (exit 6; the finisher brief's CONFLICT and exit-6
arms run the script). One case-insensitive pattern, a regular expression's spellings included. The run: 42 paths moved,
5,173 files rewritten (46,571 substitutions), then 9 more when the regular-expression rules were added (the inventory
schema's `implementation-pattern` had turned two DOC-A.1 rows GAP; the impact map's SourceGen pattern).

**The names chosen.** `WiseOwl.Cobol.sln`, `src/WiseOwl.Cobol.*`, `tests/WiseOwl.Cobol.Tests.*`, namespaces
`WiseOwl.Cobol.*` (the front end's, the editions leaf's, the CLI's and the runtime's roots are now their project names,
so PB2412, PB2413 and PB2414 landed with this; the compiler's root `WiseOwl.Cobol` stays PB2415's), identifiers
`WiseOwlCobol…`, the design documents without their prefix (`docs/DESIGN.md`, `docs/REARCHITECTURE_PLAN.md`, …), the
legacy engine's projects `Legacy.*`, the old checkout paths `E:/COBOL…`, the kb tag `wiseowl-cobol`. The environment
variables are `WISEOWL_COBOL_*`, never `COBOL_*`: `SwitchStore.Prefix` is `COBOL_`, the implementor-defined
external-switch family of §12.3.7.4 GR2 (`cite.py --check 12.3.7.4 "Switch-name-1 identifies an implementor-defined
external switch"` → OK), so an implementor variable there would be a switch a program can name. The NuGet id stays
`WiseOwl.COBOL` and the executable `cobol`. No alias, forwarder or reader of an old name (CLAUDE.md rule 4).

**What the rename moved under the audits, and the fixes at the root.** WITNESS LOSS read 10,982 re-spelled
code-locations as lost evidence: a code-location whose file git records as renamed, cited at the new path with its
member, is now RE-SPELLED (`inventory_schema.relocated`), as a moved test file already was. EVIDENCE SUPERSESSION read 92
frozen `path:line` citations of moved files as dangling: it resolves a path through the rename's map first. Eight
test-refs named tests whose names carried a code in the retired spelling: a `record_verdicts` batch retired the old
names and recorded the new (8 rows), and a second restated two DOC-A.1 rows; GAP 31 → 31. The member index re-recorded
at the renamed head showed six open Delete/Extract notes naming members already gone (PB2173, PB2181, PB2184, PB2186,
PB2206, PB2296); their lines now say so. The runtime ABI pin: `abi_pin.py bump --runtime-major`, 18 → 19, since every
public type's full name and the assembly's identity changed.

**By hand.** `ModuleRegistrationHostTests` names its module `WiseOwl` (program WISEOWL → `WiseOwl.dll`, the first segment
of the runtime's namespace), so the sibling probe still finds it by name; `install-autostart.ps1` now moves aside every
other Startup entry that runs `orchestrator/autostart.ps1`, so the renamed entry never starts a second supervisor
(`test_autostart.ps1` 13/13); `CorpusManifestTests` keeps the legacy differential switch by its suffix (the audit owns the
name); `RuntimeApiGuardTests` loses a dead lookahead arm; history prose the rename made self-referential (the owner's
PB2118 answer is quoted with bracketed present names); `CLAUDE.md`'s project paragraph and `Where things live`.

**Self-review.** Architecture: one rule table, one map (`renamed_path`) that the evidence audit reuses rather than a
second copy; the witness audit's new excuse is a separate argument, so a test-ref re-spelling still never excuses a
code-location (its self-test). Code: each rule has a self-test case asserting the result, that no retired spelling is
left, and idempotence. Performance: the witness audit reads git's whole-tree rename record only when a code-location
was lost (3.9 s on this change). Duplication: none added. Outside the repository, after landing: each account's
skill-doctor stamp is read under its new name, the Startup entry is replaced by re-running the installer, and the cloud
cache moves on its next setup (PB3837's Landing says how).

**The gate.** Run 1 was RED in the self-tests: the lower-case catch-all had spelled a case-insensitive scrub's test
input `wiseowl-cobol_gate_leg`; a rule now keeps a lower-case variable's underscore, and the audit also names a binary
file that spells a retired name instead of passing over it. Run 2 (leg 1 held the whole population) was RED on five:
the foreign-module CALL case's item that holds the runtime's name grew past its PICTURE (X(17) to X(21)); a
differential golden keyed by its source's hash moved to its new key; `EmittedGlobalNameDriftTests` now names the
captured root `WiseOwl`; the landing pipeline's arm names; and the architecture census re-recorded at the renamed head.
Run 3: `=== BUILD-LOCAL GATE: GREEN — Conformance 18,832/18,832 · Unit 38,392/38,392 · Characterization 36/36 cases ran
(skipped 0) in 1 of 1 leg(s) · Linux half (WSL clone of HEAD) GREEN … run 000003-20261026T204806Z) ===`.
