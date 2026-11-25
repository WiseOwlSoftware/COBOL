## Entry 4055 — 2026-11-24 15:07 PST — PB2627: an intrinsic catalog row names no runtime member; the renderer keys on the function and names each body through RuntimeApi; PB2724: the GLOBAL and EXTERNAL clauses are each one grammar rule, and the EXTERNAL AS phrase and GR5 default are each read once; PB2627 and PB2724 carried onto main; PB2622 and PB3231 re-measured and left for their own dispatches

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
