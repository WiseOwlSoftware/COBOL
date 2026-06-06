# COBOL.NET — ISO/IEC 1989:2023 Conformance Plan (SINGLE SOURCE OF TRUTH)

> **Purpose.** This is the durable, authoritative work-breakdown to take the compiler to **complete,
> production-quality ISO/IEC 1989:2023 COBOL support**. Work *from* this document — do **not** re-run the gap
> analysis each session (it has been done at least three times). When an item lands, tick it here in the same
> commit. When you discover a new gap, add it here. This supersedes ad-hoc backlogs scattered across DEVLOG /
> memory; `docs/MULTIVERSION_ROADMAP.md` is the high-level milestone view and points here for detail.
>
> **Provenance.** Built 2026-06-05 from a 15-area parallel spec-conformance audit (workflow
> `iso2023-conformance-audit`, 15 agents vs `specs/ISO_COBOL.md` + the actual compiler) plus the session's own
> surveys. Audit-confirmed findings are tagged **[A]**.

## 0. How to use this document

- **Process (unchanged project discipline):** implement **one item at a time on `main`**; build all layers
  together (grammar → semantic/binder → lowering → CIL emit → runtime → **output-verifying test**); run
  `bash scripts/guard.sh` after each (must stay **ALL GREEN**, 0 NIST FAIL); commit with a **DEVLOG** entry; then
  tick the item here.
