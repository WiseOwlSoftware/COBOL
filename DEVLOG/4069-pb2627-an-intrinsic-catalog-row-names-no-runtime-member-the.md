## Entry 4069 — 2026-11-25 09:17 PST — PB2627: an intrinsic catalog row names no runtime member; the renderer keys on the function and names each body through RuntimeApi; PB2724: the GLOBAL and EXTERNAL clauses are each one grammar rule, and the EXTERNAL AS phrase and GR5 default are each read once; PB2627 and PB2724 carried onto main; PB2622 and PB3231 re-measured and left for their own dispatches; At --std 2002 and 2014 the CALL, INVOKE, SET, MOVE and repository diagnostics cite their edition's rule, and a net asks it; SetBinder becomes the SET format dispatch over seven family binders; four census clone pairs become one body each; the RuntimeApi ratchet reaches zero; C# 15 `closed` hierarchies, wave 1: the bound tree and the binding model's Place and AccessPath families are closed

### PB2627: an intrinsic catalog row names no runtime member; the renderer keys on the function and names each body through RuntimeApi

**What was wrong.** `IntrinsicSig.RuntimeMethod` looked like the runtime body a function runs, but the renderer used it as
a dispatch key. The MAX row said `"MaxScaled"`, yet a MAX call emits `MaxAt`, `MaxDec`, `MaxExact`, `MaxWide` or `MaxReal`,
never `MaxScaled`. Because the key matched a method name, two dead public bodies stayed in the reachability census as
emitted text: `CobolIntrinsics.OrdMax` and `OrdMin`. The binder also picked a body name at bind time
(`sig with { RuntimeMethod = "MaxString" }`), so the bound tree carried a renderer key. The bound layer's
`FunctionValueLane` sets used that same key space.

**Re-probe (ccb23d13e4).** The column was still the key: 98 lines of `src` named it, 73 of them in the renderer, the rest in the lane, `FunctionDeviceText` and
the binder. A grep of `src` and `tests` found no emitter of `OrdMax` or `OrdMin` (now `params ReadOnlySpan<Int128>`). Their
only callers were in `SelectionRuleCarrierAgreementTests`. `MaxScaled` and `MinScaled` were called only by `RangeScaled`
and `MidrangeScaled`.

**Fix.** The catalog row has no member-name column. The function's catalog name is the one key, as kb/Work PB4370 already made
it for `IntrinsicCarrierRules`. That covers every renderer switch (`RenderNum`, `RenderDec`, `RenderExactUnary`, `RenderWide`,
`RenderWholeAligned`, the string channel), `FunctionDeviceText`, `FunctionValueLane`'s sets, `StandardValueFixedByRule` and
`EquivalentExpressionUnaryArms`. Each body an arm emits is a `nameof`-anchored constant in `RuntimeApi` (`IntrinsicBody`,
`DateBody`, `LocaleBody`, `EcBody`: 184 names), the façade `RuntimeApiGuardTests` holds every runtime name to. The `XxxScaled → XxxReal` name convention
(`RealMethod`) is now one function-keyed table, `IntrinsicRenderer.Binary64Bodies`. The binder records its string-comparison
choice as a fact on the call, `BoundIntrinsicCall.ComparesStrings`. Each arm that used to exclude the string body by its key
now asks that fact, at the same point in the flow. `OrdMax` and `OrdMin` are deleted. `MaxScaled` and `MinScaled` are private
helpers of RANGE and MIDRANGE. They were Unshipped API, so the ABI pin does not move.

**Drift tests.** `IntrinsicCarrierRuleKeyDriftTests.EveryBackendFunctionKey_IsACatalogFunctionName` checks every function-keyed
table and every string arm label against the catalog. `IntrinsicRealArgDriftTests` checks that `Binary64Bodies` covers every
function whose argument run can carry a binary64, and fails on any `…Real` body that no row names.
`IntrinsicCarrierAgreementDriftTests` now names an arm by the bodies it emits, not by its label. Eleven source-reading tests
moved from the old key space to the function names.

