---
title: Lookup — IR (Bound Node) Mapping
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs
  - src/Cobol.Net.Compiler.SourceGen/BoundVisitorGenerator.cs
tags:
  - cobolsharp
  - spec
  - lookup
  - ir
---

# Lookup — IR (Bound Node) Mapping

Maps **every** bound-tree node (the compiler's only IR — see [[kb/IR/Node Types]]) to the ISO COBOL construct
it represents, the semantic rule that governs it, the compiler phase that produces/consumes it, and a diagram. This is
the **full per-node table (158 rows)** enumerated from `src/Cobol.Net.Compiler/Binding/Bound/*.cs`; the abstract base
records (`BoundExpr`, `BoundOperand`, `BoundBoolExpr`, `BoundCondition`, `BoundStatement`, `BoundSetTarget`,
`BoundPerformControl`) are omitted. Every node is built at **Bind** and rendered at **Emit**
([[kb/Compiler/Pipeline]]) unless the Phase column notes `DispatchEmitter` (control flow). The **Diagram**
column links either the per-node flows ([[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]]) or the structure
([[kb/Diagrams/IR Node Hierarchy]]).

> **Doc-comment fidelity:** the source's own citations for `BoundOpen` (comment says §14.9.25 = MOVE) and `BoundWrite`
> (comment says §14.9.46 = TERMINATE) are cross-wired in the code comments; the canonical §14.9.27 (OPEN) / §14.9.51
> (WRITE) are used here.

## Program structure

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundProgram` | Program unit: paragraphs, entry pc, declaratives | §14.2.3 (procedure division) | §14.2.3 GR1 (begins at first nondeclarative) | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundUnit` | One compilation-group program unit + attributes | §11.10 / §8.6.6 | binder invariant (containment/registry key) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundCompilation` | Immutable whole-group Binder result | — | binder invariant (emitter reads read-only) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundParagraph` | Paragraph: sentences at a pc index | §14.9.19 | §14.9.19 GR6 (sentence boundary = NEXT SENTENCE target) | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundMethod` | One class METHOD's pc range | §11.7 | exit-bounded dispatch range (fall-off = return) | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundDeclarative` | One USE declarative section + trigger scope | §14.9.49 | §14.9.49 GR3/GR7 (file/mode scope, handler exit) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundDebugSubject` | USE FOR DEBUGGING subject procedure | X3.23-1985 debug module | edition gate (85-only; VCR 7.17) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSequence` | Fixed pre/main/post statement-group carrier | — (bind-time desugar) | no pc identity; children render in line | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |

## Data movement (MOVE / SET)

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundMove` | MOVE source TO targets | §14.9.25 | §14.9.25.3 SR / MoveClassifier per-target kinds | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundCorresponding` | MOVE/ADD/SUBTRACT CORR → pairs | §14.7.6 | §14.7.6 identification-once; statement-level SIZE ERROR | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundInitialize` | INITIALIZE expanded to elementary MOVEs | §14.9.20 | §14.9.20 GR4 expansion (no runtime INITIALIZE) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetConditions` | SET condition-name(s) TO TRUE | §14.9.39 | store first VALUE into parent place | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetTo` | SET receivers TO value (index) | §14.9.39 Format 1 | GR2 sender determined once | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetUpDown` | SET index UP/DOWN BY amount | §14.9.39 Format 2 | GR3/GR4 amount once, per-index apply | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetCapacity` | SET dynamic-table capacity | §14.9.39 Format 14 | GR31 EC-FLOW-SEARCH if active (D9) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetSize` | SET SIZE OF dynamic-length item | §14.9.39 Format 16 | edition gate (SetDynLengthSize2023); GR37/38 clamp | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetSwitches` | SET external switches ON/OFF | §14.9.39 Format 3 | GR5 source-order switch modify | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |

## Arithmetic

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundAddTo` | ADD addends TO targets | §14.9.2 | §14.7.4 ROUNDED; receiver-scale store | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundAddGiving` | ADD addends GIVING targets | §14.9.2 | GIVING = write-only | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSubtractFrom` | SUBTRACT minuends FROM targets | §14.9.44 | §14.7.4 ROUNDED; in-place read+write | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSubtractGiving` | SUBTRACT FROM from GIVING targets | §14.9.44 | GIVING = write-only | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundMultiplyBy` | MULTIPLY a BY targets | §14.9.26 | §14.7.4 ROUNDED; in-place | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundMultiplyGiving` | MULTIPLY a BY b GIVING targets | §14.9.26 | GIVING = write-only | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundDivideInto` | DIVIDE divisor INTO targets | §14.9.12 | §14.7.4 ROUNDED; in-place | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundDivideGiving` | DIVIDE … GIVING targets | §14.9.12 | GIVING = write-only | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundDivideRemainder` | DIVIDE GIVING quotient REMAINDER | §14.9.12 Formats 4–5 | §14.9.12.4 GR7 truncated intermediate quotient | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundCompute` | COMPUTE targets = rhs | §14.9.8 | §14.7.4 ROUNDED; receiver-scale store | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundComputeBoolean` | COMPUTE boolean-targets = bool-expr | §14.9.8 Format 2 | §14.9.8 GR3 width; no ROUNDED/SIZE ERROR | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `Receiver` | Arithmetic resultant + rounding mode | §14.7.4 | no phrase→Truncation; ROUNDED→NearestAway | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `SizeErrorPhrase` | ON / NOT ON SIZE ERROR imperatives | §14.7.5 | absent → checked path not emitted | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## Conditions

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundRelational` | Relational comparison left op right | §8.8.4.2 | mapped C# operator; item↔item share renderer | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundLogical` | Logical AND/OR/XOR of conditions | §8.8.4 | short-circuit && / \|\| | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundNot` | Logical negation | §8.8.4 | binder invariant | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBooleanCondition` | Length-1 boolean expr as condition | §8.8.4.3 | GR1 true iff boolean 1 | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundCondition88` | Level-88 condition-name membership test | §8.8.4.5 | over resolved conditional-variable place | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundRangeMembership` | THRU-range membership (EVALUATE / EC-RANGE) | §14.7.8 | §14.7.8 rule 2 EC-RANGE-INVALID; inclusive bounds | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundSignCondition` | Sign POSITIVE/NEGATIVE/ZERO | §8.8.4.7 | Format-2 tests IEEE sign bit (GR2) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundClassCondition` | Class NUMERIC/ALPHABETIC[-U/-L] | §8.8.4.4 | ClassKind ∈ {N,A,U,L} | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundUserClassCondition` | User-defined class condition | §8.8.4.4 / §12.3.7 | members expanded at bind | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundUdfEvaluated` | Per-evaluation UDF activations wrapper | §8.8.4.13 | §8.4.3.2.4 GR6a per-evaluation (IIFE) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundSwitchCondition` | External switch-status condition | §8.8.4.6.2 | GR1 ON/OFF per condition-name | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## IF / EVALUATE

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundIf` | IF cond THEN … [ELSE …] | §14.9.19 | binder invariant | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundEvaluate` | EVALUATE chained selection | §14.9.13 | first-true arm; WHEN OTHER = else tail | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundEvaluateWhen` | One EVALUATE WHEN arm (composed match) | §14.9.13 | AND over subject↔object pairs | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |

## PERFORM

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundInlinePerform` | Inline PERFORM … END-PERFORM over body | §14.9.28 | real loop over bound body | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundOutOfLinePerform` | Out-of-line PERFORM p THRU q (pc range) | §14.9.28 | recursive bounded Dispatch(Start,End) | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `PerformOnce` | Run body once | §14.9.28 | control descriptor | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `PerformTimes` | Run body N times | §14.9.28 | count operand | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `PerformUntil` | Loop until (TEST BEFORE/AFTER) | §14.9.28 | before→while, after→do/while | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `PerformVarying` | Nested VARYING/AFTER induction loops | §14.9.28 Format 4 | GR12/GR13 re-evaluated per iteration | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `PerformForever` | PERFORM UNTIL EXIT infinite loop | §14.9.28.4 GR11 | edition gate (2023); while(true) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## GO TO / ALTER / EXIT / termination

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundGoTo` | GO TO — set program counter | §14.9.20 Format 1 | unconditional pc transfer | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundGoToDepending` | GO TO … DEPENDING ON selector | §14.9.20 Format 2 | out-of-range falls through | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundGoToAlterable` | GO TO in ALTER-target paragraph | §14.9.17 | control-flow D4 (mutable alter field) | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundAlter` | ALTER paragraph GO TO targets | §14.9.17 (deleted 2002) | edition gate (85-only); latest ALTER wins | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundAlterEntry` | One resolved ALTER entry | §14.9.17 | assign NewPc into alter field | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundExitParagraph` | EXIT PARAGRAPH → paragraph end | §14.9.14 | fall through to next | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundExitSection` | EXIT SECTION → section end | §14.9.14 Format 4 GR7 | explicit return when __exitPc == section end | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundExitPerform` | EXIT PERFORM [CYCLE] | §14.9.14 | break/continue nearest inline loop | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundExitProgram` | EXIT PROGRAM [RAISING] | §14.9.14 Format 2 | GR2 CONTINUE in main / GR3 return | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundNextSentence` | NEXT SENTENCE transfer | §14.9.19 GR6 | archaic (Annex F.1) | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundStop` | STOP RUN [WITH status] | §14.9.42 | exit code = status/ERROR 1/NORMAL 0 | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundStopLiteral` | STOP literal (operator message) | X3.23-1985 §14 Format 2 | edition gate (≥2002 rejected); stderr + continue | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundGoback` | GOBACK [RETURNING/RAISING/STATUS] | §14.9.18 | GR2 return / GR3 STOP-equivalent in main | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundContinueAfter` | CONTINUE AFTER n SECONDS (pause) | §14.9.9 | edition gate (2023); GR1b EC-CONTINUE-LESS-THAN-ZERO | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundNop` | No-op: bare EXIT / CONTINUE | §14.9.14 | binder invariant (no emission) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `TerminationStatus` | STOP/GOBACK termination status phrase | §14.9.42 / §14.9.18 | GR2/GR5 process exit code (§4.2.16) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## SEARCH

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundSearch` | Serial / binary SEARCH over table | §14.9.37 | GR5 first-true WHEN; FromStart = SEARCH ALL | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSearchWhen` | One SEARCH WHEN arm | §14.9.37.4 GR5 | source-order; first true ends search | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |

## String ops

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundStringStmt` | STRING sendings INTO [POINTER][OVERFLOW] | §14.9.43 | GR7 untouched portion preserved; GR8 range check | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundStringSending` | One STRING sender + delimiter | §14.9.43.2 | SR9 DELIMITED BY SIZE default (back-propagated) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundUnstringStmt` | UNSTRING source INTO receivers | §14.9.48 | delimiters, POINTER, TALLYING, OVERFLOW | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundUnstringDelimiter` | One UNSTRING delimiter [ALL] | §14.9.48.2 | GR7 ALL collapses contiguous | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundUnstringReceiver` | One UNSTRING receiver + DELIM/COUNT IN | §14.9.48.2 | GR11b no-delim examination size | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundInspect` | INSPECT tally/replace/convert | §14.9.22 | GR8a shared comparison-cycle; Backward gated | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundInspectTally` | One flattened TALLYING operand | §14.9.22.2 Format 1 | GR11 counts ADD into counter | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundInspectReplace` | One flattened REPLACING operand | §14.9.22.2 Format 2 | GR14 equal-length replacement (figurative pre-expanded) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundInspectConvert` | INSPECT CONVERTING from→to map | §14.9.22.2 Format 4 | GR20 positional map; SR9 To pre-sized | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## DISPLAY / ACCEPT

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundDisplay` | DISPLAY operands (UPON routing) | §14.9.11 | SR2/GR8 SYSERR→stderr else stdout | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundAccept` | ACCEPT identifier FROM device/temporal | §14.9.1 | GR1–4 device (not MOVE) vs GR6 temporal (MOVE) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |

## File I/O

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundOpen` | OPEN files with modes | §14.9.27 | 1:1 FileOpenMode; SHARING/RETRY overrides | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundClose` | CLOSE files [LOCK\|REEL/UNIT] | §14.9.7 | REEL/UNIT no-op on disk | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundRead` | Sequential READ [INTO][AT END] | §14.9.30 | status-first shape; AT END branch | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundWrite` | WRITE record [FROM][ADVANCING] | §14.9.51 | GR25 advancing; GR27/28 END-OF-PAGE (LINAGE) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundAdvancing` | WRITE ADVANCING phrase | §14.9.51 | BEFORE/AFTER, PAGE, n LINES | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundRewrite` | REWRITE record [FROM] | §14.9.35 | replace last-read record | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundUnlock` | UNLOCK file record locks | §14.9.47 | release connector locks (else status 42) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundKeyedRead` | Keyed READ (NEXT/PREV/random) | §14.9.30 | GR19 KeyIndex −1 prime / ≥0 alternate | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundKeyedWrite` | Keyed WRITE [INVALID KEY] | §14.9.51 | GR29–42 relative/indexed release | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundKeyedRewrite` | Keyed REWRITE [INVALID KEY] | §14.9.35 | GR18–25 key-match rules | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundKeyedDelete` | DELETE record [INVALID KEY] | §14.9.10 Format 1 | GR2–4 by prior READ / key; FPI unaffected | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundKeyedDeleteFile` | DELETE FILE (remove physical file) | §14.9.10 Format 2 | grammar gate {is2023()}; GR14 absent→'05' | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundKeyedStart` | START positioning (FIRST/LAST/KEY) | §14.9.41 | GR8/GR15 EQUAL default; SR3 NOT EQUAL rejected | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundLinageCounterRef` | LINAGE-COUNTER register read | §8.4.3.14 | GR7b read-only; runtime-sourced (SR2 bars receiving) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## SORT / MERGE

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundSort` | File SORT (release/sequence/return) | §14.9.40 Format 1 | GR9 three-phase; procedure = bounded dispatch | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundMerge` | k-way MERGE of pre-sorted files | §14.9.24 | GR4 stable; GR12 each GIVING gets full result | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundRelease` | RELEASE record to active sort | §14.9.32 | GR4 FROM ≡ MOVE then RELEASE; short images space-fill | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundReturn` | RETURN next sorted record [AT END] | §14.9.34 | GR3 key-order; GR15 restore varying length | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSortMergeKey` | One sort/merge key window | §14.9.40 GR1 | GR5/GR8 collating vs algebraic; SR6a fixed positions | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundTableSort` | In-place table SORT (typed array) | §14.9.40 Format 2 | typed-array compare (design §8.2); GR20/24 extent | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundTableSortKey` | One table-sort key member-path | §14.9.40 GR23 | empty path = element itself | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## Interprogram & pointers

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundCallProgram` | CALL program [USING/RETURNING][ON…] | §14.9.4 | SR2 static / GR3b dynamic; OVERFLOW spelling gated | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundCallArg` | One CALL USING argument | §14.9.4.4 GR5 | pass-mode transitivity applied at bind | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundCancel` | CANCEL program(s) | §14.9.5 | GR3 next CALL finds initial state; GR4 cascade | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundAllocate` | ALLOCATE storage (chars / BASED) | §14.9.3 | GR1 round-up; GR2 ≤0→NULL; GR6/7 init | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundFree` | FREE pointer(s) | §14.9.15 | GR1 three-way; GR2 left-to-right (EC-STORAGE-NOT-ALLOC) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetPointer` | SET data-pointer TO NULL/pointer/ADDRESS | §14.9.39 Format 4 | ManagedPointer carrier copy | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundAddressOf` | ADDRESS OF identifier as pointer value | §8.4.3.11 GR1 | BASED implicit ptr / cell offset; occurrence displacement | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundSetAddressOfBased` | SET ADDRESS OF based TO pointer | §14.9.39 Format 7 | SR18 receiver BASED; GR12/13 snapshot | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetProgramPointer` | SET program-pointer TO NULL/pointer | §14.9.39 Format 9 | SR21 both category program-pointer | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetEntry` | SET program-pointer TO ENTRY name | §14.9.39 Format 9 | GR4 miss→NULL + EC-PROGRAM-NOT-FOUND | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetPointerUpDown` | SET pointer UP/DOWN BY bytes | §14.9.39 Format 10 | GR18 NULL→EC-DATA-PTR-NULL (2002+) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |

## Object orientation

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundInvoke` | INVOKE method/factory/NEW (resolved) | §14.9.23 | GR5 null guard; virtual dispatch; form resolved at bind | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundInvokeArg` | One INVOKE USING argument | §14.9.23.4 GR6 | positional formal; BY REFERENCE write-back | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundInvokeUniversal` | Universal-receiver INVOKE | §14.9.23 | GR7c runtime conformance → EC-OO-UNIVERSAL | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundUniversalArg` | One universal-dispatch arg + descriptor | §14.9.23 | SR6 implicit BY REFERENCE (write-back) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundSetObjectRef` | SET object-reference assignment | §14.9.39 Format 5 | GR9/GR10; §9.3.8.2 typed-target narrow check | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundMethodReturn` | Method GOBACK / EXIT METHOD | §14.9.18.4 GR4 | loud-failure D8 (throw MethodReturn, not return) | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |

## Exceptions / EC

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundEcChecked` | Statement under enabled EC checking | §7.3.25.4 GR6 | TURN fold at bind (D10); wraps Inner with guards | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `EcStatementInfo` | Per-statement EC checking decision | §7.3.25.4 | enabled (ec,file) pairs; never empty | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundRaise` | RAISE EXCEPTION exception-name | §14.9.29 | §14.6.13.1 TURN-gated; fatal→loud (D8) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundRaiseObject` | RAISE exception object | §14.9.29 | §14.6.13.1.5; never fatal alone (GR2) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundResume` | RESUME AT next / procedure | §14.9.33 | SR3 nondeclarative pc; ResumeSignal unwind | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSetLastException` | SET LAST EXCEPTION TO OFF | §14.9.39 Format 13 | §14.6.13.1.1 clear last-exception status | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundRaising` | GOBACK / EXIT RAISING phrase | §14.9.18.2 / §14.9.14.2 | exactly one of EcName/IsLast/ObjectSource | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundExceptionPerform` | Exception-checking PERFORM interceptor | §14.9.28 Format 3 | GR17 tier dispatch; F3 (2023); inline imp-1/FINALLY | Bind→Emit · DispatchEmitter | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundExceptionMatch` | One F3-PERFORM WHEN selector + pc | §14.9.28.3 | GR17 tier-first match (file+L3→…→L1) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundWhenOperand` | One WHEN exception/file operand | §14.9.28.3 SR16 | EC-I-O name required when FILE given | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## Report Writer

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundInitiate` | INITIATE reports (reset/activate) | §14.9.21 | GR1/GR4 reset counters; GR5 written order | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundGenerate` | GENERATE detail / summary | §14.9.16 | GR1 detail / GR2 summary (null detail) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundSuppress` | SUPPRESS PRINTING (one instance) | §14.9.45 | GR1 lexical USE BEFORE REPORTING group; GR3 runtime | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundTerminate` | TERMINATE reports (final footings) | §14.9.46 | GR3 inactive; GR6 does not close file | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundReportCounterRef` | LINE-COUNTER / PAGE-COUNTER read | §8.4.3.15 | GR1–4 RWCS-maintained; SR3 bars receiving | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundReportSumRef` | Report SUM counter read | §13.18.54.4 GR4 | source item of printable entry (report-section only) | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundReportVaryingRef` | Report VARYING counter read | §13.18.64.4 GR3 | SR2 referable only within its entry | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## Operands / literals / boolean

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundStringLiteral` | Non-numeric literal (+ category) | §8.3.3.5 | one node for ALPHANUM/N"…"/B"…"; drives MOVE legality | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundNumericLiteral` | Numeric literal operand (raw text) | §8.3.3.3.2 | backend scales at render | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundFieldOperand` | Data-item reference operand | §8.4.1 | category decides string-vs-numeric render | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundComputedOperand` | Computed numeric expr as operand | §8.8.1 | wraps BoundExpr for comparison/args | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundFigurative` | Figurative constant operand | §8.3.3.6 | materialized to receiver/other-operand width | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundAllLiteral` | ALL 'literal' figurative operand | §8.3.3.6.4 Format 6 | GR2 repeated to width; SR5 digit-only→numeric MOVE | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolOperand` | Boolean expr as relation operand | §8.8.4.2.2 | rides shared BoundRelational renderer | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundNumLiteral` | Numeric literal (expr, raw text) | §8.3.3.3.2 | backend scales | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundNumRef` | Numeric data-item reference | §8.4.1 | resolved Place | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBinary` | Binary arithmetic + − × ÷ | §8.8.1 | scale tracked at render | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundNegate` | Arithmetic negation | §8.8.1 | binder invariant | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundPower` | Exponentiation base ** exp | §8.8.1 | binder invariant | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundIndexRef` | Index-name as 1-based occurrence | §13.18.38 | valid in SET/SEARCH/relation/subscript | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundIntrinsicCall` | Resolved intrinsic-function call | §15 | category-resolved; Collate under non-native PCS | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolLiteral` | Boolean literal B"1010" | §8.3.3.4 | decoded '0'/'1' bit string | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolRef` | Category-boolean item reference | §8.8.2 | includes static ref-mod | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolAll` | ALL B"…" positionless boolean | §8.3.3.6.4 GR2 | materializes to other operand's length | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolBinary` | Boolean B-AND / B-OR / B-XOR | §8.8.2 | rule 9 right-zero-extend, rule 10 length | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolNot` | Boolean negation B-NOT | §8.8.2 | rule 10 length preserved | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolShift` | Boolean shift / rotate | §8.8.2 rule 8 | edition gate (2023); result = operand length | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## Error nodes

| IR Node | Purpose | Related Spec Construct (ISO §) | Semantic Rule | Compiler Phase | Diagram |
|---|---|---|---|---|---|
| `BoundUnsupported` | Unsupported statement — loud guard | — | loud-failure §1.4 (never silent skip) | Bind→Emit | [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] |
| `BoundExprError` | Unresolved numeric expr — loud guard | — | loud-failure §1.4 | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundOperandError` | Unresolved operand — loud guard | — | loud-failure §1.4 | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundBoolError` | Unresolved boolean expr — loud guard | — | loud-failure §1.4 | Bind | [[kb/Diagrams/IR Node Hierarchy]] |
| `BoundConditionError` | Unresolved condition — loud guard | — | loud-failure §1.4 | Bind | [[kb/Diagrams/IR Node Hierarchy]] |

## Counts
**Total node rows: 158** (151 concrete `Bound*` types + the 5 `BoundPerformControl` leaves + `Receiver` + `SizeErrorPhrase`). Abstract base records (`BoundExpr`, `BoundOperand`, `BoundBoolExpr`, `BoundCondition`, `BoundStatement`, `BoundSetTarget`, `BoundPerformControl`) are omitted.

| Category | Rows | | Category | Rows |
|---|---|---|---|---|
| Program structure | 8 | | SORT/MERGE | 7 |
| Data movement (MOVE/SET) | 9 | | Interprogram & pointers | 11 |
| Arithmetic | 13 | | Object orientation | 6 |
| Conditions | 11 | | Exceptions/EC | 9 |
| IF/EVALUATE | 3 | | Report Writer | 7 |
| PERFORM | 7 | | Operands/literals/boolean | 20 |
| GO TO/ALTER/EXIT/termination | 15 | | Error nodes | 5 |
| SEARCH | 2 | | DISPLAY/ACCEPT | 2 |
| String ops | 9 | | File I/O | 14 |

## See also
- [[kb/IR/Node Types]] — the node model & the source-generated exhaustive visitor.
- [[kb/Spec/Lookup/Grammar]] — the constructs these nodes bind from.
- [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Spec/Lookup/Runtime Mapping]]
- [[kb/Runtime/Runtime-Class-to-IR]] — the reverse index (runtime class → nodes).
- [[kb/Diagrams/IR Graph Overview]] · [[kb/Diagrams/IR Node Hierarchy]]

- [[kb/Reference/Bound/_Index]] — the **generated** per-type reference (184 `Bound*` notes, drift-proof from the source `///` docs). This hand-curated table is the semantic/phase/runtime overlay; the generated notes are the authoritative per-type detail.

## Flow Diagrams
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — the visual companion: each node → ISO construct → semantic rule → runtime behavior.

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/IR/Node Types]] — link here.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Bind & Codegen phases.