- **Conformance testing is PART AND PARCEL of every post-1985 feature (owner directive).** Every 2002/2014/2023
  feature ships, **in the same commit**, with at least one conformance test in `tests/conformance/<version>/` — a
  `<name>.cob` + `<name>.out` (expected stdout), **auto-discovered** by `ConformanceTests` and run inside the
  guard. This corpus is the NIST-equivalent for the post-1985 standards (NIST CCVS covers only '85). See
  `tests/conformance/README.md`. It is both conformance evidence and the regression net as features accrue.
- **Do NOT use worktree-isolated implementation workflows** for compiler fixes — in this repo `isolation:'worktree'`
  branches from a stale commit. Do compiler work directly on `main`. (Parallel *audit/design* agents are fine.)
- **Grammar changes are pre-authorized** in this conformance effort (log + full guard); new lexer keywords must be
  corpus-checked (grep NIST/fixtures for standalone occurrences) before adding.
- **Status legend:** ☐ todo · ◐ partial · ☑ done · 🐛 = a *correctness* defect (silent wrong result), not just a
  missing feature — these rank above pure feature gaps.

---

## 0.5 ⛔ TOP PRIORITY (set 2026-06-06): the .NET-native data-model migration comes BEFORE further conformance features

The owner has directed a foundational re-architecture to **"the best native .NET implementation of COBOL,"** and it
is the **#1 work item for the next session — ahead of every remaining M2/M3/M4 feature in §3 below.** Do it FIRST.

- **The design is settled and reviewed — do NOT re-litigate it** (the owner co-authored it across a long dialogue,
  DEVLOG 393). Read both first:
  - `docs/DATA_MODEL_ARCHITECTURE.md` — the ADR. Typed-native is the default: COBOL records → .NET `record struct`;
    elementary items → native value types (`long`/`decimal`/`double`/`bool`); **character data (PIC X *and* PIC N)
    → `string` (UTF-16)**; a byte image is only a **classifier-scoped fallback** for REDEFINES/RENAMES type-puns,
    file records, and a few hot loops (an inline value type embedded in the typed record — never a heap `byte[]`);
    pointers → managed references; OO → .NET classes; in-memory representation is decoupled from external encoding
    (`CODE-SET` is a boundary concern). Cecil/CIL stays primary; a Roslyn C# backend (Cecil as oracle) is a later
    phase.
  - `docs/DATA_MODEL_REVIEW.md` — a ~57-agent adversarial review; verdict **proceed-with-changes**; all 4 high + 6
    medium findings already folded into the ADR.
- **Execute the migration in the ADR's 7 stages (§10), guard-green at EVERY step, one rule at a time:** Stage 0
  scaffolding (classify *everything* byte-backed = today's behavior) → Stage 1 numeric pipeline + differential
  oracle → Stage 2 classifier (fallback on) → Stage 3 flip typed one rule at a time (**character data first — the
  cheapest, highest-payoff flip**) → Stage 4 pointers + OO → Stage 5 Roslyn C# backend w/ Cecil oracle → Stage 6
  finalize runtime + post-conformance rename (`CobolSharp` → `COBOL.NET`, exe `cobol.exe`).
  - **PROGRESS — Stage 0/1 slice 1 LANDED (DEVLOG 394):** ☑ the numeric substrate is in (`src/CobolSharp.Runtime/
    Numeric/`): `CobolRounding` (8 ISO modes), **`CobolDecimal`** (the exact `BigInteger` base-10 carrier — the
    owner-gated substrate), `NumProfile` (runtime numeric descriptor + `FromDescriptor` bridge), **`CobolNum`**
    (`ScaleAndRound`/`TryStore`: scale→round→capacity→SIZE-ERROR, never throws). ☑ the **Stage-1 differential
    oracle** proves `TryStore` bit-identical to the legacy byte pipeline across field×value×8-mode (DISPLAY/COMP/
    COMP-3/COMP-5 · signed/unsigned · leading-/trailing-P · 10–18-digit mid-range), with an independent
    `BigInteger`/two's-complement reference beyond the legacy faithful window (19–31 digits, 8-byte unsigned COMP-5,
    COMP-5 signed extreme, PROHIBITED). All additive — nothing in the pipeline calls it yet. The oracle surfaced &
    fixed **2 legacy spec bugs** (unsigned COMP-3 sign nibble; trailing-P `WouldOverflow` divide). Guard 1144/481/364.
  - **PROGRESS — Stage 1 first wiring LANDED (DEVLOG 395):** ☑ `PicRuntime.StoreArithmeticResult` (the single
    choke point for ALL COBOL arithmetic — ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE) now delegates its value-level
    scale→round→capacity→SIZE-ERROR decision to `CobolNum.TryStore`; the legacy `WouldOverflow`/`IsInexactAtScale`/
    `CountDigits` removed. The full NIST arithmetic corpus flows through `CobolNum` byte-identically (guard
    1144/481/364). The wiring + guard caught a real **layering correction**: the unsigned-magnitude rule belongs
    to the receiver's *representation* (the encoder), NOT the value-level store — a numeric-edited receiver renders
    its sign via the edit pattern and needs the signed value; `TryStore` now returns the signed rounded value
    (ADR §5 updated).
  - **PROGRESS — Stage 1 value-level scale/round COMPLETE (DEVLOG 396):** ☑ `ApplyScalingAndRounding` (the MOVE /
    numeric-edited / ACCEPT-to-numeric / MOVE-literal / DIVIDE-REMAINDER paths, 8 call sites) now delegates to
    `CobolNum.ScaleAndRound`; the legacy decimal `RoundToIntegerByMode`/`NearestTowardZero` retired. **Every**
    value-level numeric scale/round in the runtime now flows through `CobolNum` (arithmetic via `TryStore`,
    MOVE/edited/remainder via `ScaleAndRound`). Guard 1144/481/364, green first try (the 395 sign-layering fix held).
  - **PROGRESS — Stage 2 classifier Phase A LANDED (DEVLOG 397):** ☑ `RecordClassificationPass` (ADR §3 — the
    typed-vs-byte brain) — Phase-A data-division triggers (REDEFINES/RENAMES/FD-record/LINKAGE/EXTERNAL-GLOBAL/
    edited) + REDEFINES-class & downward-transitivity fixpoint; `Classify(items, categoryOf)`, default typed /
    "any doubt → byte". Additive + unit-tested (15), **not yet consumed by codegen** (Stage 2 = all byte-backed).
    2-lens/17-agent review: 0 confirmed / 15 refuted (Phase-A verified correct). Pipeline investigation map
    captured in DEVLOG 397 (IrLocation hierarchy @ `IrInstruction.cs:1246+`, the dispatch points, insertion
    point after `StorageLayoutComputer`).
  - **NEXT:** classifier **Phase B** (procedure-division scan: refmod-of-numeric-DISPLAY, group MOVE/COMPARE/
    class-condition, CALL…USING BY REFERENCE, ODO-whole-group, write-pattern) + **Phase C** cross-edge fixpoint —
    required before the classifier is consumed (ADR §3: complete before any flip); then a full adversarial review
    of the complete classifier. THEN Stage 0 scaffolding (`IrDataSlot`/`ByteWindowSlot` + `Span<byte>` adapters,
    `PicDescriptor`→`FieldShape` split per ADR M6) and Stage 3 (the `IrDataSlot` MOVE/COMPARE dispatch + the first
    character-data typed flip — PIC X → .NET string).
- **Owner success criterion: every currently-passing test stays green at 100% throughout — fix bugs as the
  migration surfaces them. Run autonomously, with maximal parallelism** (parallel design/audit agents are fine;
  do the compiler edits themselves directly on `main`, NOT in worktree-isolated workflows — they branch stale).
- **Resolve these owner-gated decisions at the stage that needs them** (ADR §12): **#1 numeric substrate =
  `BigInteger` (not `decimal`) for 19–31-digit values + intermediates — REQUIRED before Stage 1**; the
  classifier-trigger completeness (CALL…BY REFERENCE / LINKAGE / refmod-of-numeric-DISPLAY / group-with-COMP /
  PROGRAM COLLATING SEQUENCE) must be in place before Stage-3 typed flips; and the four tracked completeness
  investigations (USE FOR DEBUGGING, EXTERNAL memory model, EXEC SQL/CICS host-var ABIs, Stage-5 oracle
  determinism).
- **Why first:** the ADR opens with "we should have started with this." Every conformance feature added on the
  byte-array model has to be migrated again. Land the data model, keep the suite green, *then* resume the M2/M3/M4
  catalog below on the new foundation.

---

## 1. Current status — DONE (do not re-list as gaps)

- **M1 (COBOL-85): COMPLETE.** NIST CCVS85 = 364 baselines green (NC/IF/SM/IC/SQ/RL/IX/ST/OBSQ). Report Writer,
  collating, intrinsic-function set, file I/O all done.
- **M2 (COBOL-2002): in progress.** Landed this drive (DEVLOG 353–369):
  - **WS-2002-FORMAT — COMPLETE:** `*>` inline comments; `>>SOURCE FORMAT IS FREE|FIXED`; conditional compilation
    `>>DEFINE` / `>>IF`/`>>ELSE`/`>>END-IF` / `>>EVALUATE`/`>>WHEN`/`>>END-EVALUATE`; recognize-and-ignore of the
    other standard `>>` directives.
  - **OPTIONS paragraph** — parsed/accepted (clauses **not yet applied** — see M2-ARITH-1).
  - **REPOSITORY paragraph** — parsed/accepted (specifiers **not yet bound** — see M2-UDF-4).
  - **User-defined functions — CORE:** `FUNCTION-ID … END FUNCTION` units (compiled as callable programs);
    `FUNCTION user-name(args)` invocation as the **whole source of a MOVE/COMPUTE** (any arity, numeric **and**
    alphanumeric).
  - **CALL … RETURNING** into WORKING-STORAGE (the LINKAGE-location wiring fix).
  - **SORT Format-2** elementary self-key; **READ … PREVIOUS** (indexed + relative).
  - Intrinsic-function set already broad (incl. 2002/2014/2023 fns in `IntrinsicFunctions.cs`); EXIT
    PARAGRAPH/SECTION/PERFORM[ CYCLE] grammar present.
- **M2 — landed THIS session (DEVLOG 370–382), each with a `tests/conformance/2002/` test + this plan ticked:**
  - **Conformance framework + this SSOT** (370–371): the version-conformance corpus (`tests/conformance/<ver>/`,
    auto-discovered by `ConformanceTests`) and this plan document itself.
  - **M4-1** DELETE FILE (373) · **M4-2a** logical XOR / EXCLUSIVE-OR (375).
  - **M2-UDF-1/2** general inline UDF invocation + literal/arith args (372, 374).
  - **M2-DATA-2** FLOAT-SHORT/LONG/EXTENDED (376, 377) · **M2-DATA-1** BINARY-CHAR/SHORT/LONG/DOUBLE (380).
  - **M2-PROC-2** INSPECT … BACKWARD (378) · **M2-PROC-1** INITIALIZE TO VALUE/DEFAULT/FILLER (381).
  - **M2-ARITH-1** ROUNDED MODE — all 8 modes + per-statement `MODE IS` + CORRESPONDING (379, 382); ◐ two
    follow-ups remain (OPTIONS DEFAULT ROUNDED MODE, PROHIBITED→EC-SIZE-TRUNCATION).
  - **M2-DATA-3** National (UTF-16) data — CORE COMPLETE (383): `PIC N`/`USAGE NATIONAL`, `N"…"`, MOVE/VALUE/
    INITIALIZE/SPACE/DISPLAY/compare + national↔alpha/numeric conversion. Conformance `national_data`.
    An adversarial review caught + fixed silent corruption in the first slice before commit.
  - **M2-DATA-4** Boolean data — CORE COMPLETE (386): `PIC 1`/`USAGE BIT`, `B"…"`, MOVE/VALUE/INITIALIZE/ZERO/
    DISPLAY/compare/JUSTIFIED. Conformance `boolean_data`. A 2-agent review caught + fixed VALUE corruption,
    boolean-in-COMPUTE silent-numeric, and a JUSTIFIED spec/dead-code defect before commit. Bit operators deferred.
  - **M2-PROC-6** `GOBACK RETURNING` — DONE (387, dialect-gated 2002+; conformance `goback_returning`); EXIT
    variants verified present/green; only `CONTINUE AFTER` (non-deterministic) deferred.
  - **M2-DATA-5** Pointers **Phase-1 DONE** (389): `USAGE POINTER` (8-byte handle), `NULL`, `SET p TO NULL`/
    `SET p TO q`, `= NULL`/`= q`; pointer↔non-pointer MOVE + VALUE rejected; conformance `pointer_data`. A 2-agent
    review confirmed clean (self-review had caught 3 bugs first). **Phase-2 (ADDRESS OF/BASED/ALLOCATE) remains the
    owner-gated `PointerRegistry` design decision.**
  - **M2-ARITH-1 follow-up #1** OPTIONS `DEFAULT ROUNDED MODE` applied to bare ROUNDED — DONE (391; conformance
    `options_default_rounded`).
  - **M2-ARITH-1 follow-up #2** ROUNDED MODE PROHIBITED → SIZE ERROR — DONE at the observable level (392):
    `ON SIZE ERROR` now fires on an inexact PROHIBITED result and the receiver is left unchanged (conformance
    `rounded_mode_prohibited`). The named-EC exception object (EC-SIZE-TRUNCATION) + USE framework remains future
    work under M2-PROC-4.
  - **Bug fix (392):** `IS ALPHABETIC` restricted to ISO §8.8.4.4 {A–Z, a–z, space} (was `char.IsLetter`,
    Unicode-wide). Both 392 fixes were surfaced by the data-model ADR review.
  - **🏗 DATA-MODEL RE-ARCHITECTURE designed + adversarially reviewed (393)** — the ADR + review docs (see §0.5).
  - **🏗 DATA-MODEL MIGRATION STARTED — Stage 0/1 substrate (394) + Stage-1 first wiring (395):** the `BigInteger`
    numeric substrate (`CobolRounding`/`CobolDecimal`/`NumProfile`/`CobolNum`) + differential oracle (2 legacy spec
    bugs fixed: unsigned COMP-3 sign nibble, trailing-P `WouldOverflow`); then `StoreArithmeticResult` (all COBOL
    arithmetic) delegated to `CobolNum` byte-identically, which surfaced + corrected the unsigned-sign layering (ADR
    §5). See §0.5 PROGRESS.