**Self-review: the arm the oracle could not see.** Under the old key the string form was excluded from the numeric bodies
by its spelling (`"OrdMaxString"` matched none of them). Under the function key, each arm has to say so. The first cut missed
`RenderDec`: under ARITHMETIC IS STANDARD (2002) every call reaches its switch, and `FUNCTION ORD-MAX` over `"BBB" "CCC" "AAA"`
answered 001 from `OrdMaxDec` over the strings. A probe on the build showed it (`ORD-MAX=001`), and the guard
`if (ic.ComparesStrings) return null;` restored 002. The oracle corpus has no such call, so the golden
`15.58@2002-ORD-MAX-FUNCTION/g2-ord-max-strings-arithmetic-standard` now pins it (§2002:15.58.3 RV1-RV3: COMPUTE and MOVE
002, a tie 002, a space-padded tie 001). Every other numeric arm keyed on MAX, MIN, ORD-MAX or ORD-MIN was swept: `RenderNum`'s
selection arms, `SelectionPastReceiverHeadroom`, `RenderWide`, `FunctionDeviceText.CallField` and `FunctionValueLane`. Each
asks `ComparesStrings` at the point where the old key fell through.

**Neutrality.** `python scripts/arch/compare_oracle.py --base ccb23d13e4`:
`=== ARCH-ORACLE: IDENTICAL — 23499 cases (baseline ccb23d13e470, new ccb23d13e470-dirty) ===` before the golden, and after it
and the `RenderDec` guard `=== ARCH-ORACLE: DIFFERENT — 1 of 23500 cases differ ===`, the one line being
`ADDED suite/2002/g2-ord-max-strings-arithmetic-standard` (no CSHARP, DIAGNOSTICS or REMOVED line).

**Rows.** Eleven inventory rows cited a deleted or retired member (`RealMethod`, `OrdMax`, `OrdMin`, `MaxScaled`, `MinScaled`).
They were re-sited by `record_verdicts` batches: `RV-15.22.4-1`, `RV-15.47.4-1`, `RV-15.59.4-2`, `RV-15.63.4-1`, `RV-15.71.4-3` and
`RV-15.72.4-3` at 2023 (GAP 1 → 1); `1989-GR-A.2.32.1-p1` and `1989-GR-A.2.33.1-p1` at 1985 (GAP 0 → 0); `2002-RV-15.37.3-1`,
`2002-RV-15.58.3-3` and `2002-RV-15.59.3-3` at 2002 (GAP 1150 → 1150). The first batches missed six of them: the grep looked for
`OrdMax` but not `OrdMin`, and not in the 2002 store. Gate 3's `EveryCodeLocation_ResolvesInTheTree` named five, and the sweep
that followed found the sixth. The landing closes no row (`closes_rows: []`, a behaviour-neutral R2 wave).

**Gate reds on the way.** Run 1: CITATIONS, two stale baseline entries for the deleted `CannotLeaveTheCarrier` lines
(`--write-baseline`). Run 2: `RuntimeApiGuardTests` counted the renderer's `nameof(CobolIntrinsics.X)` operands as bare
runtime names. The ratchet is right: names are anchored in the `RuntimeApi` façade, so they moved there. Run 3:
`DocCommentShapeDriftTests` (a doc summary above the first anchor class), the six inventory rows, `NameTableOwnershipDriftTests`
(five new function-name comparisons classified KEYWORD like their siblings, three stale entries deleted, the renamed CONCAT site
re-keyed) and `OperandStringChannelDriftTests` (its arm labels are function names now).

**Docs.** `docs/INTRINSICS_DESIGN.md` (the catalog sketch, the ORD-MAX paragraph) and `DESIGN-architecture-review.md` §8.3 (5),
which says the `intrinsics.json` row carries no member name.

**Group.** PB2622, PB2724 and PB3231 were dispatched in the same group. Each is a separate mechanism and is left for a
successor.

### PB2724: the GLOBAL and EXTERNAL clauses are each one grammar rule, and the EXTERNAL AS phrase and GR5 default are each read once

