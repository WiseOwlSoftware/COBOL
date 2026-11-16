## PB4333: the program differential carries the Conformance cases byte-identical to a passed case, and a pass is keyed to its target's inputs

`gate_growth.py` had FIRED (a test-phase median of 653 s over 304 gates against 360 s; Conformance 27,004 cases), and the
changed-program fraction was 0.00 %, so by DESIGN-test-build-ci.md section 3.14.13 the differential was due.

**A pass is keyed to its target's inputs.** The question first: why did an inert one-file commit after a GREEN gate re-run
Conformance, Unit and RepoDrift (golden-2002-8, runs 000002 and 000003 of 2026-11-16)? Run 000002 ended GREEN with
`recorded: []`. `docs/REARCHITECTURE_PLAN.md` had been committed while it ran, and `PassCarry.record_passes` recorded nothing
once the tree moved, although no target's key had. That was a keying defect. The gate now computes every key again at the
end tree (`end_keys`, `keys_end`) and records each target whose key did not move. `carry` follows the same rule. The new
self-test arm fails under the old rule. This branch's run 000005 saw its tree move under a design-doc edit and still recorded
all four passes. The 41,072 against 40,963 Unit count was no population difference: both hosts ran 41,072, and the listing
names some rows once.

**The differential** (`scripts/gate_differential.py`). The suite's two theories cover 17,037 of 26,959 Conformance cases. Each
such case is carried when its case digest already passed on this host at the same harness key. The case digest covers the
theory, the row, the manifest entry, the named files' blobs, and the oracle's C#, diagnostics and program-bytes digests. The
harness key covers every `everything`-class input, the facets, the manifest's non-case content and the differential's own
inputs. The classification sits in `build_targets.json` `conformance.differential`, one class per declared input, and
`buildgraph.load_targets` refuses an unclassified input, so the BUILD TARGETS audit is its drift test. The digest is taken of
program bytes, not C#. Roslyn's emit is deterministic and carries no path: one program compiled in two directories was
byte-identical, and two captures of 21,574 compiles were identical once each WHEN-COMPILED program is written as `clock`.
The capture store reuses a compile whose compiler key and own inputs are unchanged. Unit and the other hosts start at once,
and only the Conformance host waits for its capture (a deferred plan in `Legs.run_hosts`). The merge group does not write a
manifest, because its token cannot write the repository. CI selects nothing: its Windows leg accepts the pass at the
Conformance key. CLAUDE.md's rule and the design were updated to match.

**The gate reds.** Run 000002 failed `ArchOracleDriftTests.AnObservation_IsPortable_AndRepeatable`, because a clock reader's
bytes differ on every compile. It also failed the standard-streams entry rule. Both were fixed. The first seeded gate, run
000004, selected exactly the altered case but reported 8 rows NEVER RAN. The cause was in the retry hook: `GateOnly.CasesOf`
never matched a long row by the cut-short name the listing gives it. That was fixed too, with
`GateLegDriftTests.Only_ALongRowNamedByItsListedForm_Runs`.

**Proof.** `scripts/selftests/gate/test_differential.py` has 4 cases and 16 expectations, and two mutations of the rule each
turn arms red. Run 000006 was the end-to-end check. A seeded compiler change that altered one program carried 17,036 cases,
ran 9,923, and was RED on exactly `l1c16_io_section_paragraphs` at 2002, with every population EXACT. Test phase, read on a
shared box: 575 s for the whole population, 314.6 s for a compiler change (a capture of 90.4 s) and 292 s for a change to
one case (1 compile, 6.5 s). Conformance is the larger share. Its floor is now the 9,853 cases of no carried family.

**The independent review (HUNT-gate-differential.md) found four ways to a false green, all closed before landing.** (F1) A compile that read the environment was reused from the capture store: `g2df04_parameter_environment` (a `>>DEFINE … AS PARAMETER`) had a hex program digest. Now any compile whose `CompilationInputs` record shows the environment, the repository, or a file or found probe outside its own directory is `ambient`, never carried. (F2) The capture compiled from the worktree root while the test host compiles from its output directory, and the default library searches the working directory first. The capture now compiles where the host does, and a case whose absent probe now exists runs. (F3) The shell's product variables (`COBOL*`, `WISEOWL_COBOL_*`) reached the test hosts, so passes could stand for runs under another environment. `product_clear_env` now clears them for every host and the capture, which also closes the hole in the whole-target store. The installed .NET runtimes and the locale are in the harness key. (F4) Seven protections had no arm. Each now has one, and an in-place mutation of each turns exactly its arm red. The design states that on Windows a carried case runs nowhere at the landed tree.

**Self-review.** I checked one mechanism per job (the carry, one plan and one population check serve both the retry and the
differential), that no classification is copied anywhere, that the cited rule is the brief's rule (the selection is a
superset of the changed cases), the hot paths (a set lookup per case) and the silent-failure paths: every failure leaves the
whole population to run. The gate's verdict line is in the report.