- **Guard baseline:** **1144 unit / 481 integration / 364 NIST** (all green; `bash scripts/guard.sh`).
  (+92 unit this session: the Stage-0/1 numeric substrate — `CobolDecimalTests` + `CobolNumDifferentialTests`
  (the differential oracle) + the `PicRuntimeRegressionTests` for the 2 legacy fixes (394). The substrate is
  additive — no NIST/integration change. Prior tail (392/393): the two ADR-review fixes + `rounded_mode_prohibited`.)
- **NEXT UP → THE .NET-NATIVE DATA-MODEL MIGRATION (see §0.5) — ahead of everything below.** Only after it lands
  with the suite green do the remaining M2 items resume (the easy/foundational data items — National, Boolean,
  Pointers Phase-1, GOBACK RETURNING — are already done): **M2-DATA-5 Phase-2 (ADDRESS OF/BASED/ALLOCATE) —
  OWNER-GATED**
  (the `PointerRegistry` handle→address/.NET-managed-memory design decision); **M2-PRE-1** (◐, re-scoped — two
  real but rare preprocessor mis-parse/clean-error defects, one reverses a deliberate §7.3.16-vs-§7.2 decision);
  **M2-ARITH-1/-2** (OPTIONS DEFAULT ROUNDED / standard arithmetic — needs OPTIONS-clause parsing);
  **M2-FILE-1/2** (SHARING/LOCK, line-sequential); then the large subsystems **M2-PROC-4 EC/exceptions →
  RAISE/RESUME/USE**, **M2-PROC-3 VALIDATE**, **M2-OO-1 OO COBOL** (§4 waves). Pick per §4; tick + log here.

---

## 2. Milestones

| Milestone | Standard | Validation |
|---|---|---|
| **M2** | COBOL-2002 | custom corpus `tests/conformance/2002/` (+ the existing SpecFixTests) |
| **M3** | COBOL-2014 | custom corpus `tests/conformance/2014/` |
| **M4** | COBOL-2023 | custom corpus `tests/conformance/2023/` (vs in-repo `specs/ISO_COBOL.md`) |

Order: finish M2, then M3, then M4. Within a milestone, do correctness defects (🐛) and high-value/small items
first; large subsystems (pointers, exceptions, OO) last.

---

## 3. Work-breakdown — the catalog

Each item: **ID** · feature · spec ref · severity · tractability · current state · recipe (files) · deps.

### 3.1 M2 — User-defined functions (finish the workstream)

- ☑ 🐛 **M2-UDF-1 — General inline UDF invocation (DONE — DEVLOG 372).** `FUNCTION user-name(args)` inside a
  larger expression now evaluates correctly (was silently 0). Implemented via runtime helper
  `CobolProgramRegistry.InvokeNumericFunction` (scratch RETURNING buffer + Resolve + Invoke + DecodeNumeric) so
  the inline emit only builds the args array; a Compilation pre-pass builds the signature registry
  (`SemanticModel.UserFunctionSignatures` = name → RETURNING length+PIC); `IrUserFunctionCall` +
  `ExpressionLowerer` routing. Verified `COMPUTE R = FUNCTION DOUBLER(X) + 1` → 43. Conformance:
  `tests/conformance/2002/udf_inline_expression`. **NOTE:** applies when every argument is a storage location;
  literal/arith args are M2-UDF-2 below.
- ☑ 🐛 **M2-UDF-2 — Literal / arithmetic-expression arguments (DONE — DEVLOG 374).** `FUNCTION FOO(5)` /
  `FOO(A + 1)` now evaluate correctly (were silently 0). The Compilation pre-pass also records each function's
  USING-parameter signatures (`SemanticModel.UserFunctionParameterSignatures`); `IrUserFunctionArg` carries either
  a location (BY CONTENT) or a value + the target parameter's length/PIC; the emit encodes a value argument via
  `CobolProgramRegistry.EncodeFunctionArg` (`PicRuntime.EncodeNumeric`). Verified `DOUBLER(5)` = 10,
  `DOUBLER(4+1)` = 10. Conformance `tests/conformance/2002/udf_value_args`. **The UDF correctness chapter
  (M2-UDF-1 + M2-UDF-2) is closed for numeric functions.**
- ☐ **M2-UDF-3 — Separate-compilation user functions (prototypes).** Caller + function in different translation
  units. *Medium.* §8.13 external repository + function-prototype. *Current:* caller and function must share one
  compilation group. *Recipe:* function-prototype definitions in the caller (or an external repository registry).
- ☐ **M2-UDF-4 — Bind REPOSITORY FUNCTION specifiers.** Enable `REPOSITORY. FUNCTION ALL INTRINSIC` (call
  intrinsics without the `FUNCTION` keyword) + named function specifiers. *Medium (parser ambiguity: `name(args)`
  vs subscript).* *Current:* REPOSITORY parses but specifiers are inert. §12.3.8.

### 3.2 M2 — Data types

- ☑ 🐛 **M2-DATA-1 — `USAGE BINARY-CHAR/SHORT/LONG/DOUBLE [SIGNED|UNSIGNED]` (DONE — DEVLOG 380).** §13.18.60.
  4 new `UsageKind` markers (BinaryChar/Short/Long/Double) + `SIGNED`/`UNSIGNED` reserved words + `binarySign`
  grammar; FieldSizeCalculator 1/2/4/8 bytes; `StorageLocation` synthesizes a COMP-5 descriptor (explicit width
  + TotalDigits 3/5/10/19-or-20 + sign); `DataSymbol.IsUnsignedBinary`/`IsFixedWidthBinary`; PicUsageResolver/
  RecordLayoutBuilder/SemanticBuilder threaded. Runtime COMP-5 codec extended to 1-byte (sbyte/byte) — no PIC'd
  COMP-5 is ever 1 byte. Verified BC=127/-128, BCU=255, full-width SHORT/LONG/DOUBLE, COMPUTE. Conformance
  `tests/conformance/2002/binary_usage`.
