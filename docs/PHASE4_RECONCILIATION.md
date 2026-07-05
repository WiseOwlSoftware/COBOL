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
| M2-DATA-5 | Pointers & based addressing (POINTER/NULL/SET/BASED/ADDRESS OF) | partial | **LANDED (increments 1+2, DEVLOG 613/617)** | Increment 1: USAGE POINTER + SET TO NULL/pointer + [NOT] EQUAL. Increment 2: ADDRESS OF / BASED / SET ADDRESS OF / F10 arithmetic on the StorageCell+CellPointer window model (structural §8.8.4.2 equality; the deref bridge makes §13.18.5 GR3/GR4 loud); based_pointer/pointer_alloc/pointer_arith ENABLED byte-exact; 6 negative cases; CobolPtrTests ×6; 0869/0881 bands; set-address-2002 + pointer-arithmetic-2002 rows | none (residue named in the as-built) | CALL-boundary pointers, restricted `POINTER TO`, FUNCTION/PROGRAM-POINTER stay staged (M3-4 leg). |

### M2-DATA-5 / M2-PROC-5 — increment 2 (ADDRESS OF / BASED / SET ADDRESS OF / ALLOCATE-FREE) — DECISION-COMPLETE DESIGN (recon wave 2026-07-05; ready to implement)

> Scope: the three PENDING corpus programs (tests/conformance/2002/manifest.json:44-49) — `based_pointer`
> (`SET P TO ADDRESS OF X` / `SET ADDRESS OF B TO P` / read+write aliasing through an elementary BASED item;
> golden `B=HELLO`,`X=WORLD`), `pointer_alloc` (`ALLOCATE B` form-2 / `ALLOCATE 5 CHARACTERS RETURNING P`
> form-1 / rebase / `FREE P`; golden `B=HELLO`,`B2=WORLD`,`FREED=YES`), `pointer_arith` (`SET P UP BY 4` /
> `DOWN BY 2` byte-granular + a 1-byte BASED probe; golden `C0=A`,`C4=E`,`C2=C`). **No .g4 change**: both
> setAddressStatement alternatives (CobolParserCore.g4:1039-1042), allocate/free (:1047-1055, dispatcher-gated
> `{is2002()}?` :660-661), `SET … UP/DOWN BY` via setIndexStatement (:1074-1076), and `basedClause`
> (Core/CobolData.g4:240-242) all parse TODAY; edition gates are binder-side registry checks (below).

- **Spec (ISO 2023; specs/ISO_COBOL.md :line anchors).** ADDRESS OF §8.4.3.11 (:7443-7491 — GR1 a unique
  data-pointer containing the item's address; SR1 FILE/WS/LS/LINKAGE items legal; SR5 never a receiving operand,
  but the SET receiving form is "syntactical notation", D.9.2.1 :44419). BASED §13.18.5 (:17701-17729 — GR1
  template + implicit data-address pointer; GR2 initial NULL — ⚠ standard erratum: GR2 cites "SET GR 17, 18, 19"
  but the 2023 numbering puts Format 7 at GR12-13 :31600-31604, cite both in code; GR3 deref-while-NULL →
  **EC-DATA-PTR-NULL** Fatal :24669; GR4 invalid address → **EC-BOUND-PTR** Fatal :24643); §13.16 SR16 (:17261)
  level 1/77 in WS/LS/LINKAGE only; no REDEFINES (:17215) / no EXTERNAL (:17219) with BASED; lifecycle §8.6.5
  (:8789-8801 — `ADDRESS OF based-item` READS the implicit pointer :8791). SET F7 §14.9.39 (:31172-31175; SR18
  :31399 **the ADDRESS OF receiver shall be BASED** — the IBM non-BASED-linkage idiom is NOT ISO; GR12-13
  :31602-31604 the address VALUE is assigned — snapshot, never live-tracking). SET F10 (:31194-31199 —
  **pointer arithmetic IS ISO 2023, not a vendor extension**; SR23-24 :31424-31426; GR18 NULL →
  EC-DATA-PTR-NULL :31631; GR19 non-integer → EC-SIZE-ADDRESS Fatal :31633; GR20 moves by **bytes**, outside
  the implementor's range → EC-RANGE-PTR :31635). ALLOCATE §14.9.3 (:25936-26008 — SR1 data-name-1 BASED
  :25959; SR2 CHARACTERS ⇒ RETURNING required :25961; SR3 RETURNING is category data-pointer :25963; GR1 bytes,
  non-integer rounds UP :25972; GR2 ≤0 → NULL, **no EC** :25974; GR3 based size incl. ODO max :25976; GR4a/b
  :25978-25990; GR5 EC-STORAGE-NOT-AVAIL NF :25992-25998; GR6 INITIALIZED+CHARACTERS = binary zeros :26000;
  GR7 INITIALIZED+based = `INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT` :26002; GR8/9 otherwise
  undefined / pointer+object subordinates null :26004-26006; GR10 lifetime :26008). FREE §14.9.15 (:27485-27513
  — SR1 operands are data-pointers ONLY :27500, `FREE based-item` is a vendor extension the corpus does NOT
  use; GR1a start-of-allocation → release + null THE OPERAND only :27505-27511; GR1b NULL → no-op; GR1c else →
  **EC-STORAGE-NOT-ALLOC** NF :24856; GR2 per-operand sequencing :27513). Pointer relations §8.8.4.2
  (:9591-9599 equality-only, no ordering; :9772-9774 "equal if they reference the same address" —
  **STRUCTURAL** equality over (storage, offset), not carrier-object identity).
