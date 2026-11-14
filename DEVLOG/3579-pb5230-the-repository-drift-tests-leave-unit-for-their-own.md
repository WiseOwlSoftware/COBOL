## Entry 3579 — 2026-11-13 19:28 PST — PB5230: the repository drift tests leave Unit for their own assembly and target, and Unit's inputs are declared narrowly; The FROM/INTO record-area rule ends at 2002, as the 2014 text says (PB5270); COBOL-85 arms of the function-identifier, EVALUATE, DEBUG-ITEM, INSPECT sizes and the INTO/FROM citations (PB5163, PB5183, PB5209, PB5297, PB5425, PB5352)

### PB5230: the repository drift tests leave Unit for their own assembly and target, and Unit's inputs are declared narrowly

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
| scripts/spec only (`rule_catalog_1985.py`) | `53f6674bf0` | unit, conformance | repo-drift |
| src only (`StatementBinder.cs`) | `95d7cd73bc` | unit, conformance, characterization | unit, repo-drift, conformance, characterization |
| records only (193 inventory-1985 rows) | `ca5bfe1b03` | unit, conformance | repo-drift |
| a Conformance test source only | `813e6704bd` | unit, conformance | repo-drift, conformance |
| a workflow only (synthetic: another blob for `build-and-test.yml`) | main | unit | repo-drift |

So a workflow, `scripts/arch/`, an architecture record, a `scripts/spec/` module, another project's test source or a
records-only change no longer re-runs Unit's 40,574 cases. After merging kb/Work PB5231 (main d9dde0e7ba), Unit DEPENDS on the
generated `conformance-suite` target as Conformance does: its key takes the suite's output digest, not the generator's inputs.
The After column was measured on the merged graph, with the suite generated at both trees of a pair whenever its own key
changed: at `ca5bfe1b03` and `53f6674bf0` the suite was byte-identical (`4931a9c24c88…`, `38c59fe8875b…`), so Unit and
Conformance are up to date there. RepoDrift reads the inventory itself and depends on nothing.

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