- ☑ **M2-DATA-2 — `USAGE FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED` (DONE — DEVLOG 376, 377).** Aliased onto
  COMP-1/COMP-2 (3 edits each: lexer tokens + usageKeyword/bare-form alts + `UsageMapper.FromUsageKeyword`
  FLOAT-SHORT→Comp1 / FLOAT-LONG→Comp2 / FLOAT-EXTENDED→Comp2). FLOAT-EXTENDED maps to double (.NET has no
  native 128-bit float — documented approximation; a true soft-float quad path is out of scope). Verified
  10.5*2=21, 100.25*4=401, 250.5*2=501. Conformance `tests/conformance/2002/float_usage` (all three usages).
- ☑ **M2-DATA-3 — National data (CORE DONE — DEVLOG 383).** `USAGE NATIONAL` + `PIC N(n)` (UTF-16, **2
  bytes/char**), `N"…"`/`n'…'` literals, and the full data-movement surface: MOVE national←national
  (left-justify, U+0020 pad, right truncate, JUSTIFIED RIGHT), national↔alphanumeric and numeric→national
  conversion (Latin-1 subset), figurative/literal **VALUE**, **MOVE SPACE / INITIALIZE** (national-space
  fill), **DISPLAY** (UTF-16 decode), and **national comparison** (field=field, field=literal). Single sizing
  pipeline (`ComputeStorageLength` doubles for `IsNationalLike`); `NATLIT` lexer token + `nonNumericLiteral`
  + binder; `USAGE NATIONAL`→`UsageKind.National` (category drives behavior); runtime `WriteNationalChars`/
  `MoveNationalToNational`/`MoveStringLiteralToNational`/`MoveAlphanumericToNational`/`MoveNumericToNational`/
  `MoveNationalToAlphanumeric[Edited]`/`CompareNational`/`CompareNationalToString`/national arm in
  `MoveFigurativeToField`; emit-time category dispatch in `CilDataEmitter`/`CilComparisonEmitter`/`CilEmitter`
  (VALUE). **An adversarial review (3 agents) caught that the first slice silently corrupted national↔alpha
  MOVE, figurative/VALUE/INITIALIZE fill, and comparison — all fixed before commit.** Conformance
  `tests/conformance/2002/national_data` (13 assertions). **Deferred:** NATIONAL-EDITED, `NX"…"`, full
  implementor correspondence + `EC-DATA-CONVERSION` (Latin-1 only), collating-sequence national compare,
  ref-mod ×2 byte adjustment, INSPECT-national.
