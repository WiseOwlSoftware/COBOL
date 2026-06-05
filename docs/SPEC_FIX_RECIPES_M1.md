# Spec-Fix Recipes — WS-SPEC round 2 (M1 finish + M2 seed)

Generated 2026-06-05 from a 10-agent read-only diagnosis workflow (each item root-caused vs ISO 2023
spec, version-classified M1='85 vs M2=2002+, with file list + fix sketch + test shape). This is the
durable digest; prior sessions lost the ephemeral recipes, so capture here. Implementation discipline:
ONE fix at a time, directly on `main`, full `bash scripts/guard.sh` after each, commit + DEVLOG.

## Classification & order

**M1 (implement now):** #1 COMP-4 (S) · #3 var-len MERGE (S) · #5 GLOBAL FD I-O (S) · #7 SUM(T(ALL)) ODO (M)
· #2 var-len SORT (M) · #6 Report Writer control breaks (L, staged).
**M2 (defer, 2002+):** #4 SORT Format-2 self-key · #8 multi-char CURRENCY · #9 CALL RETURNING/BY VALUE
· #10 READ PREVIOUS. (All dialect-gated when implemented; M1 deliverable for #8 is a clean reject.)

Implementation order chosen: **1 → 3 → 5 → 7 → 2 → 6** (clean wins first, large RW last as its own sub-loop).

---

## #1 — COMP-4 / COMPUTATIONAL-4 ≡ BINARY; reject unknown COMP-n  (M1, small)
**Root cause:** no COMP_4 lexer token → `COMP-4`/`COMP-9` lex as IDENTIFIER → swallowed by `genericDataClause`
(CobolExpressions.g4:15-17, last alt of dataDescriptionClause) → `usageClause()` null → silently treated as
DISPLAY, no diagnostic. (`_ => UsageKind.Object` at PicUsageResolver.cs:148 is unreachable for these.)
**Spec:** §13.18.60.2 format (BINARY/COMP/COMPUTATIONAL/DISPLAY/INDEX/PACKED-DECIMAL — no COMP-4); §13.18.60.3
SR6 "COMP is an abbreviation for COMPUTATIONAL"; GR4/GR6 BINARY≡COMPUTATIONAL. COMP-4 is the universal
vendor synonym → map to UsageKind.Binary. Unknown COMP-n violates the format → must diagnose.
**Files:** CobolLexer.g4 (add COMP_4/COMPUTATIONAL_4 tokens by the COMP family) · CobolData.g4 (add to
usageClause + usageKeyword) · PicUsageResolver.cs UsageMapper (add COMP-4/COMPUTATIONAL-4 => Binary) ·
SemanticBuilder.VisitDataDescriptionEntry (~1196, detect unknown COMP-n → CBL0816) · DiagnosticDescriptors.cs
(CBL0816 Error) · SpecFixTests.cs (2 facts). No runtime work — Binary storage/codec already complete.
**Commit split:** (1a) feature COMP-4≡Binary + positive test [zero risk]; (1b) CBL0816 reject unknown COMP-n
+ negative test [touches genericDataClause catch-all for bare form — adversarial-sweep the corpus first].
**Risk:** CBL0816 is the surface — any program using an untokenized COMP-n vendor ext would now fail. Guard
all 360 NIST + watch NC (heavy COMP/COMP-3).

## #3 — Variable-length record MERGE GIVING writes max length not source length  (M1, small, RUNTIME-ONLY)
**Root cause:** `SortRuntime.MergeRecordsInternal` (SortRuntime.cs ~116-128) reads each input record into a
max-size buffer and `sf.Records.Add(buf)` at full SD length, never calling `FileRuntime.GetLastRecordLength`.
GIVING write side is already correct (IrSortGivingWriteVariable via GetLastReturnedLength) but is fed max.
**Spec:** §14.9.24.4 GR7b (record written keeps the size it had when read), GR12b.
**Fix:** after each successful ReadRecord, `actualLen = FileRuntime.GetLastRecordLength(inputName)`, clamp to
[1, sf.RecordLength], copy to a new byte[actualLen], add that. One method. SORT path already correct
(EmitSortUsingFile→IrSortReleaseVariable). **Risk:** LOW — fixed-len MERGE unchanged (clamp covers full len).

## #5 — Cross-program GLOBAL FD inheritance for indexed/relative I-O  (M1, small)
**Root cause (2 layers):** (L1, primary) `FileStateValidator.CheckFileOpen` (FileStateValidator.cs:143-153)
reports CBL0702 when a READ/WRITE/etc targets a file with no local OPEN; a contained program relying on the
container's OPEN of a `FD … IS GLOBAL` file has empty local openedFiles → CBL0702. (L2, relative-only)
`Compilation.InheritGlobalItems` (Compilation.cs:196-208) inherits the GLOBAL FileSymbol + record fields
(so INDEXED prime key resolves) but NOT the separate WORKING-STORAGE RELATIVE KEY item → contained relative
keyed I-O resolves null → silent sequential read. Runtime connector layer already correct (name-keyed shared
_manager, Init only in Main, Entry re-registers).
**Spec:** §9.1.5(2) sharing file connectors via global file-name; §8.4.6.2.4 record-key global when FD GLOBAL.
**Fix:** (1) FileStateValidator.CheckFileOpen take FileSymbol; `if (file.IsGlobal) return;` (CBL0702 is a
Warning, unsound for globals). (2) InheritGlobalItems: also inherit `file.RelativeKey` DataSymbol+StorageLocation
with OwnerProgramId (mirror the record-field loop); add to CollectInheritedGlobalNames so CBL3128 won't flag it.
**Risk:** LOW-MED — watch IC233A/234A/227A/228A, RL/IX/SQ; grep tests for CBL0702 asserts.

## #7 — FUNCTION SUM(T(ALL)) over OCCURS DEPENDING ON ranges over MAX not current  (M1, medium)
✅ **DONE (DEVLOG 346) — SUM-scoped.** ExpandAllSubscript now masks each occurrence beyond the level minimum
by `MAX(0, MIN(1, N-(idx-1)))` (built from existing FUNCTION MAX/MIN bound nodes — no new IR). **FOLLOW-UP
(M2/quality):** the count-sensitive aggregates (MEAN/MEDIAN/MIDRANGE/RANGE/VARIANCE/STANDARD-DEVIATION, and
MAX/MIN over ODO-ALL) still expand to the maximum — a 0-mask corrupts them. The complete fix is a *runtime-range*
intrinsic argument (pass the table base + runtime count; the runtime iterates exactly the active count),
which requires restructuring the fixed `object[]` arg materialization in `CilExpressionEmitter.EmitIrIntrinsicCall`
+ a new `IrTableRangeArg` + the bind-time non-expansion marker. Deferred — niche, and a real chunk of work.
**Root cause:** `ExpressionBinder.ExpandAllSubscript` (ExpressionBinder.cs:326-363) computes the ALL count from
`Occurs.MaxOccurs` only (line 345), never reads `Occurs.DependingOnSymbol`; statically emits T(1)..T(MAX) →
SUM adds inactive tail slots.
**Spec:** §15.3 (ALL over ODO ranges over the DEPENDING object); §8.4.2.3.4 GR1a; §15.88 SUM.
**Fix (SUM-scoped masking):** collect per-level (Max,Min,Dep). For a level with a DependingOnSymbol: const Dep
→ clamp count; runtime Dep → for idx>Min wrap the element in a BoundConditionalExpression(idx<=depValue ?
element : 0). Add IrConditionalExpr + ExpressionLowerer case + CilEmitter branch emit. **DANGER:** 0-mask
corrupts count-sensitive aggregates (MEAN/MEDIAN/VARIANCE) — limit mask to SUM/reductions, leave TODO for the
general runtime-count design (IrTableRangeArg with runtime count). **Risk:** watch all ALL/aggregate intrinsic
tests + multi-dim ALL cartesian expansion; fixed-capacity tables must keep count=MaxOccurs.

## #2 — Variable-length record SORT RELEASE/RETURN loses per-record length  (M1, medium)
**Root cause:** var-len machinery wired only into implicit USING/GIVING, never explicit RELEASE/RETURN.
`FileIoLowerer.LowerRelease` (~1263-1282) unconditionally emits `IrSortRelease` at the SD MAX size; `LowerReturn`
(~867-939) never writes the returned length back into the DEPENDING item nor sizes the INTO move. Runtime is
already correct+unused here (ReleaseRecord stores exact length; ReturnRecord sets LastReturnedLength).
**Spec:** §13.18.43 GR13a/GR15/GR16; §14.9.34.4 GR5b; §14.9.32 GR4.
**Fix:** LowerRelease: if `IsVaryingRecord(SortFile)` && `ResolveRecordLengthLocation` → emit new
`IrSortReleaseFromDepending(name, recordLoc, depLoc)`. LowerReturn: after IrSortReturn, if varying, emit new
`IrSortReturnStoreLength(name, depLoc)` (mirror LowerRead 396-399). New IR nodes + CilFileIoEmitter methods
(mirror EmitWriteRecordVariable / EmitStoreRecordLength) + CilEmitter dispatch cases. Gated on IsVaryingRecord
so fixed-len SD unchanged. **Risk:** watch ST146A (RETURN INTO ODO), ST137A/147A (Format-2), SortMerge* tests.

## #6 — Report Writer CONTROL/SUM/GROUP INDICATE/RH-RF + body-group fields  (M1, LARGE — staged)
**Root cause (pipeline):** (1) SemanticBuilder.VisitReportGroupEntry never reads reportGroupIndicateClause;
ReportGroupSymbol has no GroupIndicate. (2) FileIoBinder.BuildReportLines:236 `if (g.HasColumn && g.SourceName
!= null)` drops body VALUE-literal / SUM-counter / LINE-COUNTER fields; BindReportSource uses bare ResolveData
so SOURCE LINE-COUNTER in DETAIL → null. No SUM/control-break modeling. (3) LowerInitiate registers only
Page heading/footing — never RH/RF/CH/CF, SUM counters, CONTROL hierarchy. (4) ClassifyPageField blank-fills
arbitrary WS SOURCE in PH/PF. (5) LowerTerminate never drives final CF/RF. (6) ReportWriterRuntime.EmitGroup
has no prior-control storage / break detection / CH-CF / SUM / GROUP-INDICATE.
**Spec:** §14.9.16.4 GR4-6 (GENERATE order); §13.18.16.4 (CONTROL save/compare/break); §13.18.54.4 (SUM
counters); §13.18.28.4 (GROUP INDICATE first-occurrence); §13.18.57 Fmt-2 GR6 (RH/PH/CH/CF/RF order).
**Stages (each build+guard-green before next):** A semantic + the BuildReportLines body-field bug (smallest,
unblocks all: GroupIndicate prop, BoundReportField carries an expression Source, emit VALUE/special-reg/SUM
fields, fix ClassifyPageField WS SOURCE). B RH/RF presentation. C CONTROL break detection + CH/CF. D SUM
accumulation. E GROUP INDICATE. Tests in ReportWriterSpecTests.cs. **Risk:** RW101A-104A use only DETAIL/PH/PF
(no control/sum) so baselines safe IF EmitGroup page mechanics unchanged; watch the 5 ReportWriterSpecTests
facts (BoundReportField signature change) + RW301M/302M flagging.

---

## M2 seed (deferred — 2002+, dialect-gate when built)
- **#4 SORT Format-2 self-key** (2002): FileIoLowerer.BuildKeySpecField uses whole-table StorageLocation.Length
  for an elementary self-key (should be ElementSize) → RuntimeGuard throws. Also GR23 omitted-KEY fallback
  (FileIoBinder.BindTableSort:585-604) missing. Low-cost; de-risks M2.
- **#8 multi-char CURRENCY** (2002): CurrencyOutputChar is `char` end-to-end (truncates "EUR "→'E'). Widen to
  string through PicEnvironment/SemanticBuilder/PicDescriptorFactory/PicRuntime/CilExpr+DataEmitter. M1 NOW =
  emit a dialect error for literal-7 length>1 under '85.
- **#9 CALL RETURNING/BY VALUE** (2002): (a) delete spurious caller-side CBL3304 in BoundTreeValidator.ValidateCall
  (385-388 — caller RETURNING target unrestricted §14.9.4.3 r7); (b) Binder.LowerCall must materialize computed
  BY VALUE/BY CONTENT args into a scratch WS temp (IrComputeStore) instead of dropping null-loc args.
  **⚠ FINDING (DEVLOG ~353, attempted+reverted): deeper than the recipe.** Deleting CBL3304 lets it compile but
  CALL RETURNING then CRASHES at runtime (RT0001 buffer access out of range, bufferLength=0) — the callee's
  PROCEDURE DIVISION RETURNING item is not wired to the passed pointer. Investigated: the caller DOES pass the
  RETURNING target as a trailing BY-REF arg (CilEmitter.EmitCallProgram ~1280); SemanticBuilder DOES capture the
  name (`_procedureReturningName`, VisitProcedureDivision) → Compilation `SetProcedureReturningItem`. Tried mapping
  it like a USING param (append `_semantic.ProcedureReturningItem.Name` to `module.UsingParameterNames` in
  Binder.PopulateModuleMetadata, so the entry-body maps args[count]→its _linkage field) — STILL crashes
  bufferLength=0.
  **✅ RESOLVED (DEVLOG 365).** Root cause: `CilLocationEmitter.FindLinkageField` iterates ONLY
  `SemanticModel.ProcedureUsingParameters`, so the RETURNING item (not a USING param) never matched a LINKAGE
  range → null buffer. Entry 354's `UsingParameterNames` append fixed field-creation + entry-mapping but NOT the
  location path — that asymmetry was the bug. Fix (RETURNING-only, surgical): (a) Binder appends
  `ProcedureReturningItem.Name` to `module.UsingParameterNames`; (b) `FindLinkageField` also tests the RETURNING
  item's LINKAGE range → its `_linkage_<R>` field; (c) CBL3304 removed (caller target unrestricted); (d)
  dialect-aware `CompileMultipleAndRun(DialectMode,…)` overload added. NOT added to `ProcedureUsingParameters`
  (keeps CBL3108/arity correct). Verified end-to-end: `CALL "ADDER" USING WS-A WS-B RETURNING WS-R` → `0042`.
  **This unblocks WS-2002-UDF** (a user function returns via the same mechanism).
- **#10 READ PREVIOUS** (2002): IndexedFileHandler.ReadPrevious (312-359) ignores _readNextInclusive — post-OPEN
  must AtEnd, post-START KEY=EQUAL must return key<=k inclusive. Relative analog too. Runtime-only boundary fix
  (READ … PREVIOUS already parses + flows ReadDirection.Previous → handler.ReadPrevious); cobol85-rejection is a
  separate flagging item.
- **#10 READ PREVIOUS** (2002): IndexedFileHandler.ReadPrevious (312-359) ignores _readNextInclusive — post-OPEN
  must AtEnd, post-START KEY=EQUAL must return key<=k inclusive. Relative analog too. Dialect-gate to >=2002.

Full per-item JSON (root cause / files / fix / test / risk) was produced by workflow wsnarsiy5.
