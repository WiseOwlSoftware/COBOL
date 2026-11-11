## PB5230: the repository drift tests leave Unit for their own assembly and target, and Unit's inputs are declared narrowly

Stage 2 (first half) of the build redesign the owner approved in kb/Work PB5207: "This is precisely what the Unix make and
Windows MSBuild utilities are for. They rerun only what is required due to changes."

**What changed.** The Unit assembly held the compiler's unit tests and the repository's drift tests together, so its target
declared every population input. The drift tests are now `tests/WiseOwl.Cobol.Tests.RepoDrift` (target `repo-drift`, every
population input), and `unit` declares `@build`, its own project, the data its sources read, and the inputs of the one
process its tests run (SuiteManifest's `generate.py`). The BUILD TARGETS audit decided the split: every Unit source whose
reads lie outside those inputs, or whose reads it cannot resolve, moved, and six classes holding both kinds were split by
method. A planted Unit read of `docs/rearchitecture/architecture-edges.json` is now `=== BUILD TARGETS: RED — 1 repository
read(s) outside their target's declared inputs ... ===`; before, `@population-inputs` covered it.

**The 107 bare root references.** Unit has none now. The audit learned to tell a root used as text (35
`Path.GetRelativePath` sites, messages) from a read, to read collection expressions, and to read a new helper,
`TestRepo.Within("src/…/", rel)`, which declares the directory a computed path lies in and throws when it does not. Twelve
Unit sites use it. Three shared files are judged by hand, each with its reason (TrackedTree, GateLegAudit.HostRoot,
PythonInstrument). Every other unresolved read left with its test.

**The population is conserved.** Unit had 40,746 cases. It now has 40,574 and RepoDrift 176. The four extra cases are each
assembly's own gate arms: Unit's `Arm4_ThisAssembly_NamesTheGateFramework`, and RepoDrift's `Arm5_NoDiscoveredCase_...`,
`ThisTestHost_IsConfiguredForServerGc` and `ThisAssembly_EmbedsTheShardWeights`. 173 cases left Unit (RepoDrift's other 3 are its new arms, Unit's 1 new is its Arm4); they changed only their
namespace. `test_population.py list` gave each count; the case-for-case check is in the branch report.

**Which targets a change re-runs.** Measured with `buildgraph.key` on real pairs of trees from main, before (main's targets
file) and after (this branch's). Facets: windows, Debug, one toolchain.

| Change | Commit | Before | After |
|---|---|---|---|
| docs only (`docs/CONFORMANCE.md`) | `fd3daa3764` | none | none |
| kb/Work only | `9bc90ee36c` | none | none |
| scripts only, tooling lane (`scripts/hooks/`) | `38f1836f88` | none | none |
| scripts/spec only (`rule_catalog_1985.py`) | `53f6674bf0` | unit, conformance | repo-drift, conformance |
| src only (`StatementBinder.cs`) | `95d7cd73bc` | unit, conformance, characterization | unit, repo-drift, conformance, characterization |
| records only (193 inventory-1985 rows) | `ca5bfe1b03` | unit, conformance | unit, repo-drift, conformance |
| a Conformance test source only | `813e6704bd` | unit, conformance | repo-drift, conformance |
| a workflow only (synthetic: another blob for `build-and-test.yml`) | main | unit | repo-drift |

So a workflow, `scripts/arch/`, an architecture record, a `scripts/spec/` module the generator does not import, or another
project's test source no longer re-runs Unit's 40,574 cases. A records change still does, through the suite generator. That
ends when kb/Work PB5231 makes the generator a target with an output and Unit keys on the output.

**Test seconds.** Before, Unit's host took a median 341 s over 20 recent green gates (141 to 606 s, load-dependent). In the
newest of them (wf_ea078e0c-0d9-4, run 000001), the cases that are now RepoDrift's held 508 of Unit's 7,543 case-seconds
(6.7 %). After, on this branch: RepoDrift alone ran in 75 s (176 passed), and Unit alone in 410 s (40,683 passed; the one red, the committed-weights arm expecting three assemblies, is fixed in this change). The gate's per-host seconds, all four side by side, are in the branch report.

**Also changed.** InternalsVisibleTo for RepoDrift in the compiler. `GATED_ASSEMBLIES`, the gate's self-test (four
assemblies), linux-gate.sh, the battery, CI's shard jobs and `ci-gate`. The weights of the 23 classes that moved whole. A
`repo-drift:` test-ref form, with one inventory row repointed and normalize_batch reading the forms from the schema. drift_rules.py
reads `Within`. TrackedTreeScopeDriftTests reads the compiled scopes of both assemblies. The docs that name the populations.

**Review round 1** (kb/Work PB5230/2026-11-11-review-round-1.md) found the audit still let a Unit test read outside Unit's key
while it stayed GREEN, and fixed five ways. A bare root in a narrowly declared target is now a finding naming file and line,
not a count, and a hand judgment covers one statement, never a file. A root in an interpolated string is text only when
neither neighbour is a path separator or the string's own quote; `$"{TestRepo.Root}/tests/x.out"` reads `tests/x.out`.
Every `Under` prefix of a `TreeScope` is a read (`"tests"` is the whole tree). A member that is the root
(`GateLegAudit.HostRoot`) is followed to its uses. Conformance's 5 and Characterization's 4 roots were each judged as the
statement they are: the root as text in ArchOracle, ArchOracleDriftTests and CharacterizationCorpus, and three shared
statements (TrackedTree's `git ls-files`, PythonInstrument's working directory, arm 5's RootCarrying) judged once for every
target. The reviewer's seven planted reads are each RED with file and line. The gated assemblies now have one statement,
`scripts/build_targets.json`: `GATED_ASSEMBLIES` reads it, RepoDrift's GateLegDriftTests checks the test projects and the
weights against it, and the implementer spec, MANDATORY-PRACTICES and the docs name it instead of listing assemblies.

**Proof.** `python scripts/buildgraph.py check`: GREEN, 4 targets; only repo-drift counts bare roots. `buildgraph.py
--self-test`: PASS, 13 arms; the round-0 reader read the planted source as one bare root and nothing else. `gate.py
--self-test`, `test_population.py --self-test`, `check_practices.py` over a rendered implementer spec (and RED over the
same spec with the old phrase), `work.py check`. The gate of this branch is in the branch report.

**Left.** A records change re-runs Unit until PB5231. The six split classes' RepoDrift halves carry the default weight until
`shard_weights.py generate` measures them. CI's workflow and linux-gate.sh still list the assemblies by name; the operator's
PB5235 moves CI onto the targets.
