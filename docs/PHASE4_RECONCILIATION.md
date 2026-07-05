# Phase 4 Greenfield-vs-Catalog Reconciliation

> **STATUS: the AUTHORITATIVE greenfield-truth view of the M2/M3/M4 catalog** (DEVLOG 610, the ratified
> Phase-4 entry audit — `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 4). The catalog
> `docs/ISO2023_CONFORMANCE_PLAN.md` §3 carries LEGACY-era ☑/◐ marks; THIS table supersedes them with the
> per-item greenfield status verified against `src/Cobol.Net.*` by a 10-agent audit (no LANDED/NOT-STARTED
> mark overturned on cross-check). The single largest finding: the M2-DATA "done" marks are
> LEGACY-ONLY mirages — national/boolean/pointers/floats were implemented in the retired byte engine and
> stage LOUD (COBOLNET0899) in the greenfield; Phase 4 reclaims them. Keep this in sync as tracks land.


Audit of docs/ISO2023_CONFORMANCE_PLAN.md §3 (M2/M3/M4 catalog) against the live greenfield tree
(src/Cobol.Net.*). Cross-checked 8 LANDED + 3 NOT-STARTED (all NOT-STARTED rows in the set) + spot
checks on ALLOCATE / VALIDATE / PicInfo skeletons. **No LANDED or NOT-STARTED mark overturned.** Minor
corrections in the Corrections section.

Legend: LANDED = end-to-end greenfield + green golden; STAGED-LOUD = recognized, fails with a named
diagnostic (0899/0313/COBOLNET15xx or generic BoundUnsupported); PARTIAL = some legs land, some gap;
NOT-STARTED = no greenfield surface; OBSOLETE = superseded by a ratified decision.

## M2-UDF — user-defined functions

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-UDF-1 | Inline UDF invocation FUNCTION user-name(args) | done | **LANDED (DEVLOG 615)** | StatementBinder.Udf.cs (bind → hoisted CALL…RETURNING over a §8.4.3.2.4 GR1 result temp); 5 udf_* goldens ENABLED byte-exact (invocation, inline_expression, value_args, recursion, nested_args); UdfInvocationTests ×26; user-function-invocation-2002 registry+matrix row | none | As-built + adversarial-review notes below (the two-phase bind was REALIZED, not found). |
| M2-UDF-2 | Literal/arith args to a UDF | done | **LANDED (DEVLOG 615)** | §8.4.3.2.4 GR5b private-copy cells conformed by CobolArgAdapt; udf_value_args ENABLED byte-exact (LIT/ARI) | none | Folded into M2-UDF-1 as designed. |
| M2-UDF-3 | Separate-compilation function prototypes (§8.13) | open | NOT-STARTED | Repository-declared-but-undefined now stages LOUD at the dedicated COBOLNET1505 (was the generic 1501) | (c residual) | Depends on a cross-assembly function locate step (the EC-FUNCTION-NOT-FOUND §8.4.3.2.4 GR6b surface). |
| M2-UDF-4 | Bind REPOSITORY FUNCTION specifiers (ALL INTRINSIC / named) | open | PARTIAL | Named non-INTRINSIC specifiers now BIND (DataBinder.UserFunctionNames → the §12.3.8.2 GR12 dispatch + intrinsic shadowing); ALL INTRINSIC still inert; the FUNCTION-keyword-omitted reference form (§8.4.3.2 SR2) not in the grammar | (c residual) | GR12 named-specifier leg landed with UDF-1. |

### M2-UDF-1 — DECISION-COMPLETE DESIGN (recon workflow wf_a1e33856-215, 2026-07-05; ready to implement)

> Scope: the three whole-source corpus programs `udf_invocation` / `udf_inline_expression` / `udf_value_args`
> (each a `PROGRAM-ID` caller + a sibling `FUNCTION-ID DOUBLER` with `PROCEDURE DIVISION USING L-X RETURNING L-R`
> / `COMPUTE L-R = L-X * 2`), invoking `FUNCTION DOUBLER(WS-X)` / `DOUBLER(5)` / `DOUBLER(WS-A + 1)` in COMPUTE/MOVE.
> Goldens: `C=0042`/`M=0042`, `EXPR=0043`, `LIT=0010`/`ARI=0010`. **No grammar change** (functionCall,
> functionIdParagraph, `repositoryEntry : FUNCTION functionName INTRINSIC?` all already parse).

- **Spec (ISO 2023).** §9.4 a user function is a FUNCTION-ID unit that behaves like a program but RETURNs a value
  and is always RECURSIVE (independent activations). §12.3.8.2 the name must appear in the caller's REPOSITORY as
  `FUNCTION function-prototype-name` (no INTRINSIC) to resolve as a user function (GR12). §8.4.3.2.4 GR5 argument
  manner: an identifier valid as a receiving operand ⇒ **BY REFERENCE** (`DOUBLER(WS-X)`); a literal / arithmetic
  expression ⇒ **BY CONTENT** (`DOUBLER(5)`, `DOUBLER(WS-A+1)`) — a private copy, COMPUTE-conformed to the formal
  (§14.8.2.3.3 rule 2a; copy-in §14.2.3 GR9). §14.6.5 / §14.2.3 NOTE 1: the result is placed in a TEMPORARY item
  **allocated in the caller** whose description = the RETURNING linkage item. §8.4.3.2.3 SR1: a function-identifier
  is never a receiving operand.
- **KEY ENABLER — the bind is TWO-PHASE.** `CSharpEmitter.Call.cs:94` binds EVERY unit's DATA division
  (`CallBindUnit` → `data.Bind`) before ANY procedure body binds (`:327 binder.Bind`, after `MarkStoreAsImage`).
  So when a caller's PROCEDURE binds `FUNCTION DOUBLER`, the callee's `data.LinkageReturning` PicInfo + USING
  formals are ALREADY resolved — the forward reference (DOUBLER follows the caller in-file) is free, exactly as
  `OoClassTable` gives typed object refs their class defined-later (D1).
- **Registration.** (a) In `DataBinder.cs:131-135` (the repositoryEntry loop that today reads only PROPERTY), also
  collect `re.FUNCTION() is not null && re.INTRINSIC() is null ⇒ re.functionName().GetText()` into a new per-unit
  `UserFunctionNames` set (mirror `OoRepositoryProperties`, :77). (b) Thread the group `CallUnit` list (or a small
  `UserFunctionTable` name→CallUnit built in `CallCollectUnits`, like `OoClassTable`) into each `StatementBinder`
  (alongside `OoClasses`) so a call can find DOUBLER's CallUnit → its bound RETURNING DataItem/PicInfo + USING formals.
- **Dispatch.** `StatementBinder.Intrinsics.cs:55` — on `IntrinsicCatalog.TryGet` MISS, if the name is in the
  caller's `UserFunctionNames` AND the group table ⇒ bind a new `BoundUserFunctionCall`; else the existing
  COBOLNET1501. (One point serves both COMPUTE-expr `BindIntrinsic` and MOVE `IntrinsicOperand`.)
- **Lowering (reuse, no new emit path).** Synthesize a caller-side temp DataItem `__fnres_N` with the RETURNING
  PicInfo (a Roots-declared field, the property-ref synthesis pattern), bind a `BoundCallProgram` = CALL "DOUBLER"
  USING «args» RETURNING «temp» (args: identifier→Reference, literal/arith→Content over a temp of the formal's PIC —
  `BoundCallArg` already models all three modes), and HOIST it before the enclosing statement via the property-ref
  `BoundSequence` mechanism (`OoWrapPropertyOps`, DEVLOG 607); the expression/operand then reads «temp». Emission is
  the existing `CallEmitCall` → `ProgramRegistry.CallProgram(...)`; FUNCTION-ID units already emit as callable
  `_PRG_DOUBLER` with the RETURNING carrier.
- **EXACT SEAMS (all confirmed by reading — turn-key).** (1) Group table: build a `name→CallUnit` (or
  `name→{ReturningItem, UsingFormals}`) map AFTER the DATA-bind loop `CSharpEmitter.Call.cs:94`
  (`foreach unit CallBindUnit`) and BEFORE procedure binding (`:327`); each function unit's RETURNING item is
  `data.LinkageReturning` and its USING formals live in the DataBinder linkage (DataBinder.Linkage.cs). Thread it
  into each `StatementBinder` like `OoClasses` (constructed at `:325`). (2) Temp synthesis: mirror
  `DataBinder.OoCreatePropertyTemp` (DataBinder.Oo.cs:254 — synthesizes a level-1 elementary root DataItem from a
  model) to make `__fnres_N` from the RETURNING item. (3) Hoisting: the chokepoint is `StatementBinder.cs:158-160`
  (`int mark = data.OoPendingPropertyOps.Count; core = BindStatementCore(s); core = OoWrapPropertyOps(core,mark)`).
  Add a parallel `data.PendingUdfCalls` list + a `UdfWrap(core, udfMark)` that prepends one `BoundCallProgram`
  per pending call → `BoundSequence` (UDF is ALWAYS a pre-op — a function-identifier is never receiving, §8.4.3.2.3
  SR1 — so no `BoundStores.StoreKindOf` polarity step, unlike property refs). (4) Args: reuse the CALL
  `BoundCallArg` construction (StatementBinder.Call.cs) — identifier→Reference, literal/arith→Content over a temp
  of the formal's PIC. (5) The bound value the expression/MOVE reads is a `BoundComputedOperand`/field operand over
  `__fnres_N`, carrying the RETURNING PicInfo for arithmetic typing. Nested-UDF / property-op-ordering within one
  statement is a documented follow-up (the corpus has neither).
- **Tests / registry / docs.** Flip `udf_invocation`/`udf_inline_expression`/`udf_value_args` pending→enabled
  (`tests/conformance/2002/manifest.json`); add a `ConstructRegistry` row `user-function-invocation-2002`
  (IntroducedIn 2002) + a version-matrix row per feedback_conformance_tests_per_feature; a dedicated
  `UdfInvocationTests` (no-arg/ref/content/nested-in-expr). Unblocks M2-UDF-2 (folds in) and EXIT FUNCTION (M2-PROC-6).

#### M2-UDF-1 — AS BUILT (DEVLOG 615, 2026-07-05; all three goldens byte-exact on first run)

Landed exactly on the design's lowering (temp + hoisted `BoundCallProgram` + `BoundSequence`; zero new emit
surface; no grammar change), with these deviations/realizations — recorded per the process rule:

1. **The "KEY ENABLER" was REALIZED, not found.** The design read `CSharpEmitter.Call.cs` as already two-phase;
   in truth `CallBindUnit` bound each unit's DATA **and** PROCEDURE per-unit sequentially, so a caller's
   procedure bind could not see a later FUNCTION-ID unit's signatures. The as-built SPLITS it:
   `CallBindUnitData` (DataBinder + GLOBAL/index injection + bridges + ReferenceResolver) loops over ALL units,
   then `CallBuildUserFunctionTable`, then `CallBindUnitProcedure` loops (StatementBinder + formal resolution).
   Relative order vs `MarkStoreAsImage` and class binding is unchanged; INV-1-STRONG + the full battery prove
   the restructure behavior-neutral.
2. **The table is `Dictionary<string, UserFunctionSignature>`** (`DataBinder.Linkage.cs` — Name, Returning
   DataItem, LinkageFormals), not name→CallUnit: `CallUnit` is emitter-private and the binder needs only the
   bound signature. FUNCTION-ID units get `Recursive = true` structurally (§9.4 :12529 — always recursive).
3. **GR12 dispatch precedence.** §12.3.8.2 GR12 (:14885) makes a REPOSITORY-declared name refer to the USER
   function "and not to an intrinsic function of the same name" (the spec's factorial example :43651) — so the
   user-function check runs BEFORE `IntrinsicCatalog.TryGet`, not on its miss as the design sketched.
   `DataBinder.UserFunctionNames` collects the non-INTRINSIC FUNCTION specifiers (the M2-UDF-4 named leg).
4. **The content-arg "temp of the formal's PIC" is realized by the runtime ABI, not a bind-time temp:**
   literal/arith args ride the existing `BoundCallArg` value forms; `CobolArgAdapt.Num/Text` conform the cell
   to the callee's profile (same-scale cells alias; a scale difference gets the rescaling truncation view) —
   observably the §14.2.3 GR9 copy-in. Header BY VALUE formals (GR5c) are not modeled (LinkageFormal carries
   no mode); follow-up with the program-CALL header modes.
5. **The pending-call list lives on StatementBinder** (`_udfPendingCalls`), not DataBinder: registration
   happens inside the binder itself (property ops needed DataBinder only because ReferenceResolver registers
   them). The UDF wrap is the INNER sequence at the BindStatement chokepoint (before `OoWrapPropertyOps`), so
   a property-reference argument's GET still precedes the activation consuming its temp.
6. **A per-iteration re-evaluation guard the design missed:** a once-hoisted activation cannot honor a
   PERFORM UNTIL/VARYING condition (or FROM/BY operand) or SEARCH WHEN condition, which re-evaluate per
   iteration (§14.9.28/§14.9.37) — COBOLNET1509 loud, never a stale-temp loop. Body statements are safe
   (they drain their own suffix).
7. **Temp synthesis generalized:** `OoCreatePropertyTemp` now delegates to the ONE `CreateCompilerTemp`
   (feedback_singular_pattern); the UDF result temp is `CreateCompilerTemp(returning, "__FNRES-", "__fnres", name)`.
8. **Diagnostics band:** 1501 (+ a GR12 hint when the group defines the FUNCTION-ID), 1505 declared-but-undefined
   (the M2-UDF-3 prototype gap, also class-unit references), 1506 arity (§14.8.2 positional; empty parens are a
   §8.4.3.2 parse error by design), 1507 function without PD RETURNING (§14.2 :23666, checked once per unit even
   uncalled), 1508 duplicate FUNCTION-ID, 1509 re-evaluation guard.
9. **Residue (named, staged):** function prototypes/cross-assembly locate (UDF-3 → EC-FUNCTION-NOT-FOUND
   surface), ALL INTRINSIC semantic bind + the FUNCTION-keyword-omitted reference form §8.4.3.2 SR2 (UDF-4),
   EXIT FUNCTION (M2-PROC-6 leg — now unblocked, needs the in-function placement flag), UDF references from
   class-unit methods, per-evaluation activation (1509), BY VALUE header formals, §14.8.2.3 static
   description-conformance for reference args (runtime-adapted today, the program-CALL posture).

#### M2-UDF-1 — the ADVERSARIAL REVIEW WAVE (same change set; 28 raw findings → 24 confirmed → fixed/staged/documented)

The 4-lens find→2-skeptic-verify workflow (wf_e38982d1-0d2) over the landed diff confirmed 24 findings
(4 refuted). Disposition, all in the same change set:

**Fixed:**
- **StoreAsImage clone desync (major).** `StoreAsImage` is still mutable while procedure bodies bind (a
  ref-mod store / non-digit figurative MOVE in the callee flips its RETURNING item AFTER a caller's temp
  cloned it) — the two activation-boundary sides then disagree on the carrier and `StoreReturn(long,string)`
  silently drops a non-digit image. Cured structurally: `CreateCompilerTemp` records every (temp, model)
  pair (`DataBinder.CompilerTempClones`) and the run-unit emitter re-syncs `StoreAsImage` after ALL
  procedure binds — property-reference temps ride the same cure.
- **EcWrap sequence transparency (major).** The family selection switched on the bound node's TYPE, so a
  hoisted-activation `BoundSequence` matched nothing and the carrying statement silently LOST its >>TURN
  checking (a hole the property-op sequence had since DEVLOG 607). `QueryFor` now recurses into sequence
  steps: the carrying statement keeps its families, each hoisted CALL contributes EC-PROGRAM, duplicates
  dedup.
- **ContainsNextSentence sequence transparency (major).** A NEXT SENTENCE inside a UDF/property-carrying
  statement was invisible to the label machinery — `BoundSequence` arm added.
- **§8.8.4.13 short-circuit + EVALUATE over-evaluation (major).** Rule 1 terminates a hierarchical level as
  soon as its truth value is determined; rule 2 evaluates functions "if and when the conditions containing
  them are evaluated" — a hoisted activation in a NON-FIRST AND/OR operand (XOR exempt) or in EVALUATE
  selection (whose subjects this backend's chained lowering re-renders per WHEN) would over-evaluate.
  Guarded loud: `UdfGuardConditionalOperand` at BindFlatSequence + `BoundEvaluate` in the 1509 shape check.
- **§12.3.4 GR1 configuration inheritance (major).** A contained program (which cannot own a CONFIGURATION
  SECTION, §12.3.3 SR1) now inherits the containers' `UserFunctionNames` — conforming references bound,
  the misleading add-a-REPOSITORY hint can no longer fire there. (The same inheritance for PROPERTY
  specifiers is a pre-existing hole, noted for the OO track.)
- **§8.4.6.6 self-name (major).** A function's OWN name is referable with no repository entry
  (self-recursion; §12.3.8 GR11 makes a present self-entry a no-op) — `UdfSelfName` threaded per unit;
  the udf_recursion golden (5! = 120 through five nested activations) proves it end-to-end.
- **Non-numeric RETURNING mis-carry (critical).** The result reads through `BoundNumRef`, whose category
  classifiers and relation rendering are numeric — an alphanumeric result would COMPARE numerically and a
  group RETURNING cloned a Pic-less undeclarable temp (Roslyn failure). STAGED LOUD as COBOLNET1510: only
  elementary fixed-point numeric RETURNING is implemented; the category-carrying result channel is the
  named follow-up.
- **Honest argument diagnostics (minor).** A ref-mod/figurative/unresolvable argument now reports ITS
  actual staged shape in COBOLNET1506, never a message claiming a legal form is illegal.
- **§8.4.3.2.4 mis-citation (minor).** Every "§8.4.3.2.6" citation corrected (the GR1 temp rule and GR2/GR5/
  GR6 all live under §8.4.3.2.4 General rules).
- **Coverage (majors on the tests lens):** the 0900 BINDER gate now has a caller-only 85 witness (the
  whole-source matrix row's 85 leg trips the {is2002()}? PD-header RETURNING parse hint first); 1508 and
  the SEARCH/VARYING/EVALUATE/short-circuit 1509 legs and 1510 both categories gained facts; runtime
  coverage grew two goldens — udf_recursion and udf_nested_args (sibling activations, UDF-in-UDF,
  intrinsic-in-UDF, and the GR5a BY REFERENCE argument mutation visible in the caller: A=0005).

**Documented deviations (deliberate, cited):**
- **§12.3.8 SR10 forward reference without a prototype.** SR10 admits a repository FUNCTION specifier only
  naming (a) a function PROTOTYPE in the group, (b) a definition specified PREVIOUSLY, or (c) an
  external-repository entry. The whole-source corpus places callers FIRST — strictly conforming spelling
  needs `IS PROTOTYPE`, which is M2-UDF-3 (NOT-STARTED). Accepting the in-group forward DEFINITION is a
  deliberate, documented leniency (GnuCOBOL-compatible); the SR10 ordering diagnostic lands WITH prototypes,
  when the conforming spelling becomes available.
- **§14.6.2.3.2/.3 static-data state under the recursive model.** Functions ride the pre-existing D3/D4
  registry model (`Initial || Recursive` ⇒ fresh instance per activation), so a function's WORKING-STORAGE
  re-initializes per activation, where the spec keeps static data last-used after the FIRST activation.
  This deviation PREDATES UDFs (any PROGRAM-ID … RECURSIVE unit has it) and is observable only when WS
  carries state across activations; the conforming split (shared static WS + per-activation
  automatic/LOCAL-STORAGE/formals) is a named follow-up on the interprogram track.

## M2-DATA — new data types

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-DATA-1 | USAGE BINARY-CHAR/SHORT/LONG/DOUBLE [SIGNED\|UNSIGNED] | done | **LANDED (DEVLOG 614)** | PICTURE-less native 1/2/4/8-byte two's-complement integers (SIGNED default / UNSIGNED widens) on the COMP-5 BinaryCapacity discipline: `PicInfo.BinaryItem` + `Usage.BinaryChar/Short/Long/Double` un-skeletoned; `CobolNum.WrapBinary`/`InBinaryRange` implement the byte-width wrap + SIZE-ERROR range check (was a documented stub); `binary_usage.cob` ENABLED byte-exact; PICTURE prohibited COBOLNET0870 (§13.16.3 SR8); +24 BinaryCapacityTests unit + BinaryUsageDataTests end-to-end + DataSkeleton/LoudGuard/VersionMatrix flips | none | Implied DISPLAY width 3/5/10/19·20 (GR21 implementor choice); 0900 below 2002. |
| M2-DATA-2 | USAGE FLOAT-SHORT/LONG/EXTENDED | done | STAGED-LOUD | PicInfo.cs:75-81 skeleton → 0899; ConstructDialectStatus 114-115 "Phase 6"; float_usage.cob PENDING | phase 6 | IEEE-float families deferred to Phase 6 (D16). |
| M2-DATA-3 | National data — USAGE NATIONAL + PIC N | done | STAGED-LOUD | PicInfo.cs:317/328/467 → 0899; national_data.cob PENDING | (a) | Both N symbol + USAGE NATIONAL reject loud. |
| M2-DATA-4 | Boolean & bit — USAGE BIT + PIC 1 | done | STAGED-LOUD | PicInfo.cs:318/329/468 → 0899; boolean_data.cob PENDING | (a) | Boolean OPERATORS (B-AND/OR/XOR/NOT) also absent; (a) adds them. |
| M2-DATA-5 | Pointers & based addressing (POINTER/NULL/SET/BASED/ADDRESS OF) | partial | **PARTIAL (increment 1 LANDED, DEVLOG 613)** | USAGE POINTER data + SET TO NULL/pointer + [NOT] EQUAL on the ManagedPointer carrier (PicInfo.Pointer, BoundSetPointer, ManagedPointer.SameTarget; pointer_data.cob ENABLED, +5 PointerDataTests, 0869 band); ADDRESS OF / BASED / SET ADDRESS OF / ALLOCATE-FREE still staged (based_pointer/pointer_alloc/pointer_arith PENDING) | (b increment 2+) | The carrier + data model are now LIVE; increment 2 adds real addresses (ADDRESS OF → byte-backing) + BASED rebasing + ALLOCATE. |

## M2-ARITH — arithmetic

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-ARITH-1 | ROUNDED MODE (8) + DEFAULT ROUNDED + PROHIBITED→EC-SIZE-TRUNCATION | partial | **LANDED (DEVLOG 611, track e)** | 8 modes + DEFAULT ROUNDED + the PROHIBITED-inexact EDITED-receiver cure (CobolNum.RescaleChecked; the leak was the edited-store path calling plain Rescale — silent truncation — while the numeric path used TryStore); rounded_mode_prohibited.cob (2014) ENABLED; OnSizeErrorDifferentialTests +4 (COMPUTE/ADD-GIVING/DIVIDE + exact) | none | The fix is in the ONE shared StoreArith edited branch, so it holds across every arithmetic verb. |
| M2-ARITH-2 | Remaining OPTIONS clauses (ARITHMETIC/INTERMEDIATE/FLOAT DEFAULT/ENTRY-CONVENTION) | partial | **PARTIAL→(STANDARD landed, DEVLOG 611)** | STANDARD-DECIMAL + plain STANDARD both route to the CobolDec engine for fixed-point (§8.8.1.2/§8.8.1.4 — identical there; STANDARD removed at 2023 keeps 0807); options_paragraph.cob (2014) ENABLED as a real behavior test (2/7*7 = 2.00000 vs native 1.99997). Residual: FLOAT DEFAULT waits on standard-float types (phase 6); ENTRY-CONVENTION parsed-inert; STANDARD-BINARY documented-unsupported 0806 | (e residual → phase 6) | The float-operand divergence rides the deferred float families. |

## M2-PROC — procedural statements

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-PROC-1 | INITIALIZE …TO VALUE/DEFAULT/WITH FILLER | done | LANDED | **Verified:** StatementBinder.Initialize.cs + CSharpEmitter.Initialize.cs; initialize_phrases.cob ENABLED | none | Re-implemented greenfield; pre-85 phrases gated 0830-0835. |
| M2-PROC-2 | INSPECT …BACKWARD | done | LANDED | **Verified:** StatementBinder.Inspect.cs:63-65 Backward flag, gated 2023 via COBOLNET0845; inspect_backward.cob ENABLED (2023 manifest) | none | Gated 2023 (not 2002); ISO annex E.3.3 #34. |
| M2-PROC-3 | VALIDATE statement + validation clauses | open | OBSOLETE | **Verified:** zero VALIDATE grammar hits; council decision-3 = documented non-support is conformance-legal (F.2 #5 obsolete) | phase 7 | By design. Residual = a flag-obsolete WARNING row (Phase 7). Currently bare parse error. |
| M2-PROC-4 | Exception handling (RAISE/EC/>>TURN/RESUME/USE AFTER EXCEPTION) | open | LANDED | **Verified:** StatementBinder.Exceptions.cs, TurnState.cs, CSharpEmitter.Exceptions.cs; oo_ec_* ENABLED; DEVLOG 577 | none | Catalog stale-open; landed post-catalog. -N EC twins ride Phase 4(a). |
| M2-PROC-5 | ALLOCATE / FREE (based storage) | open | STAGED-LOUD | **Verified:** grammar wired CobolParserCore.g4:660-661 but NO binder arm → generic BoundUnsupported fall-through (StatementBinder.cs has no allocate/free case); pointer_alloc.cob PENDING | (b) | Blocked on pointer subsystem (M2-DATA-5). Generic loud, not a dedicated 0899. |
| M2-PROC-6 | GOBACK RETURNING (done); EXIT variants; CONTINUE AFTER (deferred) | partial | LANDED | **Verified:** BoundGoback ReturningSource+Raising (StatementBinder.Call.cs), CallEmitGoback; goback_returning.cob ENABLED | phase 7 | Core landed. EXIT SECTION/FUNCTION → BoundUnsupported (FUNCTION leg = Phase 4c). CONTINUE AFTER not in grammar → Phase 7. |

## M2-PRE — preprocessor

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-PRE-1 | Preprocessor robustness trio (SOURCE FORMAT switch / CC-in-copy / unknown >>) | partial | PARTIAL | Leg (c) landed (ConditionalCompilationProcessor.cs:126-137); (a) first-wins SOURCE FORMAT only; (b) CC-then-COPY order bug (Frontend.cs:88-93) | none | Low-severity follow-ups (WS-2002-FORMAT). (b) = stage-order swap; (a) = per-line format state. |
| M2-PRE-2 | Directive semantics depth (>>TURN toggle / recognize-ignore / >>DEFINE AS PARAMETER / >>CALL-CONVENTION) | open | PARTIAL | >>TURN EC-toggle LANDED (TurnState/StatementBinder.Exceptions); recognize-and-ignore LANDED (KnownIgnoredDirectives); >>DEFINE AS PARAMETER Deferred; >>CALL-CONVENTION inert | none | Load-bearing legs landed. Residual low-value, no named track. |

## M2-FILE — file I/O

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-FILE-1 | SHARING, LOCK MODE/LOCK ON, RETRY | open | NOT-STARTED | **Verified:** no SHARING/LOCK MODE/RETRY in CobolIO.g4 fileControlClauses/openMode; reserved-word rows only → generic COBOLNET0001 parse error | (d) | Runtime has CobolFile.Locked primitive to build on. |
| M2-FILE-2 | Line-sequential org + 2002 FILE STATUS codes | open | LANDED | **Verified:** DataBinder.cs:496 MapOrganization, SequentialFile.cs:161/273/276 WriteLine/ReadLine; FileIoDifferentialTests:175 passes | none | Named deliverable landed. Narrow status codes (04/39) + sharing-tied (9x) ride Phase 4(d). |

## M2-OO — object orientation (parent M2-OO-1 + sub-features a–h)

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-OO-1 | OO COBOL umbrella (CLASS/METHOD/INVOKE/INHERITS/FACTORY/PROPERTY/INTERFACE/universal/EC-OO) | partial | PARTIAL | 24 oo_* goldens ENABLED (verified on disk — corrected from 25); DEVLOG 600-609. Bulk landed, residue = 1h | none (residue = Phase 3 port) | Catalog ◐ is stale-low. |
| M2-OO-1a | Instance data + typed params + INHERITS + INVOKE SELF/SUPER + multi-method | remain | LANDED | oo_instance_data/method_args/self/inherit/super/object_group ENABLED; DEVLOG 601-603 | none | 45-agent sweep fixed 22 bugs. |
| M2-OO-1b | FACTORY (static) methods and data | remain | LANDED | oo_factory ENABLED; DEVLOG 604 | none | FACTORY OF/ACTIVE-CLASS RAISING → 1h. |
| M2-OO-1c | OVERRIDE / IS FINAL | remain | LANDED | oo_override_final ENABLED; DEVLOG 605 | none | |
| M2-OO-1d | INTERFACE-ID + IMPLEMENTS | post-slice-6 | LANDED | oo_interface / oo_interface_covariant ENABLED; DEVLOG 606 | none | |
| M2-OO-1e | PROPERTY (GET/SET) decl + refs | remain | LANDED | oo_property{,_ref,_explicit_ref,_factory_ref,_methods} ENABLED; DEVLOG 606-607 | none | Pinned __GET_/__SET_ accessors. |
| M2-OO-1f | Universal object ref + __CobolInvoke dispatch | remain | LANDED | oo_universal{,_inherit,_name,_relation} ENABLED; DEVLOG 608 | none | |
| M2-OO-1g | EC-OO-* model (RAISE id/EXCEPTION-OBJECT/USE F4/RAISING via INVOKE) | remain | LANDED | oo_ec_raise_object / oo_ec_goback_raising ENABLED; DEVLOG 609; conformance 1592/1592 | none | |
| M2-OO-1h | OO residue (method own ENV/FILE/SCREEN, REDEFINES/ODO/RENAMES/INDEXED in method data, PROPAGATE ON, FACTORY-OF/ACTIVE-CLASS RAISING, object VIEWS, STOP…RAISING) | implied ◐ | STAGED-LOUD | DataBinder.Oo.cs:93-101/115-116/352-369 COBOLNET0899; DEVLOG 609 residue list | none (Phase 3 port residue) | Each fails loud naming owning phase. Some are 2014/2023 surface. |

## M3 — COBOL-2014

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M3-1 | OCCURS DYNAMIC [CAPACITY IN][FROM…TO] | open | NOT-STARTED | **Verified:** zero CAPACITY/OCCURS DYNAMIC grammar hits; only ACCESS MODE DYNAMIC exists; 2014 corpus seeded empty | phase 6 | Phase 6 serial spine (deep-dive first). |
| M3-2 | TYPEDEF / SAME AS / TYPE TO | open | PARTIAL | TYPE IS reference clause parses (CobolData.g4:253-255) but no bind; TYPEDEF/SAME AS/TYPE TO don't parse | phase 6 | PARTIAL only in the weak sense (TYPE-IS tokenizes). Feature unimplemented end-to-end. |
| M3-3 | JSON & XML GENERATE/PARSE + XML-CODE/JSON-CODE | open | STAGED-LOUD | Seam grammar only; COBOL0313 vendor diagnostic (EditionGateHints.cs:72-74) | phase 6 | Owner decision-2: VENDOR extensions, zero ISO hits. ISO framing OBSOLETE; statements staged loud. |
| M3-4 | File lock finalization / function-method pointers / IEEE-754 / limits / cond-expr | open | PARTIAL | CLOSE…WITH LOCK binds + 2023-removal gated; OPEN SHARING/record LOCK/RETRY/UNLOCK reserved-only; FUNCTION/PROGRAM-POINTER reserved-only | d (+ b, 6/7) | Bundle: (d) sharing/lock; (b) USAGE FUNCTION/PROGRAM-POINTER; IEEE/limits/cond-expr → 6/7. "method pointers" wording wrong (0 spec hits). |

## M4 — COBOL-2023

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M4-1 | DELETE FILE execution (§14.9.10 Fmt 2) | done | **LANDED (DEVLOG 612, track d)** | Keyed + SEQUENTIAL legs LANDED (CobolFile.DeleteFile handles all organizations via SequentialFile.HostPath); delete_file + delete_file_absent (2023) ENABLED (GR13 round-trip 35 + GR14 absent 05) | (d residual) | Single file-name only; the multiple-file-name form (`DELETE FILE f1 f2…`) needs the fileName+ grammar — a documented follow-up. |
| M4-2a | logical XOR / EXCLUSIVE-OR | done | LANDED | **Verified:** StatementBinder.cs:1144 BindFlatSequence(xorExpr,"^"); logical_xor.cob+.out ENABLED; re-editioned 2023 (DEVLOG 596) | none | Parse+bind+emit(^)+green golden. |
| M4-2b | SMALLEST-ALGEBRAIC + EXCEPTION-FILE-N intrinsics | open | STAGED-LOUD | IntrinsicCatalog.cs:178/133 Deferred; IntrinsicRenderer.cs:44 LoudValue | phase 5 (+ a) | Gated+arity-checked+loud. EXCEPTION-FILE-N also blocked on national (a). |
| M4-3 | Other 2023 intrinsic/bit/boolean + dynamic-table finalization | open | STAGED-LOUD | IntrinsicCatalog.cs:172-179 (BASECONVERT/CONCAT/CONVERT/FIND-STRING/MODULE-NAME/SMALLEST-ALGEBRAIC/SUBSTITUTE) Deferred+2023-gated+loud | phase 5 (+ a, 6) | Umbrella. Intrinsics→5; boolean/bit→4(a) not-started; dynamic-table→6 (overlaps M3-1). |

## Wave sizing (remaining work per track)

Excludes 13 LANDED and 1 OBSOLETE-by-design (M2-PROC-3, warning-row only). 24 rows carry remaining work.

| Track | Owns (remaining) | Count | Notes |
|---|---|---|---|
| **(a) national/boolean** | M2-DATA-3, M2-DATA-4; + boolean/bit leg of M4-3; + EC-OO -N twins (from OO-1h), EXCEPTION-FILE-N national leg (M4-2b) | 2 primary (+3 shared legs) | National runtime + boolean ops. Unblocks several -N/EC-N legs. |
| **(b) pointers/ALLOCATE/BASED** | M2-DATA-5, M2-PROC-5; + USAGE FUNCTION/PROGRAM-POINTER leg of M3-4 | 2 primary (+1 shared) | ManagedPointer carrier is the spine; ALLOCATE binder blocked here. |
| **(c) UDF/prototypes** | ~~M2-UDF-1, M2-UDF-2~~ **LANDED (DEVLOG 615)**; M2-UDF-3, M2-UDF-4 (ALL INTRINSIC + keyword-omitted legs); + EXIT FUNCTION leg of M2-PROC-6 (now unblocked); + >>CALL-CONVENTION (loose) | 2 primary (+1 leg) | UDF invocation live in-group; residue = prototypes/cross-assembly + the UDF-4 legs. |
| **(d) file sharing/lock/retry** | M2-FILE-1, M4-1 (sequential leg); + file-lock leg of M3-4; + narrow status codes of M2-FILE-2 | 2 primary (+2 legs) | Runtime CobolFile.Locked primitive exists. |
| **(e) arithmetic** | M2-ARITH-1 (PROHIBITED move-COMPUTE fix), M2-ARITH-2 (golden rebaseline + inert legs) | 2 (both small) | Effectively bugfix + rebaseline, not new features. |
| **(f)** | — | 0 | No catalog item maps to (f). |
| **(g)** | — | 0 | No catalog item maps to (g). |
| **Phase 4 misc (unlettered)** | ~~M2-DATA-1 (BINARY-CHAR family numeric-usage)~~ **LANDED (DEVLOG 614)** | 0 | Native fixed-width binary integers + the BinaryCapacity wrap (also cures COMP-5's stubbed overflow). |
| **Phase 3 OO port residue** | M2-OO-1h (0899-staged edges) | 1 | Not an (a)-(g) track; owned by the OO port. |
| **Phase 5 (intrinsics)** | M4-2b, M4-3 (intrinsic slice: BASECONVERT/CONCAT/CONVERT/FIND-STRING/MODULE-NAME/SMALLEST-ALGEBRAIC/SUBSTITUTE) | 2 | Catalogued+gated+loud; runtime bodies remain. |
| **Phase 6** | M2-DATA-2 (IEEE floats), M3-1 (OCCURS DYNAMIC), M3-2 (TYPEDEF/SAME AS/TYPE TO), M3-3 (JSON/XML vendor), + dynamic-table leg of M4-3 | 4 primary (+1 leg) | OCCURS DYNAMIC deep-dive is the serial spine. |
| **Phase 7** | M2-PROC-3 (VALIDATE warning row), M2-PROC-6 (CONTINUE AFTER remnant); + M2-PRE-1, M2-PRE-2 (preprocessor robustness, no lettered track) | 2 primary (+2 preproc) | Disposition sweep / low-severity follow-ups. |

## Corrections (claims adjusted or clarified)

1. **M2-OO-1 golden count: 25 → 24.** The audit row says "25 oo_* goldens ENABLED"; disk truth is 24
   oo_*.out files and 24 distinct oo_* names in the 2002 manifest. Sub-feature evidence (1a-1g) is
   otherwise accurate. Correction is cosmetic — does not change the LANDED verdict for any sub-item.
2. **M2-PROC-2 diagnostic code named.** Audit left the code unstated; the greenfield gate is
   `COBOLNET0845` (StatementBinder.Inspect.cs:65), gated to 2023 (not 2002). Confirms the row's own
   2023-framing note over the catalog's 2002 framing. No status change.
3. **No status marks overturned.** All 8 spot-checked LANDED (INITIALIZE, INSPECT BACKWARD, Exception
   handling, GOBACK RETURNING, line-sequential, OO-1a/e/g representative, XOR) verified genuinely
   end-to-end with green goldens. All 3 NOT-STARTED (UDF-3 prototypes, OCCURS DYNAMIC, SHARING/LOCK/RETRY)
   verified as zero greenfield surface. Extra checks (M2-PROC-5 ALLOCATE falls to generic
   BoundUnsupported; M2-PROC-3 VALIDATE zero grammar; PicInfo 19 skeleton-usage hits for DATA-1..5)
   all match the audit. The audit rows are trustworthy for wave planning.
