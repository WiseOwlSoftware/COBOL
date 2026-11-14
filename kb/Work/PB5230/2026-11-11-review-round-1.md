# PB5230 review round 1 (eb6a33ce66; E:/COBOL-coord/scratch/review-w20261997-a.jsonl): four findings and one lead, fixed

With Unit's inputs narrow, the reviewer made a Unit test read a file outside Unit's key while the BUILD TARGETS audit stayed
GREEN, five ways. Each is fixed in `scripts/buildgraph.py` (the audit's reader and verdict) and `scripts/build_targets.json`.

**R1-1 (high): an unresolved root in a narrow target was only counted.** Planted: `File.ReadAllText(TestRepo.At($"tests/
differential/{name}"))` and `Path.Combine(TestRepo.Root, $"tests/differential/{name}")` in Unit: GREEN, "unit 2", and
editing the file left Unit's key unchanged. Fix: `findings_of` refuses every bare root in a target that does not declare
`@population-inputs` (`Target.broad`), naming file, line and statement; only `repo-drift` counts its roots. An interpolated
argument's literal head is read (`$"tests/differential/{name}"` reads `tests/differential/`), so both planted reads are now
plain findings. Hand judgments of roots are per STATEMENT (`<file>|<the line, trimmed>`), never per file, and a judgment
nothing matches is a stale finding. Conformance's 5 and Characterization's 4 roots were each resolved to a statement
judgment with a true reason: ArchOracle.Portable's `(TestRepo.Root, "<repo>")` and ArchOracleDriftTests' `machineSpecific`
(the root as text, replaced or asserted absent), CharacterizationCorpus' `RepoRootFwd` (stripped from snapshot text); the
three shared statements (TrackedTree's `git ls-files` working directory, PythonInstrument's working directory,
GateLegAudit's `RootCarrying(…, HostRoot)`) are judged once under `build.judged` for every target that uses the file.
GateLegAudit's alias declaration is no root (its uses are judged where they are, R1-5).

**R1-2 (high): every `{TestRepo.Root}` hole was dropped as text.** Planted: `File.ReadAllText($"{TestRepo.Root}/tests/
differential/AlterSwitchesDifferentialTests/0df6ef7af2a7eb37.out")`: GREEN, not even counted. Fix: `_root_use` with a
small scanner of C# strings and comments (`_context`). A use is TEXT exactly when it lies in a comment or a literal's text,
is made relative or named (`GetRelativePath`, `cref`, `nameof`), or is a hole neither neighbour of which is a path
separator or the string's own quote. A hole followed by a separator reads the literal path after it up to the next hole; a
hole preceded by a separator, a whole-string hole (`$"{TestRepo.Root}"`, which flows wherever it is passed), a formatted hole
and a `{TestRepo.Root}` outside a string are bare roots. Nothing unclassified is text.

**R1-3 (medium): the TrackedTree judgment's reason was false for a prefix without `/`.** Planted: `new(Under: ["tests"],
Kinds: [".out"])`: GREEN, "unit 0". Fix: every `Under` prefix of every TreeScope is a read the audit records itself: a
prefix ending in `/` that directory, any other the directory holding it, a top-level one (`"tests"`, `""`) the whole tree,
a bare root (refused in Unit). The TrackedTree judgment is kept, one statement, with a reason that is now true.

**R1-4 (medium): three populations still named, and a check required the old phrase.** Fix: the gated assemblies have ONE
statement, `scripts/build_targets.json` (targets ordered as the gate starts hosts): `test_population.GATED_ASSEMBLIES` is
read from it (a CODE_READS entry in inert_lane.py), RepoDrift's `GateLegDriftTests.Arm4_EveryTestProject_…` checks the
test projects and the committed weights' assemblies against it (Unit's weights arm no longer lists them), and
check_practices.py requires the phrase "the WHOLE population of every test target (`scripts/build_targets.json`)", which
the implementer template, MANDATORY-PRACTICES I1, DOC_INDEX, build-local.sh/.ps1, the gate skill, CLAUDE.md, the plan and
DESIGN-test-build-ci.md now say. I8 names the `repo-drift` leg and says the drift tests run there. The comments in GateLegs.cs,
VacuousTestDriftTests and FileHarnessOneHomeDriftTests, CONTRIBUTING.md, three design docs' battery lists, linux-gate.sh,
perf_baseline.py and the 2026-11-01 measurements' labels were swept. Left by name: CI's workflow steps and linux-gate.sh's leg
vocabulary (PB5235 moves CI onto the targets).

**R1-5 (lead): `GateLegAudit.HostRoot`, a public alias of the root, hid any read built from it.** Fix: `ROOT_ALIAS` finds a
member that IS the root; `root_aliases` makes every `Type.Member` use, and every bare use in the declaring file, a use of the
root, judged by the same rules. Planted `Path.Combine(GateLegAudit.HostRoot, "tests", "differential", f)` reads
`tests/differential/`, a finding; Unit's one text use (arm 5's `"host"` witness) is a statement judgment.

**Proof.** The reviewer's planted reads in one Unit source, `python scripts/buildgraph.py check`: `=== BUILD TARGETS: RED — 7
finding(s) ... ===`, each naming `tests/WiseOwl.Cobol.Tests.Unit/PlantedReviewReads.cs` and its line (lines 13, 14, 15
tests/differential reads; 17 the alias read; 18 `TestRepo.At(name)`; 19 `$"{TestRepo.Root}"`; 8 the `"tests"` scope). Removed:
GREEN, 4 targets. The round-0 reader (eb6a33ce66) read the same planted source as one bare root and nothing else.
`buildgraph.py --self-test`: PASS, 13 arms (four new: the interpolation and literal-head reads, the scope prefixes, the
alias, the narrow-target verdict with a statement judgment and a stale one). check_practices over an implementer spec
rendered by make_dispatch_specs.py: GREEN; the same spec with the old phrase: `FAIL ... missing ['the WHOLE population of
every test target ...']`. The other proofs (gate and test_population self-tests, work.py check, the gate) are in the branch
report.

**Counts corrected:** 173 cases left Unit (Unit kept 40,573 of 40,746 and gained 1; RepoDrift's 176 are those 173 and its 3
new per-assembly arms). Round 1 adds no test case: the weights equality moved into RepoDrift's existing Arm4 test.