**Why.** ISO §2023:13.18.22 (EXTERNAL) and §2023:13.18.27 (GLOBAL) are each one clause with one general format, which the data
description, file description, report description, screen description and constant entries print as the same bracketed
slot. The grammar spelled GLOBAL four times (`globalClause`, the GLOBAL arm of the fused FD rule `fileGlobalExternalClause`,
`reportGlobalClause`, and `IS? GLOBAL` inline in `constantEntryBody`) and EXTERNAL twice. Every reader paid for it:
`OoDefinitionRules.GlobalClauseSites` had one row per spelling with a per-type predicate, `ClauseCardinalities` had a
`KindOf` column that existed only to split the fused FD rule back into its two printed brackets, and the FD and the data
description entry each carried their own copy of the §2023:13.18.22.3 SR3 screen call and of the §2023:13.18.22.4 GR5
default. kb/Work PB511 already paid once for the fused rule: it hid the AS asymmetry between EXTERNAL and GLOBAL.

**The re-probe** (at 3b5bca46e4, main with the PB2627, PB5709 and PB3227 branches replayed): the four GLOBAL spellings and two
EXTERNAL spellings were all present. The two `ExternalizedName.Screen` calls with the same descriptor, tag, rule and
`rejectZeroLength` were at `FileControlBinder.FileSection.cs` (FD) and `DataBinder.cs` (BindEntry).

**The fix.** `fileDescriptionClause` references `externalClause` and `globalClause`, and `reportDescriptionClause` and
`constantEntryBody` reference `globalClause`. `fileGlobalExternalClause`, `reportGlobalClause`,
`ClauseCardinalities.KindOfFileDescriptionClause` and `ClauseList.KindOf` are deleted, and a clause list's element is a
`Type`. `GlobalClauseSites` keys on the host rule of the one `globalClause`. The screen description entry, which already
referenced `globalClause`, gets its own SR1 c) row; before, the data-description row matched it under the wrong entry name.
`ExternalizedName.AsLiteralOf` is the one SR3 reading of the EXTERNAL AS phrase and `ExternalClauseName.Of` (model layer) the
one GR5 default, used by the FD binder and by `DataItem.ExternalizedRecordName`. `VersionConformancePass.VisitGlobalClause`
(F.2 item 61) now checks that the clause's host is a data description clause, because the constant entry's GLOBAL reaches it too.

**Drift test.** `OoDefinitionRulesDriftTests.TheGlobalClauseSites_AreTheGrammarsGlobalClauseHosts` checks two things. First,
`globalClause` is the only rule that matches the GLOBAL token, apart from the USE statement's phrase. Second, the sites table
lists exactly the rules whose ATN calls `globalClause`. A second spelling, or a new host without a row, turns it red.

