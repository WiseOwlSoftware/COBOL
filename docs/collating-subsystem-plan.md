# Collating Sequence Subsystem — State & Implementation Plan

_Authored 2026-05-29 (audit session). Supersedes the "bypassed everywhere" framing in
memory `project_collating_gap` and `session-state-2026-05-29.md` §0c._

## Executive summary

The custom **program collating sequence is NOT "bypassed everywhere."** The
`PROGRAM COLLATING SEQUENCE` → relation-condition comparison path is **fully implemented and
tested** (end to end: grammar → semantic builder → semantic model → condition lowerer → IR →
CIL emitter → runtime), with a passing integration test.

Two genuine gaps remain:
1. **SORT / MERGE key collating** — the phrase is parsed but not bound; the runtime sorts
   alphanumeric keys with raw byte order; and the program collating sequence is not applied to
   sort keys as a default.
2. **FUNCTION CHAR / ORD** — use native ASCII ordinals, ignoring the program collating sequence.

**Not collating consumers** (per ISO/IEC 1989:2023 §8.9 NOTE 2, spec line 14467): class
conditions, the CLASS clause, SYMBOLIC CHARACTERS, CODE-SET, and INSPECT all reference a
*coded character set* / membership — **not** a collating sequence. No work needed there. (The
old memo incorrectly listed class-conditions/INSPECT as collating consumers.)

---

## What is already wired (verified this session)

| Layer | Location | Status |
|------|----------|--------|
| Grammar — PCS clause | `CobolParserCore.g4:261-262` (`PROGRAM COLLATING? SEQUENCE IS? cobolWord`) | DONE |
| Grammar — ALPHABET | `Core/CobolSpecialNames.g4:84-99` (NATIVE/STANDARD-1/STANDARD-2 + literal/THRU/ALSO) | DONE |
| Grammar — SORT collating | `Core/CobolIO.g4:334-335` (`COLLATING SEQUENCE IS? cobolWord (cobolWord)?`) | parsed only |
| Build alphabet table | `SemanticBuilder.cs:499-618` `BuildAlphabetCollatingSequence` -> 256-byte code->weight; THRU ranges + ALSO equal-weight; unspecified -> weight 255 | DONE |
| Capture PCS name | `SemanticBuilder.cs:415-424` `VisitObjectComputerParagraph` | DONE |
| Resolve PCS table | `Compilation.cs:233-238` -> `model.SetProgramCollatingSequence(byte[])` | DONE |
| Model field | `SemanticModel.cs:116-119` `ProgramCollatingSequence` (`byte[]?`, null = native) | DONE |
| Lower comparisons | `ConditionLowerer.cs:326-347` — PCS active & operand non-numeric -> `IrStringCompareWithSequence` / `...LiteralWithSequence` | DONE |
| Lower figuratives | `ConditionLowerer.cs:445-520` — LOW/HIGH-VALUE remapped to min/max-weight char under PCS | DONE |
| IR nodes | `IrInstruction.cs:1472-1501` carry `byte[] CollatingSequence` | DONE |
| Emit dispatch | `CilEmitter.cs:888-889` | DONE |
| Emit compare | `CilComparisonEmitter.cs:303-352` bakes the byte[] literal, calls runtime | DONE |
| Runtime compare | `PicRuntime.cs:2310-2326` `CompareAlphanumericWithSequence` (code->weight) | DONE |
| Test | `ConditionTests.cs:453-493` `CollatingSequence_ReverseOrder` (B<A) | passing |

Design principle in use: **the 256-byte table is resolved at compile time and baked into the IR**
(then into the CIL as a byte[] literal). AOT/WASM-safe; no runtime global. Any new consumer should
follow the same pattern.

---

## Gap 1 — SORT / MERGE key collating

**Spec basis (ISO/IEC 1989:2023):**
- §14 application (spec line 14069): the alphanumeric / national **program collating sequences are
  applied to alphanumeric / national sort or merge keys** unless modified by a SET statement or a
  `COLLATING SEQUENCE` phrase in the respective SORT/MERGE statement.
- SORT precedence (spec line 31907): (a) the statement's `COLLATING SEQUENCE` phrase if present
  (alphabet-name-1 -> alphabetic/alphanumeric keys; alphabet-name-2 -> national keys); (b) otherwise
  the program collating sequences.
- MERGE precedence (spec line 28684): identical two-tier rule.
- Collating applies to keys of class **alphabetic / alphanumeric / national** only. **Numeric keys
  compare by value** (no collating).

**Current state:**
- `FileIoBinder.BindSortStatement` (59-69) and `BindMergeStatement` (135-146) **ignore**
  `sortCollatingPhrase`. `BoundSortStatement` / `BoundMergeStatement` carry no collating field.
