## Landed (kb/Work PB4333, wave 20262059 group A, 2026-11-16): the program differential is built, and a pass is keyed to its target's inputs

**Sites:** `scripts/gate_differential.py` (new: `harness_key`, `compiler_key`, `case_inputs`, `case_digests`, `CaseStore`,
`CaptureStore`, `select`, `Differ`), `scripts/gate.py` (`Gate._tested`: the deferred Conformance plan, `_with_differential`;
`_verdict`, `_record`, `_record_growth`), `scripts/gate_legs.py` (`Legs.run_hosts`: a deferred plan, `NO_HOST`),
`scripts/gate_reuse.py` (`PassCarry.end_keys`, `record_passes`, `carry`), `scripts/gate_verdict.py` (`Outcome.keys_end`,
`Outcome.differential`), `scripts/gate_host.py` (`Host.tree_entries`, `Host.oracle_digests`, `BESIDE`),
`scripts/buildgraph.py` (`DIFFERENTIAL_CLASSES`, `Differential`, `_differential`, `load_targets`),
`scripts/build_targets.json` (`conformance.differential`), `tests/WiseOwl.Cobol.Tests.Conformance/ArchOracle.cs`
(`Observation.Program`, `ProgramDigest`, `ClockProgram`, `Fold`, the `digests [--only]` verb), `tests/_shared/GateLegs.cs`
(`GateOnly.CasesOf`, `Truncation`), `scripts/gate_fake_host.py`, `scripts/selftests/gate/test_differential.py`,
`scripts/selftests/gate/test_runs_and_locks.py`, `tests/WiseOwl.Cobol.Tests.Unit/GateLegDriftTests.cs`.

**Why the trigger's remedy now.** `gate_growth.py` FIRED (test phase median 653 s over 304 gates against 360 s; Conformance
27,004 cases), and the changed-program fraction was 0.00 % (2026-11-01), so by section 3.14.13 the differential is built.

**1. A pass is keyed to its target's inputs (the inert one-file commit that re-ran three targets).** golden-2002-8's run
000002-20261116T031738Z ended GREEN with `recorded: []`: `docs/REARCHITECTURE_PLAN.md` was committed while it ran (tree
1eb53e65 at the start, 8d339011 at the end), and `PassCarry.record_passes` recorded nothing when the tree moved at all. The
keys of Conformance, Unit and RepoDrift were identical at both trees, so run 000003 re-ran them. A keying defect: the gate now
computes every key again at the end tree (`end_keys`, recorded as `keys_end`) and records a target whose end key equals its
start key; `carry` takes the same rule. Proved: the new arm of `test_runs_and_locks.py` fails under the old rule (`recorded
[]`) and passes now, and this branch's own run 000005-20261116T091855Z, whose tree moved by a design-doc edit while it ran
(c3096e82 to 0e6a155d), recorded all four passes. The 41,072 against 40,963 Unit figure was not a population difference:
both runs' hosts ran 41,072 results; 40,963 is the listing's count, which names some theory rows once.

**2. The program differential.** A Conformance case of the suite's two theories (`SuiteTest_IsNoWorseThanItsVerdictAllows`,
`WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic`) is carried, never run, when its CASE DIGEST (theory, row, manifest entry,
named files' blobs, and the oracle's C#, diagnostics and PROGRAM-BYTES digests) already passed on this host at the same
HARNESS KEY (every `everything`-class input of the Conformance target, its facets, the manifest's non-case content and the
differential's own inputs). The classification is the target's own declared inputs (`build_targets.json`
`conformance.differential`), and `buildgraph.load_targets` refuses an unclassified input, so the BUILD TARGETS audit is the
drift test (the impact map's failure, PB2696 and PB4228, was a hand list a new read escaped). Program bytes, not C#:
Roslyn's emit is deterministic and path-free (probed: one program compiled in two directories is byte-identical; two captures
of 21,574 compiles are identical line for line once a WHEN-COMPILED program is named `clock`). The capture store reuses a
compile whose compiler key (harness key and every `program`-class entry) and own inputs are unchanged.

**Design change against the note's first text** (recorded in DESIGN-test-build-ci.md section 3.14.13): the manifest is not
written by the merge group. Its token reads the repository and never writes it (section 3.14.14: "The ref adds no new
writer"), and a manifest of main's programs would still need a pass of each case at the same harness; the store already holds
passes keyed by inputs. CI selects nothing: its Linux leg runs every case, its Windows leg accepts the pass at the Conformance
key, and CLAUDE.md's rule now reads "or is byte-identical in every input to a case that ran green on this host".

**Proof.** `scripts/selftests/gate/test_differential.py` (4 cases): a changed program, expected output and diagnostic
stream are each selected; a new case runs; a compiler change whose programs are identical carries them; a harness change and a
runtime change run everything; a missing set runs everything; a failed capture carries nothing; a red among the cases run is
RED; an unclassified input is a GraphError; a three-week-old set is pruned. Two mutations (carry every digest; leave inputs
out of the harness key) each turn arms red. End to end: a seeded compiler change that alters only the program displaying
"A NEITHER PARAGRAPH" (run 000006-20261116T093113Z) carried 17,036 cases, ran 9,923, and was RED on exactly
`SuiteTest_IsNoWorseThanItsVerdictAllows(edition: "2002", id: "l1c16_io_section_paragraphs")`, every population EXACT.

**Found and fixed on the way:** the retry hook (`GateOnly.CasesOf`, PB5099) never ran a long theory row the listing names in
its cut-short form (`···`): the first seeded gate (run 000004) reported 8 UniversalCrossingShapeDriftTests rows NEVER RAN.

**Measured** (the 32-core box, not alone: other fleets' gates ran beside every run): whole population, run 000005: test
phase 575 s (Conformance 574.5 s, Unit 288.1 s, RepoDrift 148.9 s); differential on a compiler change, run 000006: 314.6 s
(capture of 17,043 compiles 90.4 s, then the Conformance host 220.4 s for 9,923 cases; Unit 219.6 s beside); differential on a change to one case's source, run 000008-20261116T094725Z: 292 s (17,042 compiles reused from the capture store, 1 compiled, in 6.5 s; Conformance 282.6 s; Unit 273.3 s). Conformance is
the larger share of the test phase, and its floor is now its 9,853 cases of no carried family.

**Left (the next levers, not this note's):** carrying the oracle's other enrolled families (NIST, the version matrix, the
optional-word sweep), whose compiles are already captured, needs only their own row rule; the hand-written theories compile
inline source and need a digest of their own; Unit is the next whole population.