- ☑ **M2-DATA-4 — Boolean & bit data (CORE DONE — DEVLOG 386).** `PIC 1(n)` / `USAGE BIT` (one byte/position,
  ASCII `'0'`/`'1'`), `B"…"`/`b'…'` literals, and the full data surface: MOVE boolean←boolean (`'0'` fill / right
  truncate / JUSTIFIED), literal/figurative-ZERO **VALUE**, **MOVE ZERO / INITIALIZE** (boolean zero), **DISPLAY**,
  **comparison** (field=field, field=literal), and **JUSTIFIED RIGHT** (§13.18.32 — the fix also un-deadened
  national JUSTIFIED). A 2-agent adversarial review caught + fixed three defects before commit: non-boolean VALUE
  corruption (§13.18.63 GR10 → CBL1002), a boolean nested in a COMPUTE tree read as numeric (→ CBL2601, recursed),
  and JUSTIFIED wrongly rejected on boolean/national (CBL0803). Conformance `tests/conformance/2002/boolean_data`.
  **Deferred:** bit operators B-AND/B-OR/B-XOR/B-NOT (reserved-word collision; needs the XOR wiring pattern),
  `BX"…"`, true bit-packing, GROUP-USAGE BIT, boolean↔non-boolean compare strictness, boolean ref-mod, the broader
  alphanumeric/national-in-COMPUTE latent hole, uninitialized-boolean=0x20 (matches the National precedent).
  - **Investigated first-slice recipe (workflow `m2-data4-boolean-investigation`, 2026-06-05) — mirrors the
    National build (DEVLOG 383) almost exactly.** Corpus-checked clean: NIST has no `PIC 1`, no `USAGE BIT`, no
    real `B"…"` (only `"B"` strings → STRINGLIT). **Storage model:** 1 byte per boolean position holding ASCII
    `'0'`/`'1'` — spec-permitted (§13.18.40.4 R14: a boolean char "may be represented … as an alphanumeric
    character"); simplest correct; true bit-packing (§8.5.1.6.3) deferred. So boolean ≈ "alphanumeric with `'0'`
    fill + a distinct category + no DISPLAY trim". **Touch (mirror National):** (1) `PicDescriptor.cs` add
    `CobolCategory.Boolean` + `IsBooleanLike()` + `UsageKind.Bit`; (2) `PicDescriptorFactory.cs` add a
    `hasBooleanChars` flag + `case '1'` (integerDigits += count) + category lattice (Boolean when only `'1'`);
    sizing = 1 byte/position (default arm, no multiplier); (3) `PicUsageResolver.cs` set `isBool` from category,
    add `'1'` to `IsValidPicSymbol`, map `"BIT"`→`UsageKind.Bit`, route `picString==null && usage==Bit`→Boolean;
    (4) `CobolData.g4` `usageKeyword += BIT`; (5) `CobolLexer.g4` add `BIT` token + `BOOLLIT : 'B' '"' [01]+ '"' |
    'B' '\'' [01]+ '\''` (before IDENTIFIER, maximal-munch safe); (6) `CobolExpressions.g4` `nonNumericLiteral +=
    BOOLLIT`; (7) `ExpressionBinder.BindNonNumericLiteral` BOOLLIT→`BoundLiteralExpression(text, Boolean)` (strip
    `B`+quotes; no doubled-quote); (8) `SemanticBuilder` VALUE-clause BOOLLIT extraction (mirror the NATLIT arm);
    (9) `CategoryCompatibility` add `(Boolean,Boolean)` move pair + `IsBooleanFamily` + comparison case; (10) **the
    full dispatch surface up front (National-review lesson):** runtime `MoveBooleanToBoolean` (`'0'` fill/right
    truncate), `MoveStringLiteralToBoolean`, `CompareBoolean`/`CompareBooleanToString` (`'0'` pad), boolean arm in
    `MoveFigurativeToField` (ZERO/`'0'`), `GetDisplayString` boolean branch (show raw `'0'`/`'1'`, **no TrimEnd**),
    `MoveBooleanLiteralToOccursField` (VALUE); emit dispatch in `CilDataEmitter.EmitMoveFieldToField` +
    `EmitMoveStringToField`, `CilComparisonEmitter`, `CilEmitter` VALUE-init, and INITIALIZE-default in
    `DataMovementLowerer`. **Conformance** `tests/conformance/2002/boolean_data` covering literal/MOVE/VALUE/
    INITIALIZE/DISPLAY/compare. **Defer:** `BX"…"` hex, **bit operators B-AND/B-OR/B-XOR/B-NOT** (corpus-collision
    risk — `B-NOT` seen as an identifier in a unit test; needs careful reserved-word handling + the XOR-operator
    wiring pattern, DEVLOG 375), true bit-packing, GROUP-USAGE BIT, SET cond-name interplay.
- ◑ **M2-DATA-5 — Pointers & based addressing. PHASE-1 DONE (DEVLOG 389); Phase-2 owner-gated.** **[A] HIGH.**
  **Phase-1 COMPLETE:** `USAGE POINTER` (8-byte opaque handle, no PIC), `NULL`, `SET p TO NULL` / `SET p TO q`,
  `= NULL` / `NOT = NULL` / `= q` equality; pointer↔non-pointer MOVE rejected (CBL0901); `VALUE` on a pointer
  rejected (CBL1002); default INITIALIZE leaves a pointer unchanged. Conformance `tests/conformance/2002/
  pointer_data`. Lean reuse: SET→MOVE, NULL→0x00 figurative fill, `= NULL`→figurative compare, `= q`→
  `IrStringCompare` (no new IR nodes). **Phase-2/3 — STILL OWNER-GATED (the `PointerRegistry` handle→address
  design decision):** `ADDRESS OF` / `SET ADDRESS OF` / `SET … UP/DOWN BY` / `BASED` deref → then `ALLOCATE`/
  `FREE` (M2-PROC-5) — these need a safe mapping of COBOL pointers onto .NET managed memory (`GCHandle` pinning).
  Also deferred: ordering-operator rejection on pointers (`< >` compile to a byte-compare today; invalid input),
  PROGRAM-/FUNCTION-POINTER distinctions, and updating/removing the orphaned `LoweringTable` (no Pointer cases).
  **Phase-1 is thin standalone** — every pointer can only be NULL until ADDRESS OF/ALLOCATE exist. §13.18, §14.9.
  - **Investigated first slice (handoff audit, 2026-06-05) — the audit's RECOMMENDED next pick** (foundational +
    smallest Phase-1, no grammar ambiguity). Current state: `UsageKind.Pointer` enum value exists but is **inert**
    (no grammar for ADDRESS OF/SET ADDRESS OF/BASED/NULL, no bound pointer node); `CobolDataPointer` exists but is
    used only for CALL BY REFERENCE/CONTENT, not USAGE POINTER. **Phase-1 slice:** `USAGE POINTER` (alias all
    three pointer usages) → 8-byte opaque **handle** storage; `NULL` literal = handle 0; `SET p TO NULL`; pointer
    `= NULL` comparison. (No managed-address taking — a `PointerRegistry` maps handles → targets; this is the
    safe-handle design decision the next session must confirm.) **Defer (Phase-2/3, the hard parts):** `SET
    ADDRESS OF` / `SET … UP/DOWN BY` (needs `GCHandle`-pinned addresses of BASED items — the .NET managed-memory
    problem), `BASED` deref, then `ALLOCATE`/`FREE` (M2-PROC-5).
  - **Turnkey Phase-1 implementation map (workflow `m2-data5-pointers-p1-investigation`, 2026-06-05).** Phase-1
    (POINTER 8-byte handle + NULL + SET p TO NULL + SET p TO q + `= NULL`/`= q` equality) does **NOT** need the
    deferred handle→address `PointerRegistry` decision — that's only for Phase-2 dereference (ADDRESS OF/BASED).
    NULL = handle 0 = 8 zero bytes. **Spec corrections to the slice above:** (a) **`VALUE NULL` is PROHIBITED on
    pointer items** (§13.18.26 SR9) — do NOT implement it; instead REJECT a VALUE clause on a pointer (mirror the
    boolean VALUE check). (b) Comparison is **equality only** (= / NOT =), operands same category or NULL.
    **Lean approach — reuse existing machinery, minimal new code:** model pointer as `CobolCategory.Pointer`
    (8 bytes); **SET p TO NULL / SET p TO q → lower as a MOVE** in `BindSetToValue` when the target is a pointer
    (NULL figurative → `MoveFigurativeToField` default fills 0x00×8 since `FigurativeToByte(Null)=0x00`; pointer←
    pointer → an 8-byte byte-copy), bypassing the numeric index-set path; **`IF p = NULL` reuses the
    figurative-comparison path** (NULL→0x00 fill, like LOW-VALUE — verify `EmitLocationVsFigurative` handles
    `FigurativeKind.Null`→0x00) and **`IF p = q` reuses `IrStringCompare`** (byte compare, correct for equality on
    equal-length 8-byte handles) — so **no new IR/runtime compare nodes needed**. **Touch:** (1) `PicDescriptor.cs`
    add `CobolCategory.Pointer` (+ optional `IsPointerLike()`); `UsageKind.Pointer` already exists. (2) 8-byte
    sizing at the 4 sites (mirror BinaryDouble=8): `FieldSizeCalculator.ComputeElementSize` (`Pointer`→8),
    `PicDescriptorFactory.ComputeStorageLength` (`case Pointer`→8), `StorageLocation`/`CompilerPicDescriptorFactory`
    synth an 8-byte non-numeric descriptor (category Pointer), `RecordLayoutBuilder.MapToIrType` (`Pointer`→
    ByteArray). (3) `PicUsageResolver.ResolveForDataItem` `picString==null && usage==Pointer`→category Pointer.
    (4) `CobolExpressions.g4` add `NULL_` to `figurativeConstant`; `ExpressionBinder.BindFigurativeConstantExpression`
    `NULL_`→`BoundFigurativeExpression(FigurativeKind.Null)` (both exist; `NULL_` token currently used only in
    `objectReference` — disambiguate by target category, no grammar conflict). (5) `DataStatementBinder.BindSetToValue`
    pointer-target → emit a `BoundMoveStatement` (+ reject SET p TO a non-pointer/non-NULL). (6) `CilDataEmitter
    .EmitMoveFieldToField` `(Pointer,Pointer)`→8-byte byte copy; ensure `MoveFigurativeToField(Null)` over a pointer
    fills 0x00×8. (7) `CategoryCompatibility` add `(Pointer,Pointer)` move + comparison + `IsPointerFamily`.
    (8) `ConditionLowerer` route pointer Location/Location → `IrStringCompare`; handle pointer vs NULL figurative.
    (9) `DataItemClassifier.ValidateValueClause` REJECT a VALUE on a pointer item. (10) restrict pointer comparison
    to = / NOT = (reject ordering). Conformance `tests/conformance/2002/pointer_data` (declare, SET TO NULL, = NULL
    YES, two NULLs equal). **Note:** Phase-1 is thin standalone (every pointer can only be NULL until ADDRESS OF/
    ALLOCATE exist) — its value is the foundation; the useful Phase-2 (dereference) is the owner-gated design decision.

### 3.3 M2 — Arithmetic & configuration