**Review round 2** (kb/Work PB5230/2026-11-11-review-round-2.md) found three more ways past the audit. A literal `..`
made it drop a read as a path no file has: reads are now judged at their resolved path, a climb out of the repository is a
finding, and a read naming no tracked path is dropped only when it is a build output (`bin`/`obj`) or git ignores it;
anything else in a narrow target is a finding (14 today, each judged: a test's own scratch names and one file asserted
absent). A path formed around TestRepo was invisible: the one-home rule of the locator (`TestRepoDriftTests.Anchors`, read by
the audit itself) is extended, so a narrow target reaches the file system only through TestRepo and ScratchDirectory or a
judged statement. It refused Unit 21 statements, Conformance 8 and Characterization 6 (six of them the shared TestRepo and
HostLoad lines); each was judged with its reason, since none read the repository: compilation references to loaded
assemblies, `/proc` counters, the working directory as expected text, the host's build output. The PythonInstrument
judgment's reason now covers all ten callers. This audit is the early warning; the sound net is PB5235's dynamic proof,
the only check that sees a child process's, native code's or reflection's paths. The reviewer's planted reads (round 1's
seven, round 2's four files) are each RED with file and line.

**Review round 3** (kb/Work PB5230/2026-11-11-review-round-3.md) found the audit's C# scanner hiding code. A verbatim
literal ending in an escaped quote (`@"x"""`, in at least three drift tests) holds three quotes, so the three-quote pattern
for raw literals paired two of them and read every line of code between as string text. The pattern and the line-start
scanner beside it are replaced by ONE lexer of the whole file (`_lex`), which follows every literal form, so the same
sweep closed three siblings: a character literal `'"'` hid the rest of its line, a block comment read as code, and a
multi-line verbatim literal's later lines read as code. Code inside an interpolation hole now counts as code for the
bypasses; it found one more TestRepo line (its refusal message names the base directory), judged. Over the 1,115 tracked
test sources the new lexer changes no read and that one bypass.

**Proof.** `python scripts/buildgraph.py check`: GREEN, 5 targets (conformance-suite generated); only repo-drift counts
bare roots. `buildgraph.py --self-test`: PASS, 33 arms; the round-0 reader read the planted source as one bare root and nothing else. `gate.py
--self-test`, `test_population.py --self-test`, `check_practices.py` over a rendered implementer spec (and RED over the
same spec with the old phrase), `work.py check`. The gate of this branch is in the branch report.

**Left.** The six split classes' RepoDrift halves carry the default weight until
`shard_weights.py generate` measures them. CI's workflow and linux-gate.sh still list the assemblies by name; the operator's
PB5235 moves CI onto the targets.

### The FROM/INTO record-area rule ends at 2002, as the 2014 text says (PB5270)

Wave 20262004 group G (PB5183, PB5163, PB5209, PB5270). This branch continues w20261998c (PB5120 and its siblings, merged in
from worktree-wf_c902d12a-c75-3); the merge kept both sides of main's new DialectBehavior rows 8 to 13 and VCR rows 7.58 to 7.63,
so the INSPECT BEFORE/AFTER row PB5120 added is now DialectBehavior 14 and VCR row 7.64.

**PB5270.** Re-probed on the branch build: `WRITE FR FROM FR` and `READ F INTO FR` drew COBOL6827 at `--std 2014` as at 2002,
though `§2014:E.3` lists all five phrases (READ INTO, RELEASE FROM, RETURN INTO, REWRITE FROM, WRITE FROM; items 21, 24, 25, 26
and 38) as no longer prohibited. The rule had been kept at 2014 by a DETERMINATION made before the 2014 text was in the
repository. `RecordAreaRule.InForce` is now `editionYear < 2014`, and the COBOL6827 descriptor names the 2014 change. The five
`pb5077-*-own-record-area` refusals refuse at 85 and 2002; the new golden `pb5270_from_into_own_record_area_2014` runs all five
phrases from 2014 and prints only the defined results (I-O status, the record count the SORT returned). Docs: CONFORMANCE §3.5,
FILES_DESIGN D31 (b), VCR row 7.68. No inventory row closes: there is no 2014 inventory, and the 2023 rows never carried the rule.

PB5183, PB5163 and PB5209 were re-probed and still reproduce; the fleet stop arrived before their fixes (see the branch's
STATUS.md for the next step).

### COBOL-85 arms of the function-identifier, EVALUATE, DEBUG-ITEM, INSPECT sizes and the INTO/FROM citations (PB5163, PB5183, PB5209, PB5297, PB5425, PB5352)

Wave 20262020 group A. This branch finishes w20262014a (whose implementer died on an API 403): it merges
`worktree-wf_7021f6f6-0af-1`, which carries PB5230 (the repository drift tests leave Unit), PB5270, PB5309 and the w20262013a
Report Writer group (landed separately as PR 1599 and merged back from main). The merge took main's side of the dead finisher's
uncommitted PB5378 halves, because main had already landed PB5378's review answers; its shared C# lexer
(`scripts/csharp_lexer.py`) passes its seven cases and `buildgraph.py --self-test` (36 arms). One merge fix-up: main's PB5196
drift test reads the 2002 and 1985 texts from Unit, so `scripts/build_targets.json` declares both (BUILD TARGETS was RED, now
GREEN). Every note below was re-probed on the branch's own build first; each reproduced.

**PB5163.** `FUNCTION RANDOM ()` and `FUNCTION CURRENT-DATE ()` compiled at `--std 85`. The edge is 2002, not 2023: ISO/IEC
1989:2002 8.4.2.2.1 brackets the argument inside the parentheses and its SR4 NOTE writes `FUNCTION RANDOM ()`. New construct
`function-empty-argument-list-2002` (COBOL0900), gated by recognition in `VersionConformancePass.ParseArm.VisitFunctionCall`.

**PB5209.** `FUNCTION MAX (TE (ALL) (2:1))` was refused COBOL2363 at every edition: `TryBindAllArgument` gave up on any suffix
after the subscripts. It now takes the modifier through the one reference-modification tail (`ScreenRefModCount`,
`ReadScreenedRefMod`, `RefModView`) over the element's place, so the backend's enumeration modifies each occurrence
(§1985:IV.4.3.8.4.4 GR3). Goldens at 85 and in the 2023 tree.

**PB5183.** At 85 an EVALUATE object no WHEN phrase reached was never evaluated, against §1985:VI.6.13.4 GR1 ("operates as if each
selection subject and selection object were evaluated"). New `DialectBehavior.EvaluateObjectsInWhenPhraseOrder` (2002, VCR row
7.69): where it is inactive, `EvaluateBinder.Bind` holds the truth of each pair whose object references a function at the head of
the statement (`SendingValueTemp.MaterializeTruth`). Design D5 of docs/CONDITIONS_EXCEPTIONS_DESIGN.md records it.

**PB5297.** `MOVE "A" TO TE (DEBUG-SUB-1)` outside a debugging section compiled: the position is evaluated into a temporary by a
pre-op still pending when the funnel asked `ReferencesDebugItem`, which now asks those pre-ops too (COBOL6442).

**PB5425.** The 1985 INSPECT size rules skipped `W3 (2:)` and `FUNCTION UPPER-CASE (W3)`. `IntrinsicBinder.KnownWidth`, the one
static size reader, now reads an open reference modification's remaining positions and a function's fixed length, from a new
`IntrinsicSig.Length` column (`IntrinsicResultLength`: CHAR 1, CURRENT-DATE and WHEN-COMPILED 21, LOWER-CASE, UPPER-CASE and
REVERSE argument-1's length). Six refusals (COBOL0846), one control, `IntrinsicResultLengthTests`.

**PB5352.** A COBOL-85 READ INTO refusal cited ISO §2023:14.9.30.3 SR1. New `EditionCite` (a rule's 2023 citation and its 1985
one per I-O module); `IntoPhraseRules` and its twin `FromPhraseRules` cite the admissibility rule, its arms and the MOVE rule as
the edition numbers them. Three 85 refusals pin the message; `EditionCiteTests` pins the selection.

**Inventory.** 1985 rows re-verdicted CONFORMS through five `record_verdicts --edition 1985` batches (w20262020a-pb5163, -pb5209,
-pb5183, -pb5297, -pb5425): 1989-FMT-A.2.35.2, 1985-FMT-IV.4.3.8.3.2, 1985-SR-IV.4.2.1-4, 1985-FMT-IV.4.3.8.4.2,
1985-FMT-IV.4.3.8.5.2, 1985-GR-VI.6.13.4-1, 1985-SR-XV.3.2.3-11, 1985-SR-VI.6.18.3-7, -8 and -9 (1985 GAP 279 to 269). PB5373
stays open: its remaining rows wait for PB5374's tooling and PB5308.

**Self-review** (architecture, full code, performance, duplication): each rule is asked at the one place its siblings are (the
parse arm for the format gate, the reference-modification tail, the statement funnel, the one size reader, one citation type
for both phrase records); the EVALUATE hoist adds code only to an 85 pair whose object references a function; no second copy of
a rule was added. The gate's verdict is in the branch's report.


**The merge-group bounce (run 43552030404).** The first queueing of this entry failed `ci-gate`'s population check on both
OSes with `FileNotFoundError: scripts/build_targets.json`: `test_population.py` now reads the targets file at import, and
`ci-gate` checks out only a sparse list that did not hold it. The audit of those lists, `scripts/sparse_checkout.py`,
followed a script's imports but not the files it reads. It now also follows each file a Python script reads by a literal
path (a `/` chain or a string constant in the receiver of `read_text`, `read_bytes` or `open`, or the argument of `open(`),
outside the script's own `self_test`, and treats such a file as data. `ci-gate`'s two lists now name the file. With that
line reverted, `--check` is RED on exactly the file and the new real-tree self-test arm fails (kb/Work PB1957, class (e)).