- `SortRuntime` alphanumeric keys use **raw unsigned byte** comparison
  (`SortKeyComparer.Compare` alphanumeric branch `SortRuntime.cs:258-260`; `CompareBytes:264-274`).
  Numeric keys already decode + compare as decimals (250-256) — correct, leave as is.

**Plan (mirror the comparison subsystem — bake byte[] at compile time):**
1. **Bind** (`FileIoBinder`): resolve the collating phrase's first alphabet-name -> 256-byte table
   via `model.ResolveAlphabetDefinition(name).CollatingSequence`; if no phrase, fall back to
   `model.ProgramCollatingSequence` (may stay null = native). Add `byte[]? CollatingSequence` to
   `BoundSortStatement` and `BoundMergeStatement`. (alphabet-name-2 / national: capture if present
   but national sort keys do not occur in the NIST suite — single alphanumeric table for now; note
   the limitation.)
2. **Lower** (`FileIoLowerer`): thread the `byte[]?` into the SORT/MERGE IR instruction (new field).
   Null -> native; keep the existing raw-byte runtime path for null.
3. **Runtime** (`SortRuntime`): add an optional `byte[]? collating` to `SortRecords` / `MergeRecords`
   (and `SortTable` for table SORT). In `SortKeyComparer`, when `collating != null && !key.IsNumeric`,
   compare via a new `CompareBytesWithSequence` (weight lookup, space-pad short side) instead of
   `CompareBytes`. Numeric keys unchanged.
4. **Test:** a SORT with `PROGRAM COLLATING SEQUENCE` (reversed alphabet) and a SORT with an explicit
   `COLLATING SEQUENCE` phrase that overrides the PCS; assert output order differs from native.

**Regression surface:** a program that declares a PCS but relied on native sort order would change
(now spec-correct). Re-run the ST baselines; keep null=native fast path so non-PCS programs are
byte-identical.

---

## Gap 2 — FUNCTION CHAR / ORD

**Spec basis:** §15.15 CHAR (spec line 34707) and §15.36 ORD use the **alphanumeric program
collating sequence**; ordinal position is **1-based**. §15.15.4 rule 2: if multiple characters share
a position, CHAR returns the **first** character defined for that position.

**Current state:** `IntrinsicFunctions.Char(n)` returns code `n-1`; `Ord(c)` returns code `+1`
(`Intrinsics/IntrinsicFunctions.cs:196-203`). Native only — ignores PCS.

**Semantics under a custom table** (`seq[code] = weight`, weights are the 0-based ordinal positions):
- `ORD(c)` = `seq[code(c)] + 1`.
- `CHAR(n)` = the character `code` whose ordinal position is `n`, i.e. the **first** (lowest) code
  with `seq[code] == n-1`.

**Threading problem:** intrinsics are `static` with no PCS context. Recommended approach (mirrors the
comparison split, AOT-safe): in intrinsic **lowering**, when `model.ProgramCollatingSequence != null`,
emit CHAR/ORD variants that take the baked `byte[]` table (e.g. `CharWithSequence(decimal, byte[])`
/ `OrdWithSequence(string, byte[])`); otherwise keep the native static calls. Do **not** add a
runtime global.

**Priority:** lower. The IF suite is 100% with native CHAR/ORD; no NIST NC/IF test exercises CHAR/ORD
under a non-native PCS. Implement for spec completeness after Gap 1.

---

## Non-consumers (intentionally NOT collating) — ISO NOTE 2 (spec line 14467)

Class conditions, CLASS clause, SYMBOLIC CHARACTERS, CODE-SET, INSPECT reference a *coded character
set* or do membership tests — not collating order. `PicRuntime.IsInUserClass` and the class-condition
emitters correctly use byte membership. **No change.**

---

## Channel note (this session)

The tool **result-rendering channel** intermittently corrupted large `Read` outputs (reset line
numbers, fabricated content) and eventually a `Grep` summary. Disk writes/content stayed intact
(stable sha/md5). The findings above were corroborated while the channel was reliable (repeated,
cross-consistent Grep + clean full Reads of `SortRuntime.cs`, `SemanticModel.cs`, `Compilation.cs`,
`ConditionTests.cs`, `SemanticBuilder.cs`, and the spec). **Implementation deferred to a fresh
session** to avoid editing against an unreliable read channel. Work the plan top-down: Gap 1 ->
build -> guard -> baseline; then Gap 2.

---

## Status update (2026-05-29, end of session) — Gap 1 implemented