- **GOLDEN-vs-ISO AUDIT — no re-editioning, no owner conflict.** All three goldens are ISO-conformant as
  written: pointer_arith is SET Format 10 (standard — clears the "pointer arithmetic may be vendor" flag);
  pointer_alloc FREEs the RETURNING pointer P (the ISO data-pointer form, and `FREED=YES` = GR1a's operand-null);
  based_pointer's read/write aliasing = F7 GR12-13 + one shared storage. pointer_arith's re-`SET ADDRESS OF`
  after each step is consistent with GR13 snapshot semantics (the design implements snapshot; the corpus cannot
  distinguish, the spec decides). Introduction edition: 2002 for all rows — filed per
  docs/ISO2023_CONFORMANCE_PLAN.md:205-214 and Annex E's silence (predates 2014, spec :49014ff); keep the
  existing `vcr: "2002 introduction (derive from the 2002 standard)"` caveat on new rows (in-repo texts cannot
  prove 2002-vs-2014; see OWNER-5).
- **The storage-model DECISION (D-P2): ONE run-unit cell type + a window pointer, generalizing what EXTERNAL
  already ships.** Promote `ExternalStore.Holder` (ProgramRegistry.cs:264-286) to a general `StorageCell`
  (`string Ref` + `bool Allocated` + `bool Freed`); add **`CellPointer(StorageCell Cell, long Offset)`** as the
  third concrete `ManagedPointer` beside `NullManagedPointer` and `ManagedPointer<T>` (ProgramRegistry.cs:69-127);
  `SameTarget` (ProgramRegistry.cs:80-81) gains the structural branch its own doc comment promises
  (:77-79 "when ADDRESS OF lands … it compares referenced storage"): window vs window ⇒
  `ReferenceEquals(Cell) && Offset ==`; anything vs the null carrier ⇒ existing both-null test; legacy
  `ManagedPointer<T>` closures ⇒ existing `ReferenceEquals` (cross-kind never-equal unless same instance —
  unreachable until the CALL-boundary residue lands). Justification: (a) **feedback_singular_pattern** — the
  job "shared, aliasable, re-basable character storage" already has ONE canonical mechanism, the Tier-B
  string-canonical backing aliased through a heap cell + `ref`-property (`CallMakeExternal`
  DataBinder.Linkage.cs:207-242; the emitted bridge CSharpEmitter.Call.cs:512-514); ADDRESS OF, BASED deref,
  and ALLOCATE are all instances of that job, and a second near-identical cell type is exactly what the rule
  forbids. (b) **the no-byte-substrate directive** — the cell holds the record's CHARACTER IMAGE string, the
  design-sanctioned Tier-B shape (`ExternalStore`'s own doc: "never a persisted byte substrate",
  ProgramRegistry.cs:258-263); no `byte[]` anywhere; byte offsets are character offsets in this model
  (byte=char holds for the alphanumeric/zoned world — see RESIDUE-11 for NATIONAL/bit). (c) **SSOT authority**
  — COBOLNET_DESIGN.md:627-628: the ONE carrier serves "USAGE POINTER, ADDRESS OF, BASED, ALLOCATE/FREE, SET
  ADDRESS OF". **Rejected:** `ManagedPointer<T>.OverField/Cell` closures as the pointer VALUE — they fail
  structural equality (§8.8.4.2.16), have no byte offset for F10 GR20 (pointer_arith unrealizable), and no
  window algebra for a 1-byte BASED view over a 10-byte buffer; OverField stays what it is: the CALL-ABI
  carrier. A PointerRegistry-style side table stays rejected (memory feedback_managed_pointers). The
  `Cell(T)`-doc's "also the ALLOCATE backing" claim (ProgramRegistry.cs:93,113-114) is SUPERSEDED — update the
  doc comment + COBOLNET_DESIGN §9.1 in the same change set (feedback_follow_design_docs_and_spec).
- **ADDRESS OF ⇒ byte-backing (the CallMakeExternal generalization).** A parse-tree pre-scan collects the
  `SET p TO ADDRESS OF x` operands (alt 2 of setAddressStatement — the ONLY ADDRESS OF surface in the grammar;
  alt 1's operand is the BASED receiver, never forced); a late hook `PtrBindAddressables(program)` runs beside
  `CallBindExternalAndGlobal` (DataBinder.cs:187 — the proven post-`ClassifyRedefinesClasses` overwrite seam,
  :183) and, for each resolved non-BASED target, forces the target's ROOT-01 class to Tier-B StringCanonical
  over a per-instance `StorageCell` field: factor `CallMakeExternal`'s body (DataBinder.Linkage.cs:207-242)
  into ONE `ForceStringCanonical(anchor, gate)` used by both callers — the external caller keeps its proven
  Display-only leaf gate THIS increment; the addressable caller uses the CLASSIFIER's current gate
  (Display + fixed-point BINARY/PACKED as zoned image, DataBinder.cs:966-990) since that is the canonical
  Tier-B acceptance rule (gate convergence = follow-up, see RESIDUE). Emission mirrors the EXTERNAL bridge
  (CSharpEmitter.Call.cs:512-514): `private readonly StorageCell _cell_X = new() { Ref = «CallInitialImage» };`
  + `private ref string _redef_X => ref _cell_X.Ref;` (initial image via the existing `CallInitialImage`,
  DataBinder.Linkage.cs:244; nested classes reach the cell through the same parent path `BackingPath` builds,
  ReferenceResolver.cs:326-333). All reads/writes then flow through the UNCHANGED Tier-B machinery
  (`PlaceForItem` → `RedefViewPlace`, ReferenceResolver.cs:230-253; Place.cs:57-77) — zero new verb code. The
  pointer value: `SET P TO ADDRESS OF X` ⇒ `P = ManagedPointer.At(_cell_X, «X.ClassOffset»)`; an
  EXTERNAL+ADDRESS-OF item needs no special case — after unification its cell IS `ExternalStore.Cell(...)`
  (same `StorageCell`), and `At` takes it directly. `SET p TO ADDRESS OF B` where B is BASED reads the implicit
  pointer (§8.6.5 :8791): `p = __addr_B`. Rejected-tier targets, OCCURS-resident anchors (`BackingPath` null),
  and carrier-resident LINKAGE formals (no local cell, DataBinder.Linkage.cs:122-129) stage LOUD (0869).
- **BASED ⇒ pointer-routed Tier-B views.** New `basedClause` arm in the `BindEntry` clause loop
  (DataBinder.cs:630-676 — today the clause is silently DROPPED and the item gets real private storage,
  silently wrong per §13.18.5): set `item.IsBased`, emit NO stored field, classify the entry (elementary or
  group) as a synthetic StringCanonical class whose backing is reached THROUGH the implicit pointer. Two
  emitted members per BASED 01/77: the implicit pointer field `private ManagedPointer __addr_B =
  ManagedPointer.Null;` (GR2 — the increment-1 `PicInfo.PointerItem` init machinery, PicInfo.cs:183-184/:251)
  and the deref bridge `private ref string __based_B => ref CobolPtr.Deref(__addr_B, «classWidth»).Ref;` where
  `Deref` throws `CobolFatalException("EC-DATA-PTR-NULL", …)` on the null carrier (GR3) and
  `CobolFatalException("EC-BOUND-PTR", …)` on a freed cell, a non-window carrier, or `Offset + classWidth >
  Cell.Ref.Length` (GR4 — the bounds check lives here because the bridge knows the class width). References
  build `RedefViewPlace(Backing: "__based_B", OffsetExpr: $"CobolPtr.OffsetOf(__addr_B) + {classOffset}", …)`
  — `OffsetExpr` is already an arbitrary runtime `long` expression (Place.cs:66-69), and both Read and Write
  render the Backing FIRST (Place.cs:69,:75-76), so `Deref` trips before `OffsetOf` (which is null-lenient).
  **No new Place subtype** — RedefViewPlace over a swappable ref-property IS the fit. `SET ADDRESS OF B TO P`
  ⇒ `__addr_B = «P.Read()»` (F7 GR13 snapshot). The active `based-clause-2002` matrix witness (a
  declaration-only BASED GROUP, constructs.json:88-95) keeps compiling: classification + fields emit fine and
  the bridge is never invoked. Declaration validation rides the new 0881 band (below); a VALUE clause on a
  BASED entry follows the LINKAGE posture (level-88 only — verify the exact §13.18.65 SR at implementation).
- **ALLOCATE / FREE.** New binder arms in the statement dispatch (StatementBinder.cs:171-226 — today both fall
  to the generic `BoundUnsupported` tail at :226). `BoundAllocate`: form-1 `ALLOCATE expr CHARACTERS
  [INITIALIZED] RETURNING p` ⇒ `«p» = CobolPtr.Allocate(«ceil(expr)», zeroFill: INITIALIZED)` (GR1 round-up;
  GR2 n≤0 ⇒ the helper returns `Null`, no EC; GR6 INITIALIZED ⇒ `'\0'`-fill — the faithful binary-zeros image
  in the character model; GR8 otherwise ⇒ space-fill, conformant under "undefined"); form-2 `ALLOCATE B
  [INITIALIZED] [RETURNING p]` ⇒ `__addr_B = CobolPtr.Allocate(«B.classWidth»)` (+ `p = __addr_B` per GR4a),
  with INITIALIZED lowered to a `BoundSequence` [allocate; `BoundInitialize(B, WithFiller, ToValue,
  ThenDefault)`] reusing the LANDED M2-PROC-1 machinery verbatim (GR7's exact wording); GR9's
  pointer/object-subordinate nulling is moot this increment (a pointer/object leaf fails the Tier-B gate ⇒ the
  BASED group stages loud). GR5 (EC-STORAGE-NOT-AVAIL) is documented-unreachable: allocation failure under .NET
  is a platform OOM (RESIDUE-7). `BoundFree` (one or more operands, each category data-pointer — 0869 otherwise
  per SR1 :27500): emit sequentially per GR2 as `«p.Write»(CobolPtr.Free(«p.Read()», out bool __notAlloc));`
  — the helper implements the GR1 three-way: window at Offset 0 over an `Allocated && !Freed` cell ⇒ mark
  `Freed`, clear `Ref` (GC reclaims; every dangling alias later hits the Deref `Freed` trap = the "contents
  become undefined" license made loud), return `Null`; null carrier ⇒ return unchanged (no-op); anything else
  ⇒ `__notAlloc = true`, return unchanged — the emitter appends the TurnState-gated
  `ExceptionState.Set("EC-STORAGE-NOT-ALLOC", fatal:false, …)` block ONLY when that EC's checking is turned
  (the DEVLOG 577 raise-site pattern, ExceptionState.cs:10-13), so unchecked NF behavior = continue, exactly GR1c.
- **SET Format 7 / Format 10 binding.** Replace the staged `BoundUnsupported` at StatementBinder.cs:727-728
  with `BindSetAddress` handling both grammar alternatives: alt 1 receiver must be BASED (SR18 :31399 — 0869
  otherwise; see OWNER-4 for the IBM idiom), sender must resolve to `PicCategory.Pointer` (SR17); alt 2 routes
  into the existing `BindSetPointer` (:752-789) with the new AddressOf source leg. `BindSetUpDown` (:819+, the
  85-form setIndexStatement) gains a POINTER ARM mirroring the D-U7 category re-route precedent (:799-804): if
  the first target resolves `PicCategory.Pointer` ⇒ all targets must be pointers (0869 on a mix, SR23) and the
  amount must be statically integer-typed (0869 on a fractional-scaled expression — the bind-time face of GR19;
  the runtime EC-SIZE-ADDRESS leg is RESIDUE-7) ⇒ `BoundSetPointerUpDown`; emit `«p» = CobolPtr.UpBy(«p», «±n»)`
  (GR18 null ⇒ EC-DATA-PTR-NULL throw; GR20's implementor data-pointer range is DECIDED unbounded — EC-RANGE-PTR
  is never raised at SET time and out-of-cell addressing surfaces at deref as EC-BOUND-PTR, a conformant
  implementor choice to record in the deep-dive).
- **EXACT SEAMS (all confirmed by reading — turn-key).** (1) Pre-scan + late hook: `PtrBindAddressables(program)`
  beside `CallBindExternalAndGlobal(program)` at DataBinder.cs:187 (post-`ClassifyRedefinesClasses` :183 —
  the proven tier-overwrite point); factor `ForceStringCanonical` out of `CallMakeExternal`
  (DataBinder.Linkage.cs:207-242). (2) basedClause arm: the clause loop DataBinder.cs:630-676; the
  PICTURE+POINTER rejection joins the 0812/0870 pattern block at DataBinder.cs:692-718. (3) Cell/bridge
  emission: beside the `CallExternalBackings` loop CSharpEmitter.Call.cs:512-514 (new `AddressableBackings` +
  `BasedBridges` lists on DataBinder). (4) Statement dispatch: new `allocateStatement()`/`freeStatement()`
  arms before the generic tail StatementBinder.cs:226. (5) SET F7: replace :727-728; extend `BindSetPointer`
  :752-789 (drop the ":783 ADDRESS OF senders are a later increment" message). (6) SET F10: the pointer arm at
  the top of `BindSetUpDown` :819+. (7) Emit arms: dispatch beside `case BoundSetPointer` CSharpEmitter.cs:433;
  new emitters next to `EmitSetPointer` :1198-1203. (8) Runtime: `StorageCell`/`CellPointer`/`SameTarget` in
  ProgramRegistry.cs:69-127+264-286 (the carrier family stays in ONE file); the `CobolPtr` static helpers may
  live in a new `Control/CobolPtr.cs` (helpers, not a second carrier). (9) Whole-group-image guard: reject a
  group image spanning a pointer leaf where `BuildPhysicals` walks widths (FieldEmitter.cs:80-112) — closing
  increment-1 gap #10 loud.
- **Bound-node additions** (Bound/BoundTree.cs, beside `BoundSetPointer` :346): `BoundAddressOf(DataItem Item)`
  (an operand-shaped record; the emitter derives cell path + ClassOffset, or reads `__addr_x` when Item is
  BASED); EXTEND `BoundSetPointer` with a nullable `BoundAddressOf Address` source leg (one node per job —
  never a parallel SET-pointer node); `BoundSetAddressOfBased(DataItem Based, Place Source)`;
  `BoundSetPointerUpDown(IReadOnlyList<Place> Targets, BoundExpression Amount, bool Up)`;
  `BoundAllocate(DataItem? Based, BoundExpression? Chars, bool Initialized, Place? Returning)`;
  `BoundFree(IReadOnlyList<Place> Operands)`.
- **Runtime additions** (namespace CobolNet.Runtime): `StorageCell` (Holder generalized — `ExternalStore.Cells`
  becomes `Dictionary<string, StorageCell>`; the generated `ref ….Ref` bridge shape is UNCHANGED, so IC-series
  baselines must stay byte-identical — guard-proved); `CellPointer` + `ManagedPointer.At(cell, offset)`;
  `SameTarget` structural branch; `CobolPtr.Deref(p, classWidth) → ref string`, `.OffsetOf(p) → long`,
  `.UpBy(p, n)`, `.Allocate(n, zeroFill=false)`, `.Free(p, out notAlloc)` — fatal ECs throw
  `CobolFatalException` (the documented "runtime raise points" channel + §14.6.13.1.3 #8 loud-failure doctrine,
  CobolFatalException.cs:5-13); NF ECs report via out-param for the emitter's TurnState-gated
  `ExceptionState.Set` block.
- **Diagnostics band plan.** (i) **0869 (existing pointer band — extend, message-differentiated):** SET ADDRESS
  receiver not BASED (SR18); F7 sender not a data-pointer (SR17); ADDRESS OF target unresolvable /
  Rejected-tier / OCCURS-resident / carrier-resident-formal (staged-loud wording); UP/DOWN pointer-mix or
  statically non-integer amount (SR23/GR19); ALLOCATE data-name not BASED (SR1), CHARACTERS without RETURNING
  (SR2 — the grammar leaves RETURNING optional, :1048), RETURNING not a pointer (SR3); FREE operand not a
  pointer (SR1 :27500); **the SR9 misuse sweep** (§13.18.60.4 SR9 :22730 enumerates the ONLY legal reference
  contexts) — MOVE/DISPLAY/arithmetic over a `PicCategory.Pointer` operand rejects loud (closes increment-1
  gap #7; safe: the active usage-pointer-2002 witness displays a literal); whole-group image spanning a
  pointer leaf (gap #10). (ii) **NEW 0881 (declaration-entry band; 0881 verified free — 0898 is the only other
  free 08xx code):** PICTURE with USAGE POINTER (§13.18.60.4 pointer is PICTURE-less; today it silently
  misbinds by picture at DataBinder.cs:720-721 — the W2 hazard class); BASED level not 01/77 or outside
  WS/LS/LINKAGE (§13.16 SR16 :17261); BASED+REDEFINES (:17215); BASED+EXTERNAL (:17219); VALUE on a BASED
  entry (LINKAGE posture). (iii) **0900 introduction gates** via `ConstructRegistry.Check` (the
  PicInfo.cs:515-520 pattern — binder-side, NOT a `{is2002()}?` predicate inside setStatement: a failing
  predicate there would fall through to the OTHER SET alternatives and mis-diagnose).
- **Edition gating (registry + matrix).** Existing rows: `usage-pointer-2002` ACTIVE (constructs.json:439-447
  + ConstructDialectStatus.cs:111) — update BOTH descriptions to drop the stale "ADDRESS OF/BASED/ALLOCATE are
  increment 2+" clause; `allocate-2002` (:40-48, ConstructDialectStatus.cs:64) and `free-2002` (:50-58, :65)
  flip pending→active AND strip the PENDING sentence (the PendingRows contract, VersionMatrixTests.cs:64-74);
  `based-clause-2002` (:88-95, ConstructDialectStatus.cs:69) stays active — its declaration-only group witness
  must keep compiling (verified above). NEW rows + registry entries: `set-address-2002` (ISO §14.9.39 F7 +
  §8.4.3.11; checked in `BindSetAddress`) and `pointer-arithmetic-2002` (F10; checked in the BindSetUpDown
  pointer arm — the 85 index form itself stays ungated); both introducedIn 2002, expectDiagnostic
  COBOLNET0900, vcr caveat "2002 introduction (derive from the 2002 standard)". EditionGateHints needs NO new
  entries (ALLOCATE/FREE/BASED already covered :36-37/:43/:84-85/:97; the new gates are binder-side).
- **Tests / goldens / docs.** Flip `based_pointer`/`pointer_alloc`/`pointer_arith` pending→enabled
  (manifest.json — keep both lists alphabetical; `boolean_data`/`float_usage`/`national_data` stay pending).
  New `PointerAddressingTests` unit battery: two independent `ADDRESS OF X` compare EQUAL (the SameTarget
  structural case increment 1 could not have); UP/DOWN composition; deref-while-NULL and deref-after-FREE
  throw the named ECs; Deref bounds (offset+width > cell length); ALLOCATE form-1 zero/negative → NULL;
  form-2 + INITIALIZED GR7; FREE three-way incl. mid-block operand → notAlloc; ADDRESS OF an EXTERNAL item.
  NEGATIVE corpus additions (per feedback_conformance_tests_per_feature — none exist today, grep-verified):
  allocate_non_based (SR1), allocate_chars_no_returning (SR2), set_address_non_based (SR18), free_non_pointer
  (SR1 :27500), move_pointer (SR9), pic_pointer_conflict (0881). Matrix: the two flips + two new rows + the
  description refresh. The three goldens double as the phase demo (feedback_demo_per_phase) — outputs
  ISO-derived above, not just non-crashing. Full battery + legacy guard green pre-commit with the IC-series
  watched specifically for the Holder→StorageCell rename (behavior-neutral by construction; escalate on any
  diff). Same-change-set doc updates: COBOLNET_DESIGN §9.1 (supersede the `Cell(T)`-as-ALLOCATE-backing claim
  with the StorageCell/CellPointer model + WHY), PicInfo.cs:31-35 and ProgramRegistry.cs:77-79/:93 doc
  comments, this file's M2-DATA-5/M2-PROC-5 rows, DEVLOG entry, resume-prompt STATE banner.
- **STAGED RESIDUE (named, loud).** (1) Pointers across the CALL boundary: no CobolArgAdapt pointer arm
  (ProgramRegistry.cs:168-255) / no CallEmitArg leg (CSharpEmitter.Call.cs:820-860) / LINKAGE POINTER formals
  / `CALL … USING ADDRESS OF` (grammar) — the SR9 CALL/INVOKE/PD-header contexts. (2) The general
  data-address-identifier as an operand (relations, function args) — `ADDRESS` exists ONLY inside
  setAddressStatement today. (3) Multi-receiver F7 + `ADDRESS OF` / NULL senders in the receiver form (grammar
  is single-receiver, plain-dataReference sender :1040). (4) ADDRESS OF: OCCURS-resident anchors (BackingPath
  null), carrier-resident LINKAGE formals, GLOBAL items referenced from contained programs — all staged 0869.
  (5) Restricted data-pointers (`POINTER TO type-name`, SR18 :22760) + STRONG-typedef interactions (F10 SR24).
  (6) FUNCTION-POINTER / PROGRAM-POINTER (F8/F9) — the M3-4 track-(b) leg. (7) Runtime EC-SIZE-ADDRESS (a
  non-integer amount reaching runtime) and EC-STORAGE-NOT-AVAIL (unreachable under .NET OOM — documented
  implementor posture). (8) §8.6.5 end-of-life-cycle disassociation for LOCAL-STORAGE BASED items
  (per-activation re-NULL; WS static lifetime matches today). (9) FILE STATUS / LINAGE / RECORD-clause items
  shall-not-be-BASED (:15498/:15903) — file-subsystem validation sweep. (10) The EXTERNAL leaf-gate
  convergence onto the classifier gate (widens EXTERNAL acceptance — separately testable). (11) byte=char
  breaks under NATIONAL/bit data behind a pointer — coordinate with track (a). (12) Full SR14 strictness for
  USAGE POINTER placement (see OWNER-3).
- **⚠ OWNER-DECISION ITEMS (explicit; defaults chosen, none block implementation).** **OWNER-1** Holder→
  StorageCell unification touches the EXTERNAL emission surface (IC baselines): DECIDED unify per
  feedback_singular_pattern with guard-proof; escalate only if the guard shows a diff. **OWNER-2** GR6
  "binary zeros" for `ALLOCATE … CHARACTERS INITIALIZED` in the character model: DECIDED `'\0'` fill (the
  faithful image; DISPLAY of such storage is undefined-content anyway) — flag if a vendor-comparison corpus
  ever expects spaces. **OWNER-3** §13.18.60.4 SR14 literally says USAGE POINTER at "level 1" (elementary, or
  STRONG-typedef subordinate) — unlike BASED SR16/§14.2.2 SR1 which spell "1 or 77" — yet the repo's own
  matrix witnesses use `77 P USAGE POINTER` (constructs.json:47,:57): DECIDED lenient (01/77 + elementary
  group members, the vendor norm; reading the omission as editorial), strict enforcement deferred to a
  dialect-strictness row. **OWNER-4** the IBM `SET ADDRESS OF non-BASED-linkage-item` idiom is NOT ISO (SR18
  :31399): DECIDED strict-ISO reject (0869); if NIST/legacy corpus ever needs it, add a dialect-leniency
  registry row (project_dialect_strictness) — never silent acceptance. **OWNER-5** `introducedIn` provenance
  for set-address/pointer-arithmetic rows is 2002 by catalog+corpus placement, but no in-repo text can prove
  2002-vs-2014 (Annex E only covers 2014→2023); if matrix authority is required, source the 1989:2002 text.

#### Increment 2 — AS BUILT (DEVLOG 617, 2026-07-05; all three goldens byte-exact)

Landed on the design's storage decision exactly (StorageCell + CellPointer + the CobolPtr helpers; the
Holder→StorageCell unification; the deref-bridge BASED realization over the UNCHANGED Tier-B machinery;
zero grammar change). Deviations/realizations, recorded per the process rule:

1. **The addressable cell's seed honors VALUE via `ImageInitOf`, not `CallInitialImage`** — the design cited
   `CallInitialImage`, which produces the DEFAULT image (the EXTERNAL GR6 posture); based_pointer's first run
   read `B=` five spaces because X's `VALUE "HELLO"` was lost. The emitter now seeds the cell with the SAME
   VALUE-honoring `CobolString.Store(ImageInitOf(canonical), width)` expression the Tier-B stored backing
   uses (`FieldEmitter.ImageInitOf` made internal; the ONE image-seed producer).
2. **`ForceStringCanonical` keeps the proven EXTERNAL leaf gate for ALL THREE callers** (Display-only
   fixed-point leaves) — the design suggested the classifier's broader zoned-BINARY/PACKED gate for
   addressable/BASED callers; the narrower gate only rejects MORE loudly (never silently wrong), and gate
   convergence stays the named follow-up (RESIDUE-10).
3. **The §13.18.60.4 SR9 misuse sweep is DEFERRED** (was increment-1 gap #7, stays a documented gap): a
   pointer operand misused in MOVE/DISPLAY/arithmetic today fails at the Roslyn level (a loud CS error over
   the ManagedPointer field — never silent), not with a named 0869; the total operand-context sweep is
   follow-up. The `move_pointer` negative case is dropped with it.
4. **ALLOCATE based-item INITIALIZED stages loud** (BoundUnsupported naming the GR7 INITIALIZE lowering) —
   the corpus has no consumer and the lowering deserves its own witness when it lands.
5. **Negative-corpus lesson:** the `.err` contract matches ONE CONTIGUOUS substring, and diagnostics
   interpose the item context after the code (`COBOLNET0881: data item 'B': …`) — an `.err` of
   `CODE: message` never matches; write the message fragment only.
6. **Diagnostics:** the SR checks landed on 0869 (statement band) + the NEW 0881 (declaration band:
   PIC-with-POINTER, BASED level/REDEFINES/VALUE); EcWrap gained the BoundFree arm (EC-STORAGE-NOT-ALLOC
   family selection). GR20's data-pointer range is UNBOUNDED (recorded implementor choice — EC-RANGE-PTR
   never raises at SET time; out-of-cell addressing surfaces at deref as EC-BOUND-PTR).
7. **Residue (named):** pointers across the CALL boundary (CobolArgAdapt/CallEmitArg legs, LINKAGE POINTER
   formals), the general data-address-identifier as an operand (relations/args — `ADDRESS` exists only in
   setAddressStatement), multi-receiver F7 + ADDRESS OF/NULL senders in the receiver form,
   qualified/subscripted ADDRESS OF operands, GLOBAL BASED items from contained programs, restricted
   `POINTER TO` + FUNCTION/PROGRAM-POINTER (M3-4), EC-STORAGE-NOT-AVAIL posture (unreachable under .NET),
   LOCAL-STORAGE BASED end-of-cycle re-NULL, the FILE-subsystem shall-not-be-BASED sweep, byte≠char under
   NATIONAL/bit (track a), SR9 sweep (item 3), EXTERNAL-gate convergence (item 2).

#### Increment 2 — the ADVERSARIAL REVIEW WAVE (same change set; 24 raw → 23 confirmed / 1 refuted; ~10 distinct issues)

The 4-lens find→2-skeptic-verify workflow (wf_4c49e522-ec0) over the landed diff. Disposition, all same set:

**Fixed:**
- **ADDRESS OF a SUBORDINATE of a BASED record dropped its ClassOffset (major)** — the BASED leg of
  `PtrAddressOfText` returned the raw implicit pointer for ANY item under a based root (silent wrong-bytes
  aliasing); now `CobolPtr.UpBy(__addr_X, classOffset)` for non-root items (§8.4.3.11 GR1 — the address OF
  THE ITEM; UpBy's GR18 null trap is the right posture for a child of an unallocated record). CLI-probed:
  `ADDRESS OF B2` reads `PQR`, not the base's `XYZ`.
- **GR19 was a silent truncation (major)** — `SET P UP BY 2.5` moved by 2 via the Align(…, 0) rescale, and
  the shipped registry/matrix text falsely claimed a bind-time reject. Realized EXACTLY now:
  `CobolPtr.UpByScaled(p, scaledValue, scale)` — the divisibility test IS §14.9.39 F10 GR19's integer-VALUE
  rule (2.0 moves by 2; 2.5 → EC-SIZE-ADDRESS fatal); registry + matrix text corrected. This SUPERSEDES the
  design's "statically integer-typed 0869" plan — GR19 is a value rule, and the runtime check is strictly
  more conformant than a static-type reject.
- **BASED+EXTERNAL undetected (§13.16.3 SR5)** — both mechanisms would emit a bridge under the ONE
  BackingCsName (CS0102); the 0881 band gained the arm.
- **A BASED USING formal undetected (§14.2 SR1 :23658** — the same sentence as the implemented REDEFINES
  half**)** — the carrier-resident CsName rewrite would poison the based BackingCsName into invalid C#;
  0889 now covers both clauses.
- **BASED/ADDRESS OF in a CLASS unit → CS0103** — class binders ran the pointer pass but the OO emitter has
  no cell/bridge loops; staged loud (0899, the DataBinder.Oo method-WS EXTERNAL/GLOBAL gate posture).
- **FREE's nonfatal EC set never ran the declarative** — the checked leg now emits the §14.6.13.1.3 #5
  sequence (status set → F3 selection → RESUME-AT honored, no-handler continues).

**Reverted over-rejection:** the 0881 **VALUE-on-BASED arm was WRONG** — ISO defines VALUE semantics for
based entries (it seeds `ALLOCATE … INITIALIZED`'s GR7 TO-VALUE leg; un-INITIALIZED content is GR8-undefined,
so the space-filled cell is conformant). The arm is removed; a prohibition existed in no spec sentence.

**Documented (named residue, not fixed):** (a) >>TURN-enabled FATAL pointer ECs (EC-DATA-PTR-NULL /
EC-BOUND-PTR) raise as raw fatal terminations without consulting a USE F3 declarative — the EcWrap
family-selection walk for based-referencing statements is a named follow-up (checking-OFF behavior, the
corpus-wide default, is conformant as-is); (b) the whole-group-image pointer-leaf guard (increment-1 gap
#10, the design's seam 9) did NOT ship — a pointer leaf inside an image group still contributes zero width
silently (pre-existing increment-1 behavior, unchanged by this wave; still open).

**Coverage added (the tests lens):** PointerAddressingTests ×9 — ADDRESS OF an EXTERNAL record (the
unification's named witness), F10 multi-target + the SR23 mix 0869, UNMASKED gate-identity facts at 85 (the
review showed the two new 0900 gates were deletable without any test failing — the facts assert the gates'
own where-texts), the ALLOCATE legs (fractional round-up / INITIALIZED / form-2 RETURNING / zero→NULL),
FREE under >>TURN'd checking (both emit legs compile), subordinate-of-BASED; CobolPtrTests +1 (UpByScaled
both ways); negative corpus +1 (based-level-05).

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
| M2-PROC-5 | ALLOCATE / FREE (based storage) | open | **LANDED (DEVLOG 617)** | Both ALLOCATE formats (GR1 round-up / GR2 ≤0→NULL / GR6 zero-fill / GR3-GR4 based) + FREE's GR1 three-way (nonfatal EC-STORAGE-NOT-ALLOC through the TurnState-gated block; dangling aliases loud at Deref); pointer_alloc ENABLED byte-exact; allocate-2002/free-2002 matrix rows flipped ACTIVE | none | The based INITIALIZED GR7 INITIALIZE lowering is a named staged residue. |
| M2-PROC-6 | GOBACK RETURNING (done); EXIT variants; CONTINUE AFTER (deferred) | partial | LANDED (+ EXIT FUNCTION, DEVLOG 616) | **Verified:** BoundGoback ReturningSource+Raising (StatementBinder.Call.cs), CallEmitGoback; goback_returning.cob ENABLED. **EXIT FUNCTION leg LANDED (Phase 4c):** UdfBindExitFunction → BoundGoback (the §14.9.18.4 GR5 function-return synonym; RAISING tail rides GOBACK's), 0827 placement band, the exit-function-window matrix row flipped ACTIVE with a conforming FUNCTION-ID witness (expectDiagnosticBelow 0900 / 0902 at 2023); udf_exit_function golden proves the early return (X=0014) | phase 7 residue | EXIT SECTION → BoundUnsupported. CONTINUE AFTER not in grammar → Phase 7. |

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
| **(b) pointers/ALLOCATE/BASED** | ~~M2-DATA-5, M2-PROC-5~~ **LANDED (DEVLOG 613/617)**; + USAGE FUNCTION/PROGRAM-POINTER leg of M3-4 | 0 primary (+1 shared leg) | Data pointers live end-to-end (StorageCell+CellPointer); residue = CALL-boundary pointers + the M3-4 typed-pointer leg (as-built list). |
| **(c) UDF/prototypes** | ~~M2-UDF-1, M2-UDF-2~~ **LANDED (DEVLOG 615)**; ~~EXIT FUNCTION leg of M2-PROC-6~~ **LANDED (DEVLOG 616)**; M2-UDF-3, M2-UDF-4 (ALL INTRINSIC + keyword-omitted legs); + >>CALL-CONVENTION (loose) | 2 primary | UDF invocation + EXIT FUNCTION live in-group; residue = prototypes/cross-assembly + the UDF-4 legs. |
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