- ◑ 🐛 **M2-ARITH-1 — `ROUNDED MODE` (all 8 modes DONE — DEVLOG 379).** All eight modes (AWAY-FROM-ZERO,
  NEAREST-AWAY-FROM-ZERO, NEAREST-EVEN, NEAREST-TOWARD-ZERO, PROHIBITED, TOWARD-GREATER, TOWARD-LESSER,
  TRUNCATION) implemented in `PicRuntime.RoundToIntegerByMode` + per-statement `ROUNDED MODE IS …`
  (new mode reserved words, `roundedPhrase` grammar, `BindRounded`, `BoundArithmeticTarget.RoundingMode`,
  lowerer threads the mode int — IR/emit were already mode-agnostic). Conformance
  `tests/conformance/2002/rounded_modes`; CORRESPONDING `ROUNDED MODE` also DONE (DEVLOG 382,
  `tests/conformance/2002/corresponding_rounded_mode`). **Follow-ups:** (1) ☑ **DONE (DEVLOG 391)** — OPTIONS
  `DEFAULT ROUNDED MODE` now sets the bare-ROUNDED default: `SemanticBuilder.VisitOptionsParagraph` token-scans
  the OPTIONS blob for `DEFAULT ROUNDED [MODE] [IS] <mode>`, `Compilation` transfers it to
  `SemanticModel.DefaultRoundingMode`, `BindRounded` (now instance) returns it for a bare ROUNDED; per-statement
  `MODE IS` still overrides. Conformance `options_default_rounded`. (2) ☐ PROHIBITED → raise EC-SIZE-TRUNCATION
  on an inexact result — **blocked on the EC framework (M2-PROC-4)**; needs the arithmetic store paths to detect
  precision loss. §14.9.4, §11.9.6.
- ◑ **M2-ARITH-2 — Apply remaining OPTIONS clauses.** `DEFAULT ROUNDED MODE` is now **applied** (DEVLOG 391, see
  M2-ARITH-1 follow-up #1 — the OPTIONS token-scan + `SemanticModel.DefaultRoundingMode` infrastructure is in
  place). Still recognize-and-ignore: `ARITHMETIC IS STANDARD/STANDARD-BINARY/STANDARD-DECIMAL`,
  `INTERMEDIATE ROUNDING`, `FLOAT-BINARY/DECIMAL DEFAULT`, `ENTRY-CONVENTION`. *Medium–large* (standard arithmetic
  is a real intermediate-precision change). §11.9.

### 3.4 M2 — Procedure-division statements

- ☑ 🐛 **M2-PROC-1 — `INITIALIZE … TO VALUE` / `THEN TO DEFAULT` / `WITH FILLER` (DONE — DEVLOG 381).** §14.9.20.
  Binder now captures all three phrases (`BoundInitializeStatement.WithFiller/ToValue/ToValueCategory/ToDefault`);
  the lowerer applies per-item precedence TO VALUE → REPLACING → default, includes FILLER under WITH FILLER, and
  suppresses the default for non-VALUE items under TO VALUE unless TO DEFAULT. `EmitValueClauseInit` emits each
  item's declared VALUE (figurative / ALL-literal / numeric / string). Conformance
  `tests/conformance/2002/initialize_phrases`.
- ☑ **M2-PROC-2 — `INSPECT … BACKWARD` (DONE — DEVLOG 378).** §14.9.21. New `BACKWARD` reserved word +
  grammar `INSPECT BACKWARD? …` + a `bool Backward` threaded Bound→IR→emit→runtime. Implemented as a
  reverse-wrapper: reverse the target + every multi-char operand/delimiter, run the existing forward
  TALLYING/REPLACING/CONVERTING cycle, reverse the result buffer back (TALLYING needs none; FROM/TO sets
  unreversed). Verified backward = exact forward mirror (FIRST→rightmost, LEADING→trailing-run, BEFORE-region
  flips side). Conformance `tests/conformance/2002/inspect_backward`.
- ☐ **M2-PROC-3 — `VALIDATE` statement + validation clauses.** CLASS/DEFAULT/DESTINATION/INVALID/PRESENT WHEN/
  VARYING + error handling. *Large (new facility).* §14.9 VALIDATE, §13.
- ☐ **M2-PROC-4 — Exception handling: `RAISE`, EC framework, `>>TURN` runtime checking, `RESUME`,
  `USE … AFTER EXCEPTION`.** **[A] medium, large.** *Recipe:* an EC-condition catalog, a TURN on/off map, a
  condition register, and runtime guards on checked operations. **Backbone for RAISE/RESUME/USE and
  exception-checking PERFORM.** §14.6, §14.9.
- ☐ **M2-PROC-5 — `ALLOCATE` / `FREE` (based storage).** *Medium.* **Depends on M2-DATA-5 (pointers).** §14.9.
- ◑ **M2-PROC-6 — `GOBACK RETURNING` (DONE — DEVLOG 387); EXIT variants verified; `CONTINUE AFTER` deferred.**
  §14.9.16. `GOBACK RETURNING|GIVING identifier` (dialect-gated 2002+) lowers to a synthetic MOVE into the
  PROCEDURE DIVISION RETURNING item + `IrGoBack` (reuses the CALL…RETURNING wiring); conformance
  `tests/conformance/2002/goback_returning`. EXIT PROGRAM/PERFORM[ CYCLE]/SECTION/PARAGRAPH/METHOD/FUNCTION
  grammar present and EXIT PROGRAM/PERFORM green across NIST — no gap. **Remaining:** `CONTINUE AFTER expr SECONDS`
  (2002 timed delay) — deferred (non-deterministic, poor conformance fit, low value).

### 3.5 M2 — Preprocessor robustness & directives