**Oracle.** `compare_oracle.py --base 3b5bca46e4`: 12 of 23581 cases differ, all in diagnostic text or column; no outcome
and no code changed. COBOL2604's list of admitted clauses now names the two printed brackets (`IS EXTERNAL clause, IS GLOBAL
clause`). COBOL1520 on a constant entry's GLOBAL is reported at the GLOBAL clause, as every other host already was.

**Rows.** The witnesses were re-sited from the deleted rules to their host rules: FMT-13.18.22.2, FMT-13.18.27.2,
1985-FMT-X.4.2.2, 1985-FMT-X.4.4.2 and 1985-FMT-X.4.5.2, with their prose restated. SR-13.18.22.3-3 and GR-13.18.22.4-5 gain
the two `ExternalizedName` members. Five more rows' prose was restated (GR-13.14.4-1, GR-13.18.22.4-5, GR-13.4.5.4-3,
SR-13.18.22.3-1, SR-13.18.22.3-3). GAP did not move: 2023 1 → 1, 1985 0 → 0. `closes_rows: []`.

### PB2627 and PB2724 carried onto main; PB2622 and PB3231 re-measured and left for their own dispatches

**What this branch carries.** Wave 20262161 group A finished PB2627 and PB2724 on `worktree-wf_b65a448b-ca0-1` and
returned SPLIT, so the work was not landed. Wave 20262164 group A cherry-picked those unlanded commits onto main. It
also merged `worktree-wf_c37a519f-ba4-15` (PB5707, in the merge queue) and the current main. That main includes PB3227
(pull request 2064) and PB5725, so the PB3227 commits that were replayed add nothing to the net diff. The citation
baseline `scripts/spec/data/cite-resolve-baseline.json` was regenerated for the merged tree (1935 entries, then
`audit_code_citations.py`: 0 findings).

**One stale comment fixed.** In `CobolData.g4`, the comment above `propertyClause` said "EXTERNAL clause". It now names
the PROPERTY clause, §2023:13.18.42.

**Not done.** PB2622 and PB3231 were bundled into this group again, but each is a mechanism of its own. A 2026-11-24
re-measure found PB2622 at 1467 descriptor-plus-message emit sites over 678 descriptors in 149 files, where the note
counted 862. PB3231's forgers moved to `ObjectPropertyResolver.cs`, and the change to its input reaches 278 uses of
`DataReferenceContext`. Each note has a 2026-11-24 addendum with the measured surface and the re-sited code sites.

**The 1985 code reads the move owed (finisher).** The landing refused the branch (`code_read_landing.py`, kb/Work PB6056,
exit 6): seven stored 1985 reads named the deleted `fileGlobalExternalClause` and `reportGlobalClause` (1985-GR-X.4.2.4-2,
1985-GR-X.4.4.4-2, 1985-FMT-X.4.6.2) and `CobolIntrinsics.OrdMax` / `OrdMin` (1989-FMT-A.2.32.2, 1989-RV-A.2.32.4-3,
1989-RV-A.2.33.4-1, 1989-RV-A.2.33.4-3). Re-read at pin 9a7e82ffe9 as slice `1985-c10-pb2724` against the 1985 text, each
row re-sited at the members that now carry it (`externalClause`, `globalClause`, `ExternalizedName.AsLiteralOf`,
`ExternalClauseName.Of`; `OrdMaxAt`, `OrdMinAt` and the other `ExtremeIndex` carriers), with new probes at `--std 85` and a
separate adversarial pass (relative, indexed and LINAGE FDs, SAME RECORD AREA over a GLOBAL FD, an RD with CODE, a local
RD, a reversed PROGRAM COLLATING SEQUENCE, numeric-edited operands, an ODO table under ALL); `code_reads.py import
--replace`, 7 CONFORMS, 7 UPHELD.

### At --std 2002 and 2014 the CALL, INVOKE, SET, MOVE and repository diagnostics cite their edition's rule, and a net asks it

Wave 20262165 group A (kb/Work PB5922, PB6118, PB6149, PB5670, PB5659, PB5660, PB5676, PB5671, PB5654), continuing the branches of
PB5706 (worktree-wf_c37a519f-ba4-18) and PB2622 (worktree-wf_38508f28-551-1), merged here. Their two new dialect behaviours both took
number 71 and VCR row 7.127; main settled them (PB5706 `DialectBehavior` 71 and VCR row 7.127, PB5707 72 and 7.128), and this branch
takes main's numbering.

**The net.** `EditionCitationNet` asked only `--std 85` (PB4950). It now asks 2002 and 2014 too: a diagnostic citing a later
edition's rule and none of its own fails, unless it is an edition gate. Measured on the merged base the 2002 and 2014 directions were
3395 and 3146 offending lines under 525 and 536 codes, about four times PB4950's 1985 campaign, so the net is laid as a ratchet of
codes (`edition-citation-sweep.json`) that the PB6118 sweep empties; `WISEOWL_COBOL_RECORD_CITATION_SWEEP` measures it.

**The shapes.** `CallSyntaxRules` holds the CALL rules by their 2023 number (2014 numbers them alike, 2002 by its own table) and the
conformance clauses (14.7.1/14.7.2 at 2002, 14.8.1/14.8.2 at 2014). `StorageSectionRule.For` carries each edition's rule and its section
list: 2002's still names the communication section. `ObjectReferenceSet.Refusal` takes the SET rules clause (`OoConformance.SetRules`;
SR8 to SR14 are numbered alike at 2002, 2014 and 2023), the run time passing 2023's. `SymbolTable.ClassNames.ParameterizedGround` is the
one statement of the parameterized-name ground, now also INVOKE's (COBOL0823 said "neither a resolvable data item nor a class").
The procedure-header rules, the report-group rules, CURRENCY SR21, USE AFTER EXCEPTION OBJECT and the scope rule choose by edition.

**Re-probes and adjudications.** PB5659's refusal is right at 2002 (§2002:8.4.2.9.2 SR6 gives the property the ACTIVE-CLASS
description, §2002:14.8.35.2 SR14 refuses the sender); the message now cites 2002 and names `SELFREF OF A`, not `__PROP-TEMP-200002`.
PB5660's COBOL0809 at 2002 is right (MOVE of class object, §2002:14.8.24.2 SR1), and at 2014 the receiver's 0809 is not a cascade; the
sender is now named as the compiler temporary it is. PB5671 and PB5654 were comment and help-text fixes.

**Goldens.** `wiseowl/cases/14.9.4-CALL/pb6149-call-by-reference-pointer-2002`, `14.9.39-SET/pb5659-set-property-active-class-2002`,
`14.9.25-MOVE/pb5660-inline-invocation-move-object-2002`, `12.3.8-REPOSITORY/pb5676-invoke-parameterized-class` (2002, 2014, 2023).
Thirteen refusal expectations that quoted a 2023 clause at every edition keep its rule designator. 32 codes left the ratchet.

**Review round 1 (R1-1).** Eleven `Cite` calls of this branch passed the COBOL-2002 rule only as `y1985`, which `Cite` reads at 85
alone, so `--std 2002` printed the 2023 rule: `PROCEDURE DIVISION USING BY VALUE L` (`L PIC X(4)`) said "numeric, message-tag,
object, or pointer (ISO §2023:14.2.2 SR2)", and `MOVE P TO Q` of two pointers cited §2023:13.18.60.3 SR9 and SR8. Each now passes its
2002 text as `y2002` (the MoveTable16 pointer arm, USE AFTER EXCEPTION OBJECT, CallBinder's formal-count and RETURNING rules,
OoBinder's INVOKE fallthrough, UdfBinder's activation rule, the BY VALUE class words and rule, CURRENCY SR21's 2002 rule, the
REPOSITORY scope rule). The build now refuses the shape: `EditionCiteSlotAnalyzer` (WOCA0004) reports a `Cite` or `CiteRule` call whose
2002 or 2014 citation is not in the argument the call chooses at that edition; a planted violation failed `dotnet build` with WOCA0004,
and `EditionCiteSlotAnalyzerTests` plants each shape. The 2002 net had stayed silent because COBOL1553 is in the sweep and the 0809
message also names a §2002 rule. Goldens `pb5922-by-value-formal-class-2002` and `pb5922-move-pointer-2002` pin both messages.

**Left open.** PB5922 (COBOL1560/1707 licence text, COBOL1627 intrinsic clauses, the run-time reason texts) and PB6118 (the ratchet).

### SetBinder becomes the SET format dispatch over seven family binders; four census clone pairs become one body each; the RuntimeApi ratchet reaches zero

Wave 20262168 group A (kb/Work cluster PB2119: PB2155, PB2314, PB2157, PB2158, PB2162, PB2302, PB2156 landed or retired; PB2399,
PB2416, PB2305, PB2418 left open for a continuation), carried on top of closed-hierarchies-wave1 (PB6168) and
worktree-wf_dc6f75bb-a8b-25 (PB5922), merged at 325c5e2f38. Every change is behavior-neutral: the §4 oracle
(`scripts/arch/compare_oracle.py --base 325c5e2f38`) answered IDENTICAL over 23,639 cases after each mechanism.

**The unifies (census R0-0053 to R0-0056).** Each pair was triaged by what its bodies bind, not by their shape.
- PB2156: LEAP-SECOND, PROPAGATE and REF-MOD-ZERO-LENGTH each copied one `{ ON | OFF }` collection loop; it is now
  `OnOffDirectiveStage.Collect`, and each stage keeps its keyword, registry row and event type. TURN and FLAG stay apart: they
  report a malformed operand themselves.
- PB2157: `SymbolTable.TryResolve` and `TryResolveCondition` share one method-overlay precedence (§2023:11.7.4 GR5),
  `TryNearestTable<T>`; the table selector is a static lambda, so the hot lookup allocates no delegate.
- PB2158: INITIATE and TERMINATE bind through `ReportWriterBinder.BindReportNameSeries` with a `ReportNameVerb` per verb.
  GENERATE stays apart (a report-name or a detail group, one statement, not a series).
- PB2155: SET Formats 8 and 9 bind through `BindSetPrototypedPointer` over a `PrototypedPointerFormat` per carrier. PB817 had
  made the compare one rule; now the statement is one too. Every message is unchanged, character for character.
- PB2162 retired: `RuntimeApi.CodomainConst` was deleted by ef68c6d1ac (PB3829), so R0-0060 has no pair left.

**PB2314, the SetBinder extract.** SetBinder had 1,778 lines on this tree. It now holds only the dispatch (249 lines). The
formats moved, member for member, to `SetContentBinder`, `SetLocaleBinder`, `SetPointerBinder`, `SetAddressBinder`,
`SetIndexBinder`, `SetCapacityBinder` and `SetConditionBinder`. Format 1's tail became `SetIndexBinder.BindIndexAssignment`,
and the receiver helpers became `SetFormatSelection.Receivers`. `SetBinderFamilyDriftTests` pins the boundary: SetBinder
declares only the dispatch, and no member has two homes. The SET drift tests now read every `Set*Binder.cs`. Three
`record_verdicts` batches moved the code-locations with the members (58, 13 and 7 rows in the 2023, 2002 and 1985 stores; GAP
unchanged). Every comment, doc and grammar comment that named `SetBinder.<member>` now names the member's new owner.

**PB2302 step 1.** The guard's own rule counted 30 bare runtime names (the ratchet table allowed 36). All of them now go through
a new section of `RuntimeApi`: emitted-text composers, plus three compile-time rule calls and `DecSidiDigits`.
`RuntimeApiGuardTests` lost its table and now forbids every bare access.

**Self-review** (architecture, code, performance, duplication): one mechanism per job. No forwarder is left behind; the format
entries `BindSetProgramPointer`/`BindSetFunctionPointer`, `BindInitiate`/`BindTerminate` and the directive stages' `Process` each
name their per-format data, which the inventory and the statement binder read. No hot path gained an allocation. The shape
siblings were checked and recorded as apart (TURN/FLAG, GENERATE, Format 7, `InspectReplaceKindText`).

**Gate:** see the landing's record.

### C# 15 `closed` hierarchies, wave 1: the bound tree and the binding model's Place and AccessPath families are closed

**Owner, 2026-11-24:** "I approve closed". The C# 15 evaluation and the check the owner asked for are in kb/Work PB6168.

**What changed.**
- `closed` replaces `abstract` on 20 records in `Binding/Bound/` and `Binding/Model/Place.cs` and `AccessPath.cs`: the
  eight bound roots, `ReceiverBracketKey`, `InitializeAction`, `TableSortStorage`, `ValidateStep`, `Place`,
  `PlaceDecorator`, `WindowCoding`, `CellWindowCoding`, `AllCount`, `AccessSegment`, `RootSegment` and `ContainerAnchor`.
  A switch over these hierarchies that names every case now needs no default arm, and a missing case is a compile error
  (CS8509 under `TreatWarningsAsErrors`).
- The repository tools that parse C# declarations learned the keyword: `scripts/spec/code_location.py`,
  `scripts/spec/drift_rules.py` (three patterns), `scripts/arch/source_units.py`, `scripts/prune_worktrees.py` (two
  patterns) and `scripts/gen-vault-reference.ps1`. Without it, the spec-traceability resolver lost every `closed` type
  (`EveryCSharpFilesSymbols_AgreeWithThePythonResolver` failed in the pre-landing check).
- DESIGN-architecture-review §8.5 and §5.5 name `closed` as the mechanism. The union-type plan is withdrawn, and unions
  stay a candidate only for a sum with no common base type.

**Behavior-neutral, measured before the change** (main 9e65164eb, throwaway worktree):
- The generator output was byte-identical, 95 of 95 files.
- The build had 0 warnings.
- `WiseOwl.Cobol.Tests.Unit` passed 43,099 of 43,099.
- `IsAbstract` holds in Roslyn 5.12 and in metadata.

**Filed:**
- PB6169, wave 2: remove the now-provable `throw` arms, then close the remaining hierarchies with judgment for the
  runtime ABI.
- PB6170: the C# modifier vocabulary is written seven times across five tools.