**Gap 1 DONE (SORT/MERGE/TABLE-SORT alphanumeric key collating).** Files changed:
- `Semantics/Bound/BoundNodes.cs` — `CollatingAlphabetName` on BoundSortStatement /
  BoundTableSortStatement / BoundMergeStatement.
- `Semantics/Bound/Binding/FileIoBinder.cs` — `ExtractCollatingName(sortCollatingPhrase)` (alphabet-name-1).
- `IR/IrInstruction.cs` — `CollatingSequence` (byte[]?) on IrSortSort / IrSortMerge / IrTableSort.
- `CodeGen/Lowering/FileIoLowerer.cs` — `ResolveCollating(name)` (phrase alphabet -> PCS -> null) threaded into the 3 IR nodes.
- `CodeGen/Emission/CilFileIoEmitter.cs` — `EmitCollatingArg` (bake byte[] or ldnull); 3 SortRuntime call sites take the extra arg.
- `Runtime/SortRuntime.cs` — `byte[]? collating` on SortRecords/MergeRecords/SortTable + internals; `SortKeyComparer` uses new `CompareBytesWithSequence` for alphanumeric keys when non-null.
- `tests/.../SortMergeCollatingTests.cs` — 3 passing tests + 1 skipped (below).

Build clean. Unit 1000 pass. 3 collating tests pass.

### Gap 1 follow-up — numeric SORT-key misclassification (NEW latent bug, test skipped)
`SortNumericKey_IgnoresCollatingSequence` is `[Fact(Skip=...)]`. A `PIC 9(1)` key under a reversed
digit alphabet sorts by collating weight, not value. Root cause: `FileIoLowerer.BuildKeysSpec`
(~line 672) gets a null/non-numeric pic from `_ctx.Semantic.GetPicDescriptor(k.Key)` for SD
elementary keys -> isNumeric=0 -> SortRuntime takes the alphanumeric (collating) path. Pre-existing;
masked before collating because raw-byte order of unsigned DISPLAY digits == numeric value order.
**Fix next session:** make BuildKeysSpec classify numeric SD keys correctly (first locate the
`GetPicDescriptor` definition — grep for it flaked this session; it resolves on `SemanticModel` or an
extension). Then un-skip the test. Spec: numeric keys compare by value, never collate (ISO 14.9.40).

### Not committed
Run `bash scripts/guard.sh` (expect ALL GREEN — numeric test skipped; null=native keeps NIST byte-
identical) and verify, THEN commit. Then resume with Gap 2 (FUNCTION CHAR/ORD under PCS).

## HANDOFF (context-limit, 2026-05-29)
Gap 1 code complete; guard was ALL GREEN (exit 0) before commit.
NEXT SESSION VERIFY FIRST: `git log --oneline -3` — confirm the 'SORT/MERGE collating sequence' commit landed.
If working tree dirty / commit absent, re-stage the 9 files (BoundNodes, FileIoBinder, IrInstruction, FileIoLowerer, CilFileIoEmitter, SortRuntime, SortMergeCollatingTests, DEVLOG.md, this doc) and commit with the message in DEVLOG 224.
Then: (a) fix numeric SORT-key misclassification (BuildKeysSpec/GetPicDescriptor) + unskip SortNumericKey_IgnoresCollatingSequence; (b) Gap 2 FUNCTION CHAR/ORD under PCS.

---

## FINAL STATUS (2026-05-29) — supersedes all provisional notes above

Ignore the earlier "deferred to a fresh session", "Not committed", "test skipped", and "Fix next
session" notes — they were written mid-session and are obsolete. Actual end state:

- **Gap 1 (SORT/MERGE/TABLE-SORT collating) — DONE and COMMITTED** (0a7caae). The "parsed only"
  status for the SORT collating grammar row above is now "DONE".
- **Numeric-key misclassification — FOUND and FIXED, COMMITTED** (8900437). `BuildKeySpecField`
  derives the key PIC via `_ctx.Location.ResolveLocation(key.Key)?.GetPic()` (the live path); the
  dead `SemanticModel` pic registry (`RegisterPicDescriptor`/`GetPicDescriptor`/`_picDescriptors`,
  zero callers) is no longer used and is flagged for a separate zero-dead-code deletion.
- All 4 SortMergeCollatingTests PASS (numeric test un-skipped). Guard ALL GREEN: 1000 unit /
  340 (+1 unrelated skip) integration / 149 NIST baselines 0 FAIL*.
- **Remaining: Gap 2 only** — FUNCTION CHAR/ORD under a program collating sequence (native ASCII
  today). See the Gap 2 section above; lower priority (no NIST test exercises it under a non-native
  PCS).