- ◐ **M2-PRE-1 — Preprocessor robustness trio.** *medium.* **RE-SCOPED 2026-06-05 by an empirical
  repro (workflow `m2-pre1-investigation`): the "crashes the parser" claim is REFUTED** — nothing crashes
  (all exits 0/1, never an ICE); these are clean-error / mis-parse defects on *rare* 2002 constructs, so the
  severity is below a silent-wrong-result 🐛.
  - **(a) mid-file `>>SOURCE FORMAT` switching — MIS-PARSE (real).** `ReferenceFormatProcessor` honors only the
    FIRST directive, whole-file ("first wins", `ReferenceFormatProcessor.cs:86-111`). A genuinely fixed-form
    region after a `>>SOURCE FORMAT IS FREE` keeps its column-1-6 sequence numbers → stray tokens; a later
    FIXED switch is silently ignored. Spec **§7.3.24.3 R1/R5**: a SOURCE FORMAT directive governs the text
    *following it until the next directive* (and reverts at end of COPY'd library text). Fix needs **per-line
    format state** in the normalizer (or a new region-wise pass). Non-trivial.
  - **(b) conditional-compilation directives inside copied library text — CLEAN-ERROR (real).** `Compilation.cs:
    279-301` runs `ConditionalCompilationProcessor` (285) BEFORE `CopyProcessor` (295), so `>>IF`/`>>DEFINE` in a
    copybook pass through as text → stray `>` parse error. Spec **§7.2** order is: COPY *inclusion* (Step 1) →
    DEFINE/IF/EVALUATE (Step 2a) → COPY REPLACING (2d) → REPLACE (Step 3), i.e. **COPY-inclusion-then-CC**. Fix =
    swap so COPY runs before CC (low NIST risk — CC is a no-op on directive-free source). **CAUTION: the current
    order is a DELIBERATE decision** (its comment cites §7.3.16 GR1 to let `>>IF` gate a COPY); reconcile
    §7.3.16 vs §7.2 first — gating still works COPY-first (the expanded text falls in the IF false path, §7.2
    line 3314 "may be omitted"). Verify the `conditional_compilation` conformance test + add a copybook-CC test.
  - **(c) unknown/`>>`-directive handling — ALREADY CORRECT (no fix needed).** Known-ignored directives are
    consumed; an unknown `>>X` surfaces as a clean COBOL0001 by design (typo-catching). Spec §7.3.3/§7.3.4 leave
    unknown-directive behavior implementor-defined (only `>>IMP` reserved). The proposed directive-skip lexer
    rule is at most diagnostic polish — **dropped from the trio**.
- ☐ **M2-PRE-2 — Directive semantics depth.** Does `>>TURN` actually toggle EC checks (pairs with M2-PROC-4); is
  recognize-and-ignore of LISTING/PAGE/LEAP-SECOND/PROPAGATE/FLAG-* conformant; `>>DEFINE … AS PARAMETER` (env
  source); `>>CALL-CONVENTION` effect. *Low–medium.*

### 3.6 M2 — File & I/O (2002+ deltas only; the 85 file I/O is done)

- ☐ **M2-FILE-1 — `SHARING`, `LOCK MODE`/`LOCK ON`, `RETRY`.** *Medium.* §13 FILE-CONTROL, §14.9 OPEN.
- ☐ **M2-FILE-2 — Line-sequential organization** + 2002 FILE STATUS codes. *Medium.*

### 3.7 M2 — Object-Oriented COBOL (the single largest sub-project)

- ☐ **M2-OO-1 — OO COBOL.** CLASS-ID/END CLASS, METHOD-ID/END METHOD, INTERFACE-ID, FACTORY/OBJECT, INHERITS,
  `USAGE OBJECT REFERENCE`, `INVOKE` + inline method invocation, PROPERTY, SELF/SUPER, conformance. *Very large.*
  `CobolParserOO.g4` has CLASS-ID/METHOD-ID grammar scaffolding but it is **not bound/emitted**. Map the object
  model onto .NET types. §11 OO source units, §14.9 INVOKE. **Sequencing is an owner decision (non-OO M2 first).**

### 3.8 M3 — COBOL-2014

- ☐ **M3-1 — Dynamic-capacity tables** `OCCURS DYNAMIC [CAPACITY IN dn] [FROM…TO]`. *Medium–large.*
- ☐ **M3-2 — `TYPEDEF` / `SAME AS` / `TYPE TO`.** *Medium.*
- ☐ **M3-3 — JSON & XML** `JSON GENERATE/PARSE`, `XML GENERATE/PARSE` + special registers (XML-CODE/JSON-CODE/…).
  *Large.* `CobolParserJsonXml.g4` exists — assess completeness; likely needs binder/emit/runtime. §14.9.
- ☐ **M3-4 — File sharing/locking finalization** (if not folded into M2-FILE-1), function/method pointers,
  IEEE-754 alignment, increased limits, conditional-expression enhancements.

### 3.9 M4 — COBOL-2023

- ☑ **M4-1 — `DELETE FILE` execution (DONE — DEVLOG 373).** Full statement pipeline (BoundDeleteFileStatement →
  IrDeleteFile → FileRuntime.DeleteFile = ResolveHostPath + File.Delete + I-O status 00/35/30). Verified status
  35 after delete + the physical file removed. Conformance `tests/conformance/2023/delete_file`. **Follow-up:**
  the `ON EXCEPTION`/`NOT ON EXCEPTION` phrases + multiple file-names are not yet honored.
- ☑ **M4-2a — logical `XOR` / `EXCLUSIVE-OR` operator (DONE — DEVLOG 375).** New `logicalXorExpression` grammar
  level (precedence NOT>AND>XOR>OR), `BoundBinaryOperatorKind.Xor` → `IrLogicalOp.Xor` → CIL `xor`. NOTE: XOR is a
  §8.8.4.9 logical operator (2002-era), not a 2023 delta — the audit mis-tagged it. Conformance
  `tests/conformance/2002/logical_xor`.
- ☐ **M4-2b — `SMALLEST-ALGEBRAIC` + `EXCEPTION-FILE-N` intrinsics.** Both verified REAL but non-trivial:
  EXCEPTION-FILE-N (§15.29) returns NATIONAL data (blocked on M2-DATA-3 national support); SMALLEST-ALGEBRAIC
  (§15.83) = 10^(-fractionDigits) of the argument's PIC — needs the binder to pass the argument's fraction-digit
  count (HIGHEST/LOWEST-ALGEBRAIC pass total digits). Deferred — neither is the trivial dispatch case the audit
  implied.
- ☐ **M4-3 — Other 2023 intrinsic/bit/boolean additions + dynamic-table finalization + clarifications.** Audit
  `specs/ISO_COBOL.md` 2023-marked changes when M4 begins (many intrinsics already in `IntrinsicFunctions.cs` —
  verify completeness + **version gating**, i.e. a 2023 fn used under `--standard cobol85` should flag).

---

## 4. Execution order (waves) — implement top-down

**Wave 1 — UDF correctness + cheap high-value (start here):**
1. **M2-UDF-1** general inline UDF (🐛 silent 0) — and **M2-UDF-2** literal/arith args (🐛) together.
2. **M2-DATA-1** BINARY-CHAR/SHORT/LONG/DOUBLE (🐛 mis-typing).
3. **M4-1** DELETE FILE execution (cheap), **M2-PROC-2** INSPECT BACKWARD (cheap), **M4-2** XOR + 2 intrinsics
   (cheap).
4. **M2-PROC-1** INITIALIZE TO VALUE/DEFAULT/FILLER (🐛).

**Wave 2 — arithmetic + data aliases:**
5. **M2-ARITH-1** ROUNDED MODE (all 8) + DEFAULT ROUNDED (🐛 2-of-8).
6. **M2-DATA-2** FLOAT-SHORT/LONG (alias COMP-1/2).
7. **M2-PRE-1** preprocessor robustness trio (🐛 crashes).

**Wave 3 — new data categories:**
8. **M2-DATA-3** national data · **M2-DATA-4** boolean/bit · **M2-FILE-1/2** file 2002+.

**Wave 4 — big subsystems:**
9. **M2-DATA-5** pointers → **M2-PROC-5** ALLOCATE/FREE.
10. **M2-PROC-4** EC/exception framework → RAISE/RESUME/USE.
11. **M2-PROC-3** VALIDATE · **M2-ARITH-2** standard arithmetic.
12. **M2-OO-1** Object-Oriented COBOL.

**Wave 5 — M3:** dynamic tables · TYPEDEF · JSON/XML.
**Wave 6 — M4:** remaining 2023 deltas + version-gating of intrinsics.

---

## 5. Production / commercial-grade axes (run alongside conformance)

A commercial compiler needs more than spec checkboxes. Track these in parallel:

- **Diagnostics quality** — comprehensive `CBL####` coverage, accurate source locations, actionable messages,
  exit codes; never crash on invalid input (P1 hardening already started — see memory `project_p1_diagnostics`).
- **Dialect strictness** — the two-axis model (version `--standard` × strictness); every leniency dialect-gated
  (see memory `project_dialect_strictness`).
- **Conformance corpus (BUILT — `tests/conformance/`)** — the NIST-equivalent suite for 2002/2014/2023. A
  file-based corpus (`<name>.cob` + `<name>.out`) per version, auto-discovered + run under the matching
  `--standard` by `ConformanceTests` (integration project), inside the guard. Seeded with M2 features (UDF
  invocation, conditional compilation, OPTIONS, REPOSITORY); **grow it with every feature** and backfill
  already-landed ones. Future: a per-version "% to spec" dashboard; optional negative tests (`.cob` + expected
  diagnostic) for dialect-rejection cases.
- **Performance** — parse/compile throughput on large programs; runtime efficiency of generated CIL.
- **Tooling & packaging** — CLI UX, `--help`, listing output, single-exe packaging, NuGet/runtime deployment.

---

## 6. POST-CONFORMANCE MILESTONES — GATED (do NOT start until an operational ISO 2023 compiler exists)

> Owner-stated; see memory `project_post_conformance_goals`. Do nothing here until M2→M4 conformance is achieved.

### 6.1 Full software architectural review
- Project **folder & file layout and naming** rationalized.
- **Class organization with proper isolation / single-responsibility — decompose any god classes.**
- Best software-design principles throughout; clear module boundaries.
- **Complete, accurate code documentation + code comments** across the source.
- Adopt the **latest C# language features**.

### 6.2 Project + executable rename
- Rename the project **`CobolSharp` → `COBOL.NET`** (rationale: it is COBOL for the .NET runtime — there is no
  "sharp" in it).
- **Produced executable MUST be named `cobol.exe`** (lowercase).
- Touch-points to plan for: `.csproj` `AssemblyName` + output exe name; the `.sln`; root namespaces
  (`CobolSharp.*`); `scripts/guard.sh` paths; `CLAUDE.md`/`README`/docs; the runtime assembly name; NuGet/package
  ids. Execute as one planned, guard-verified operation — not ad hoc.

---

## 7. Plan change-log
- **2026-06-05** — Created from the 15-area parallel conformance audit (workflow `iso2023-conformance-audit`) +
  session surveys. Initial catalog + waves. Established as SSOT.
- **2026-06-05 (session DEVLOG 372–382)** — Landed, each conformance-tested + ticked: M4-1 DELETE FILE; M4-2a
  XOR; M2-UDF-1/2 inline UDF invocation; M2-DATA-2 FLOAT-SHORT/LONG/EXTENDED; M2-DATA-1 BINARY-CHAR/SHORT/LONG/
  DOUBLE; M2-PROC-2 INSPECT BACKWARD; M2-PROC-1 INITIALIZE TO VALUE/DEFAULT/FILLER; M2-ARITH-1 ROUNDED MODE
  (all 8 + per-statement MODE IS + CORRESPONDING, ◐ two follow-ups left). Guard 1047/474/364. Next: M2-DATA-3
  National (§3.2). Process note: keep THIS document the singular live plan — tick items + log here as work lands;
  no separate resume/handoff docs.
- **2026-06-05 (session DEVLOG 383)** — **M2-DATA-3 National (UTF-16) data CORE COMPLETE** (conformance
  `tests/conformance/2002/national_data`, 13 assertions). `PIC N`/`USAGE NATIONAL` (2 bytes/char), `N"…"`
  literals, MOVE national←national, national↔alphanumeric + numeric→national conversion, literal/figurative
  VALUE, MOVE SPACE / INITIALIZE, DISPLAY, and national comparison — all char-aware. A 3-agent adversarial
  review caught that the initial slice silently corrupted national↔alpha MOVE, figurative/VALUE/INITIALIZE
  fill, and comparison; all fixed before commit (lesson: a narrowly-scoped passing conformance test is not
  evidence of completeness — verify the whole dispatch surface). Guard 1047/475/364. Next: M2-DATA-5 Pointers
  or M2-DATA-4 Boolean/bit (§3.2).
- **2026-06-05 (session DEVLOG 384–386)** — **M2-PRE-1 re-scoped** (384): an empirical repro refuted the
  "preprocessor crashes on valid source" claim (nothing crashes); the two real-but-rare defects + a spec caution
  are documented, item dropped to ◐. **M2-DATA-4 Boolean data CORE COMPLETE** (385 recipe, 386 implementation):
  `PIC 1`/`USAGE BIT`, `B"…"`, MOVE/VALUE/INITIALIZE/ZERO/DISPLAY/compare/JUSTIFIED; conformance `boolean_data`.
  A 2-agent adversarial review caught + fixed VALUE corruption (§13.18.63 GR10), a boolean-in-COMPUTE
  silent-numeric read, and JUSTIFIED wrongly rejected on boolean/national (the last also un-deadened national
  JUSTIFIED). Guard 1047/476/364. Next: M2-DATA-5 Pointers (needs the PointerRegistry design decision).
- **2026-06-05 (session DEVLOG 387)** — **M2-PROC-6 `GOBACK RETURNING`** (ISO §14.9.16, dialect-gated 2002+):
  lowers to a synthetic MOVE into the PROCEDURE DIVISION RETURNING item + `IrGoBack` (reuses CALL…RETURNING);
  conformance `goback_returning`. EXIT variants verified present/green; `CONTINUE AFTER` deferred. Guard
  1047/477/364. Still next: **M2-DATA-5 Pointers** (needs the PointerRegistry design decision — owner to confirm).
- **2026-06-05 (session DEVLOG 388–389)** — **M2-DATA-5 Pointers Phase-1 DONE** (388 recipe, 389 impl):
  `USAGE POINTER` (8-byte handle), `NULL`, `SET p TO NULL`/`SET p TO q`, `= NULL`/`= q`; pointer↔non-pointer MOVE
  + VALUE rejected; default INITIALIZE leaves pointers unchanged; conformance `pointer_data`. Lean reuse (SET→MOVE,
  NULL→0x00 figurative, compare via figurative/`IrStringCompare` — no new IR nodes). Self-review caught 3 bugs
  (`DataTypeSymbol.Category` not deriving Pointer → silent `SET p TO NULL` no-op; no-PIC pointer mis-classified
  `IsGroup`; VALUE-check ordering); the 2-agent adversarial review then confirmed clean (13 probes). Phase-1
  did NOT need the deferred design decision (NULL = 8 zero bytes, no targets to map). Guard 1047/478/364.
  **NEXT: the easy data items are done; remaining M2 is Phase-2 pointers (owner-gated), M2-PRE-1, OPTIONS
  arithmetic, file 2002, then the big subsystems (EC/exceptions, VALIDATE, OO).**
- **2026-06-05 (session DEVLOG 390–391)** — SORT Format-2 conformance backfill (390); **M2-ARITH-1 follow-up #1
  applied: OPTIONS `DEFAULT ROUNDED MODE`** (391) now sets the bare-ROUNDED default (token-scan in
  `VisitOptionsParagraph` → `SemanticModel.DefaultRoundingMode` → `BindRounded`); per-statement MODE still
  overrides; conformance `options_default_rounded`. Guard 1047/480/364. Follow-up #2 (PROHIBITED→EC) blocked on
  the EC framework. Remaining M2: Phase-2 pointers (owner-gated design decision), M2-PRE-1, file 2002, and the
  big subsystems (EC/exceptions, VALIDATE, OO, standard arithmetic).
