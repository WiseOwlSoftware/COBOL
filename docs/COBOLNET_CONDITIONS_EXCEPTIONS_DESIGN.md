# COBOL.NET — Conditions & Exception Model (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §11; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Decision-complete design for the conditions + exception subsystem of the greenfield COBOL→C#/Roslyn compiler (src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}; C# namespaces `CobolNet.*`). Covers IF/ELSE/END-IF; EVALUATE (all forms); level-88 condition-names + SET cond TO TRUE/FALSE; class/sign/relational/abbreviated-combined conditions with NOT>AND>XOR>OR precedence; and the COBOL-2002/2023 EC exception model (EC-* hierarchy, >>TURN, RAISE/RESUME, USE…EXCEPTION/ERROR declaratives, EXCEPTION-OBJECT, ON SIZE ERROR/AT END/INVALID KEY/ON OVERFLOW/ON EXCEPTION).

Two C# code shapes — both are the Roslyn backend's rendering of the ONE backend-neutral bound tree (G4/ICodeGenBackend: all semantics live in the BoundCondition/EC bound nodes + the binder-computed TurnState; emitters only render; the future CIL backend lowers the SAME bound nodes to branches with its own private lowering): (1) conditions are PURE C# boolean expressions (ConditionRenderer.Render(BoundCondition)→string, no side effects) so they compose into if/while/?:/EVALUATE-arms and level-88 bool properties; (2) the exception model is stateful runtime (CobolNet.Runtime.Exceptions) plus emitted guards that appear ONLY when a program uses the feature — EC checking is OFF by default (ISO §14.6.13.1.1, §5000), so the typed-native fast path emits zero exception scaffolding in the common case.

Correct behavior is defined by the ISO spec (specs/ISO_COBOL.md — cite the §); the legacy CobolSharp.Compiler/CodeGen/Lowering/ConditionLowerer.cs and CobolSharp.Runtime/PicRuntime.cs (364 NIST green) are a differential regression net and reference ONLY, never authority; the byte IMPLEMENTATION is rejected and re-derived over native string/long(/Int128)/bool — the numeric design's scaled integers; System.Decimal is rejected (docs/COBOLNET_NUMERIC_DESIGN.md). THIS document is that full prose (condensed view: docs/COBOLNET_DESIGN.md §11; brief overview: docs/COBOLNET_ARCHITECTURE.md). New diagnostics occupy a COBOLNET07xx band. New runtime classes: CobolClass (class-condition predicates over UTF-16 chars), ExceptionCatalog (generated from ISO Table 13: level-3→level-2→EC-ALL hierarchy + fatality), ExceptionState (last-exception register, EXCEPTION-OBJECT, file/location/statement), CobolException/CobolFatalException, ExceptionDispatch (declarative registry). Implementation is mechanical from here.

## Decisions

### D1. Conditions are bound to backend-neutral BoundCondition nodes (BoundRelational/BoundLogical/BoundNot/BoundCondition88/BoundSignCondition/BoundClassCondition); the Roslyn backend's ConditionRenderer.Render(BoundCondition)→string emits them as pure, side-effect-free C# boolean expressions. The grammar's rule cascade (logicalOr→logicalXor→logicalAnd→unaryLogical→primaryCondition) fixes precedence at parse/bind time. (As built: src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs.)

**Rationale.** COBOL condition precedence NOT>AND>XOR>OR is already encoded by the grammar's rule cascade, so precedence is preserved by construction without us re-grouping. Pure expressions compose into if/while(!(…))/?:/EVALUATE arms/level-88 properties — one translator serves every consumer.

**Rejected alternatives.** Lowering conditions to imperative IR with temporaries (the legacy IrBinaryLogical model) — unnecessary in a C# target where the host language already has boolean expressions; it would also force statement context where an expression is wanted (e.g. ?:).

### D2. Fully parenthesize every emitted binary boolean node: (a && b), (a || b), (a ^ b).

**Rationale.** C#'s bool precedence is ! > & > ^ > | > && > || — so ^ binds TIGHTER than &&/||, which does NOT match COBOL's AND>XOR>OR. Explicit parens make the emitted tree's grouping exactly the COBOL parse tree's grouping, independent of any C# precedence subtlety.

**Rejected alternatives.** Rely on C# operator precedence — the ^-vs-&& ordering mismatch is a genuine correctness trap for logical XOR.

### D3. Emit short-circuiting && / || for COBOL AND / OR (a deliberate divergence from the eager legacy oracle).

**Rationale.** Idiomatic, faster, and safer (suppresses faults in a guarded right operand). Empirically verified corpus-safe: a scan of tests/nist/programs found ZERO guard-then-same-variable-subscript idioms; the 44 'AND <subscripted>' cases use a subscript independent of the guard (e.g. IF SUB4 = 6 AND WZ-X-CHAR(SUB2) = SPACE).

**Rejected alternatives.** Eager evaluation matching legacy ConditionLowerer lines 188–196 (both operands into temporaries, then combine) — would defeat clean &&/|| output for a behavior no conformance test needs. If a future program relies on eager eval (IF I>0 AND TABLE(I)=X with EC-BOUND off), the local fix is to hoist that operand before the &&.

### D4. EVALUATE (all forms) lowers to a chained if/else-if/else, NOT a C# switch.

**Rationale.** This is exactly ISO §14.9.13.4 GR4 (process each WHEN left-to-right, first match wins). COBOL WHEN arms are ranges, conditions, multiple ALSO subjects, ANY, partial expressions, and arbitrary per-subject values — they are not constant case labels. The if/else-if chain is correct, readable, and the C# compiler optimizes dense integer chains.

**Rejected alternatives.** C# switch — illegal for non-constant labels; ranges/conditions/ANY/partial-expressions have no switch form. (A future peephole may detect a single-subject all-single-integer EVALUATE and emit a switch for prettiness.)

### D5. EVALUATE selection subjects are hoisted into locals (var _e0=…; var _e1=…;) evaluated exactly once before the chain; bare identifiers/literals may stay inline.

**Rationale.** ISO §14.9.13.4 GR3: each selection subject is evaluated once at the start. Side-effecting subjects (functions, arithmetic) must not be re-evaluated per WHEN. Hoisting is always correct; inline is a readability shortcut only for the no-side-effect case.

**Rejected alternatives.** Re-render the subject in each WHEN match — re-evaluates side effects and arithmetic per arm; wrong.

### D6. Level-88 condition-names become C# expression-bodied static bool PROPERTIES derived from the conditional variable's live value (not stored bools).

**Rationale.** ISO §8.8.4.5: a condition-name is an abbreviation for 'conditional variable == one of its values'. A property recomputes truth from the current value, so any MOVE/arithmetic to the parent is reflected with no bookkeeping. SET cond TO TRUE/FALSE writes the PARENT, never a bool.

**Rejected alternatives.** A stored bool kept in sync on every assignment to the parent — fragile, requires intercepting every write path; semantically wrong (the value can change via REDEFINES/group MOVE).

### D7. SET cond-name TO TRUE moves the FIRST VALUE literal into the conditional variable; SET cond TO FALSE moves the WHEN SET TO FALSE literal (error COBOLNET0705 if none).

**Rationale.** ISO §14.9.39 GR6 (TRUE → first literal of the VALUE clause; for a THRU range, the range start) and §13.18.63 GR20 (FALSE → the WHEN SET TO FALSE literal-4). The FALSE phrase is required for SET TO FALSE.

**Rejected alternatives.** Treat SET cond TO TRUE as setting a bool flag — wrong; it is a MOVE of a specific literal into the parent per the VALUE-clause rules.

### D8. Class conditions (NUMERIC/ALPHABETIC/-LOWER/-UPPER/user CLASS) run over the character image via a new CobolClass runtime; for a pure native scaled-integer (long/Int128) item, IS NUMERIC folds to true (COBOLNET0706).

**Rationale.** In the typed model the value IS the field; class tests operate on the char image. A native numeric item cannot hold non-digits, so NUMERIC is constant-true (the meaningful test is on a PIC X holding digits). ALPHABETIC is the closed Latin set {A-Z,a-z,space} (ISO §8.8.4.4) — NOT char.IsLetter (must reject Unicode/accented letters; legacy comment).

**Rejected alternatives.** Reuse the legacy byte-buffer PicRuntime predicates — rejected byte substrate. Use char.IsLetter/char.IsDigit — wrongly accepts Unicode letters/digits; ISO defines closed character sets.

### D9. EC checking is OFF by default; conditional phrases (ON SIZE ERROR/AT END/INVALID KEY/ON OVERFLOW/ON EXCEPTION) are ALWAYS active when written and do NOT require >>TURN.

**Rationale.** ISO §14.6.13.1.1/§5000: default is EC-ALL CHECKING OFF. ISO §14.6.13.1.4 GR1: an explicit conditional phrase handles the condition regardless of TURN state. The phrases are the COBOL-85/2002 handler form the NIST corpus uses; >>TURN/EC-name declaratives are the secondary 2002+ mechanism (edition-gated; diagnosed at --std=85 — see Per-edition gating). Result: programs that don't use exceptions emit zero scaffolding (commercial-quality fast path).

**Rejected alternatives.** Always-on EC checking — huge per-statement runtime cost (ISO NOTE warns of significant penalty) and non-ISO default. Require >>TURN for the classic phrases — breaks COBOL-85 ON SIZE ERROR / AT END semantics.

### D10. >>TURN is resolved at COMPILE time by a TurnState that walks the procedure division in source order; it decides WHETHER the emitter emits an EC guard at all for each statement.

**Rationale.** ISO §4970/§5018: TURN enables checking for the source text that follows in the compilation group. EC-ALL expands to all level-3 names; a level-2 name expands to its children (§5002-5004); EC-I-O-WARNING only toggles explicitly (§5006). Compile-time resolution means OFF compiles to nothing — the key C#-native win.

**Rejected alternatives.** A runtime per-EC enabled-flags table consulted at every statement — defeats the zero-overhead property and adds branches where none are needed.

### D11. USE…EXCEPTION/ERROR declaratives compile to paragraph-methods plus a compile-time declarative registry keyed (EC / file / open-mode); the dispatch call is injected at the operation site and the declarative method RETURNS a ResumeAction enum {Default, NextStatement, Procedure(name)}.

**Rationale.** ISO §9.1.12 'first one in the list that matches' (file-specific > open-mode > exception-name) and §14.6.13.1.4 GR3 (declarative runs when no explicit phrase handled it). Returning ResumeAction lets RESUME (§14.9.33) redirect control: NextStatement falls through past the offending statement; Procedure does a goto (as if GO TO). USE GLOBAL chains to the parent program's registry.

**Rejected alternatives.** A single program-wide try/catch that re-dispatches — loses the precise 'resume after the statement' semantics and the applicable-statement selection; harder to debug.

### D12. The exception-checking PERFORM WHEN form (COBOL-2023 — VCR row 79; diagnosed at --std=85|2002|2014) is the one place a real C# try/catch is used; RAISE/RESUME and fatal/nonfatal termination are runtime calls (CobolException.Raise / CobolFatalException at the run-unit boundary in Main).

**Rationale.** PERFORM…WHEN ec explicitly traps an exception in imperative-statement-1, which maps naturally to try{…}catch(CobolException)when(matches). RAISE EXCEPTION runs the §6.6 handling sequence; a fatal EC with no handler throws CobolFatalException caught at Main → abnormal termination + nonzero exit (ISO §14.6.13.1.3 — implementor may terminate).

**Rejected alternatives.** Model every EC via C# exceptions/try-catch globally — exceptions are for the trap forms; the common inline phrases and declaratives use status-flag/branch control flow for correctness (resume-after-statement) and zero default cost.

## C# mapping

IF: `IF c [THEN] s1 [ELSE s2] END-IF` → `if (<RenderCondition(c)>) { s1 } else { s2 }`. CONTINUE→empty block. NEXT SENTENCE→lower the sentence as a labeled block + `goto <after_sentence>;` (COBOLNET0701). Nested IF: each branch is fully braced so C# dangling-else is structurally impossible.

RELATIONAL: numeric (both operands numeric) → render as scaled longs, align to larger scale via existing NumX/Align, then `(<l> <op> <r>)` (exact, no truncation). Alphanumeric (either side non-numeric) → `(CobolString.Compare(a,b,weights?) <op> 0)` with space-extension of the shorter operand (ISO §8.8.4.1.2). Pointer (= / NOT = only) → `ReferenceEquals(p,q)` / `p is null`. Figurative ZERO vs numeric → numeric 0. Literal-vs-literal constant-folds to true/false. Operator mapping via existing MapOperator (all symbolic + word + NOT-prefixed forms; needs an ~18-form unit-test matrix).

SIGN: `op IS [NOT] POSITIVE|NEGATIVE|ZERO` → `(<num> > 0)` / `(<num> < 0)` / `(<num> == 0)`, NOT wraps in !(…). (NOT POSITIVE = ≤0, includes zero — the !(…) handles it.)

CLASS: `IF X IS NUMERIC` → `if (CobolClass.IsNumeric(X))`; `IS ALPHABETIC`→`CobolClass.IsAlphabetic(X)` ({A-Z,a-z,space} closed set). Numeric long item: folds to `if (true)` (COBOLNET0706). User CLASS HEX → `CobolClass.IsUserClass(X, "0123456789ABCDEF")` (THRU ranges expanded). NOT wraps in !(…).
New runtime: `static class CobolClass { bool IsNumeric(string); bool IsNumericDisplay(string,NumProfile); bool IsAlphabetic(string); bool IsAlphabeticLower(string); bool IsAlphabeticUpper(string); bool IsUserClass(string, ReadOnlySpan<char>); }` — ported verbatim from PicRuntime.IsNumericClass/IsAlphabeticClass (legacy 2379–2464) but over UTF-16 chars, sign-aware (overpunch {,A-I,},J-R / separate +,-).

LOGICAL: AND→`(a && b)`, OR→`(a || b)`, XOR→`(a ^ b)`, NOT→`(!(p))` — all fully parenthesized, short-circuiting.

ABBREVIATED COMBINED (ISO §8.8.4.2): walk keeping a current subject+operator from the last full relation; `op operand`→expand to `subject op operand`; bare `operand`→`subject currentOp operand`; leading NOT negates that relation only. Example `IF A = B OR C OR > D` → `((A==B) || (A==C) || (A>D))`.

LEVEL-88: 
```
01 WS-STATE PIC 9.   88 ACTIVE VALUE 1.   88 PENDING VALUE 2 THRU 4.   88 DONE VALUE 5 9 WHEN SET TO FALSE 0.
```
→
```
private static long WS_STATE = 0L;
private static bool ACTIVE  => WS_STATE == 1L;
private static bool PENDING => WS_STATE >= 2L && WS_STATE <= 4L;
private static bool DONE    => WS_STATE == 5L || WS_STATE == 9L;
```
Single value→`==`; THRU→`>= from && <= to`; multiple→OR; alpha values space-extended to parent width, ALL literal repeated to width. Subscripted cond-name→method `bool COND(long i)=>parent[i-1]==v;` (tables have LANDED — fixed OCCURS→T[]; implement the parameterized-method form, keeping COBOLNET0704 only while it remains unimplemented). SET ACTIVE TO TRUE→`WS_STATE = CobolNum.Store(1L,0,_P_WS_STATE);`. SET DONE TO FALSE→`WS_STATE = CobolNum.Store(0L,0,_P_WS_STATE);`.
DataItem gains `List<ConditionName> ConditionNames`; `record ConditionName(string CobolName, string CsName, IReadOnlyList<CondValue> TrueValues, CondValue? FalseValue)`; `readonly record struct CondValue(string FromLiteral, string? ThruLiteral, bool IsAll)`.

EVALUATE: hoist subjects → chained if/else-if/else. One WHEN clause's match = OR over its WHEN phrases ( AND over ALSO subjects ( per-subject match ) ). Per-subject: ANY→true; value→`==` (scaled/collating); range v1 THRU v2→`>=v1 && <=v2`; partial-expr (item starts with relop/class/sign)→prepend subject; TRUE/FALSE↔condition subject→`_eK == true/false`; group NOT→negate the group's conjunction. WHEN OTHER→final else.
Example `EVALUATE WS-DAY ALSO TRUE / WHEN 1 THRU 5 ALSO WS-OPEN … / WHEN 6 7 ALSO ANY … / WHEN OTHER …` →
```
var _e0 = WS_DAY; var _e1 = true;
if (((_e0>=1L && _e0<=5L) && (WS_OPEN==_e1))) {…} else if (((_e0==6L||_e0==7L) && true)) {…} else {…}
```

ON SIZE ERROR (ISO §14.7.5): the checked store is **`CobolNum.TryStore`** — REVISED 2026-06-08 (SSOT §14.7): the numeric design's `TryStore` and this section's original `StoreChecked` are the SAME operation (store + capacity/inexact check, receiver unchanged on overflow), settled on the single name **`CobolNum.TryStore`** (returns `bool`; `false` = ON SIZE ERROR). It rounds to scale FIRST, then tests integer-part capacity; division-by-zero / exponentiation-rule → size error. On error the receiver is LEFT UNCHANGED, so stage the value and assign conditionally. Multiple receivers: OR the per-receiver flags; non-overflowing receivers ARE updated. (Signature: `bool TryStore(CobolInt value, in NumProfile receiver, CobolRounding mode, out long stored)`.)
```
bool _se=false; { var _v = new CobolInt(…, _s); if (CobolNum.TryStore(_v, _P_B, _mode, out long _r)) B = _r; else _se = true; }
if (_se) { <ON SIZE ERROR> } else { <NOT ON SIZE ERROR> }
```

AT END / INVALID KEY / ON OVERFLOW / ON EXCEPTION: status flag from the op drives a branch; NOT form = the else (success). `var _st = CobolFile.Read(f,…); if (CobolStatus.IsAtEnd(_st)) {AT END} else {NOT AT END}`. AT END↔EC-I-O-AT-END↔status 1x; INVALID KEY↔2x; ON OVERFLOW↔EC-OVERFLOW-STRING/UNSTRING; ON EXCEPTION (CALL)↔EC-PROGRAM-NOT-FOUND.

EC RUNTIME (CobolNet.Runtime.Exceptions): `enum ExceptionCondition` (level-3 names) + hierarchy map (level3→level2→EC-ALL) + per-name Fatality {Fatal,NonFatal,Imp}, generated from ISO Table 13 in ExceptionCatalog.cs. `static class ExceptionState { string? LastExceptionName; object? ExceptionObject; string? LastExceptionFile/Statement/Location; void Clear(); }`. RAISE EXCEPTION ec→`CobolException.Raise(ec)`; RAISE id→`CobolException.RaiseObject(obj)` (sets EXCEPTION-OBJECT); fatal unhandled→`throw new CobolFatalException(ec)` caught at Main. FUNCTION EXCEPTION-STATUS→`ExceptionState.LastExceptionName ?? "        "`; EXCEPTION-OBJECT→`ExceptionState.ExceptionObject`.

USE declaratives: declarative SECTION→paragraph-method returning `enum ResumeAction {Default, NextStatement, Procedure(string)}`; `ExceptionDispatch.Invoke(ec, file)` selects first match (file>open-mode>ec) and calls it; site inspects ResumeAction (NextStatement→fall through; Procedure→goto label).

## Hard problems

### Short-circuit (C# &&/||) vs eager (legacy non-short-circuit) AND/OR — observably differs for IF I>0 AND TABLE(I)=X when the guard protects a faulting subscript (EC-BOUND off → IndexOutOfRangeException in the typed model).

Chose short-circuit (idiomatic, safer). Verified corpus-safe by scanning tests/nist/programs: ZERO guard-then-same-variable-subscript idioms (the 44 'AND <subscripted>' cases use independent subscripts). Documented as a conscious divergence from the oracle; local escape hatch if a future program needs eager eval: hoist the right operand before &&.

### Abbreviated combined conditions (IF A > B AND < C OR = D) — the subject and/or operator are elided after the first relation; the LEGACY emitter silently dropped them — the greenfield binder must expand them into full relations.

Expand at BIND time into ordinary full BoundRelational nodes (G4: the expansion is semantics, so it lives in the binder — every backend receives the already-expanded tree): maintain current-subject + current-operator from the most recent full relation; `op operand`→`subject op operand`; bare `operand`→`subject currentOp operand`; reset on each full comparison; leading NOT negates that relation only (it is part of the operator, not the subject). Ships with a dedicated test set; flagged as the single most error-prone condition feature.

### EVALUATE partial expressions (EVALUATE X ALSO Y / WHEN > 5 ALSO "A" THRU "M") — a WHEN object that begins with a relational/class/sign operator must be combined with its subject (ISO §14.9.13.3 GR5/8, §14.9.13.4 GR4a-2).

Binder detects the partial form (leftmost token is a relop/class-name-without-id/sign-word) and synthesizes `subjectK <partial>` as a full condition; the corresponding subject is treated as TRUE. Grammar already admits `condition` as a WHEN item; injection happens in binding.

### ON SIZE ERROR leaving the receiver unchanged + multiple receivers + rounding interaction (ISO §14.7.5).

CobolNum.TryStore (the single settled name — see the REVISED note in the C# mapping) computes the candidate, rounds to scale FIRST, then tests integer-part capacity; writes the field only if no overflow (or no SIZE ERROR phrase present). Each receiver tested independently; non-overflowing receivers updated; the phrase fires if ANY overflowed (OR of per-receiver flags). Division-by-zero and exponentiation-rule violations route to the same size-error path (EC-SIZE-ZERO-DIVIDE / EC-SIZE-EXPONENTIATION).

### RESUME control flow (NEXT STATEMENT vs procedure-name vs GLOBAL-declarative≡CONTINUE) requires a declarative to redirect the caller's control after it returns (ISO §14.9.33).

Declarative methods return a ResumeAction enum; the dispatch site at the offending statement inspects it: NextStatement→fall through past the statement (suppress termination for a fatal EC); Procedure(name)→goto that label (as if GO TO); Default→ISO default (continue for nonfatal, terminate for fatal). RESUME inside a GLOBAL declarative is compiled as CONTINUE (§30319).

### >>TURN must gate WHETHER a guard is emitted per statement, in source order, with EC-ALL/level-2 expansion — without a runtime cost when OFF.

A compile-time TurnState walks the procedure division in source order maintaining the enabled-EC set (EC-ALL→all; level-2→its level-3 children; EC-I-O-WARNING explicit-only). The emitter consults TurnState.IsEnabled(ec, atStatement) before emitting any EC check. OFF compiles to nothing — no runtime branch. WITH LOCATION makes the guard pass (file,line,verb) into ExceptionState.

### Whole-group comparison (IF GROUP-A = GROUP-B, or group vs literal) compares the group as one alphanumeric value, but a group is a record struct in the typed model (no character buffer).

RESOLVED — the whole-group character-image facility LANDED (G6-core): every group record struct emits AsImage()/FromImage() (FieldEmitter), and a character-image group operand routes through .AsImage() into CobolString.Compare (OperandText). COBOLNET0708 is retired. This is the one IF/EVALUATE operand kind that native field comparison cannot do alone.

### Distinguishing a bare data-name operand in a condition: level-88 condition-name vs boolean PIC 1 vs mnemonic switch vs numeric-implicit-≠0 vs alphanumeric truthiness.

Binder resolves the name's category: 88→condition-name bool property; PIC 1/boolean→the field itself; SPECIAL-NAMES switch→switch test; numeric→`(num != 0)` (legacy lines 220–228); alphanumeric→`!string.IsNullOrWhiteSpace(item)` (legacy-compatible, COBOLNET0702).

## Edge cases

- NEXT SENTENCE (obsolete) is NOT CONTINUE: it jumps past the next period, so it lowers via a labeled sentence block + goto, unlike a plain if-fall-through (COBOLNET0701).
- ALPHABETIC is the closed set {A-Z,a-z,space} only (ISO §8.8.4.4) — must NOT use char.IsLetter (rejects accented/Unicode letters); legacy comment line 2436.
- IS NUMERIC on a signed numeric-DISPLAY item accepts the overpunch sign ({,A-I positive; },J-R negative) or separate sign (+/-) at the sign position; spaces are NOT digits so a field with embedded/trailing spaces is NOT NUMERIC.
- IS NUMERIC on a pure native long/Int128 item folds to constant true (COBOLNET0706) — the REDEFINES/overlay revisit has FIRED (Tier A+B landed): the fold applies ONLY to a numeric item with no REDEFINES/overlay view; an aliased item routes through the runtime CobolClass check (CobolClass.IsNumeric, §8.8.4.4 GR1/GR2).
- NOT POSITIVE means ≤ 0 (includes zero), which is NOT the same as NEGATIVE — the !(>0) wrap gets it right.
- Figurative ZERO compared with a numeric value is the numeric 0 (ISO §8.3.1.2), not the character '0'.
- Literal-vs-literal comparisons constant-fold at emit time to true/false (clean output, matches mainstream compilers).
- SET cond TO FALSE with no WHEN SET TO FALSE phrase is a syntax error (the FALSE phrase is required) → COBOLNET0705.
- SET cond TO TRUE on a THRU-range condition-name moves the range START (first literal).
- EVALUATE subjects with side effects (function calls / arithmetic) must be hoisted to a local and evaluated exactly once (ISO §14.9.13.4 GR3); bare identifiers/literals may stay inline.
- Multiple consecutive WHEN phrases sharing one body are OR-ed (WHEN a WHEN b … imperative = a OR b); ALSO subjects within one WHEN are AND-ed.
- WHEN with no match and no WHEN OTHER → EVALUATE does nothing (no final else emitted).
- ON SIZE ERROR leaves the receiver UNCHANGED; without the phrase, overflow silently truncates to the PICTURE low-order digits — UNLESS EC-SIZE is >>TURNed on (the bridge between the phrase and the EC mechanisms).
- ROUNDED happens BEFORE the size-error test (round to scale, then check integer-part capacity).
- AT END/INVALID KEY phrase, when present and the condition exists, suppresses all OTHER applicable exception processing (ISO §11409) — the phrase wins over declaratives.
- Pointer relations support only = / NOT = (ISO §8.8.4.1.4) → ReferenceEquals / is null on ManagedPointer.
- EC-I-O-WARNING can only be turned on/off explicitly in a >>TURN or PERFORM WHEN (§5006); EC-ALL does not include it.
- Whole-group comparison routes through the record struct's AsImage() character image (landed, G6-core) into CobolString.Compare.
- User-defined exceptions EC-USER-<suffix> are always nonfatal (ISO §24505) and only raisable by RAISE / EXIT…RAISING / GOBACK RAISING.
- A condition-name may be qualified/subscripted (cond-name OF grp (i)) → emit a parameterized bool method, not a property (tables have landed; COBOLNET0704 only while the method form remains unimplemented).

## Per-edition gating (G1 — one `cobol.exe`, four ISO editions via `--std`)

Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in
every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed).
Tests (NIST etc.) only VERIFY; they never SCOPE. Gating keys off the single `DialectMode` (SSOT §2); the per-construct
rows live in `docs/VERSION_CHANGE_REFERENCE.md` (VCR) — the 130-row edition-change checklist (2002→2023 deltas ONLY;
it has NO 85→2002 rows — derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog) —
and become (construct × edition) cases in the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md`; Phase 0
done).

- **COBOL-85 baseline (valid in all four editions):** IF/ELSE/END-IF, EVALUATE, CONTINUE, level-88 + SET cond TO TRUE,
  class/sign/relation/abbreviated-combined conditions, the ON SIZE ERROR / AT END / INVALID KEY / ON OVERFLOW /
  ON EXCEPTION phrases, and USE AFTER STANDARD ERROR/EXCEPTION file declaratives.
- **XOR / EXCLUSIVE-OR (D1/D2): introduced 2023** (VCR rows 32/41 — user-defined words before). At `--std=85|2002|2014`
  an XOR operator in a condition is a diagnostic, and `XOR`/`EXCLUSIVE-OR` must still be accepted as user-defined words.
- **The EC model is 2002+:** `>>TURN`, the EC-* exception-names, RAISE, RESUME, EXCEPTION-OBJECT, the EC-name USE
  declarative form (USE AFTER EXCEPTION CONDITION), and FUNCTION EXCEPTION-STATUS/-FILE/-LOCATION/-STATEMENT. At
  `--std=85` each gets a not-in-this-edition diagnostic.
- **2023-only EC additions, diagnosed at 85/2002/2014:** the exception-checking PERFORM (VCR row 79), `>>PROPAGATE`,
  EC-I-O-WARNING and the EC-MCS-*/EC-FLOW-*/EC-CONTINUE-*/EC-EXTERNAL-* names (VCR rows 40/61), and the optional
  file-connector argument of EXCEPTION-FILE/-N (VCR rows 68/69).
- **SET cond-name TO FALSE / WHEN SET TO FALSE (D7): 2002+** — diagnosed at `--std=85`; COBOLNET0705 (missing FALSE
  phrase) applies only in editions that have the phrase.
- **CALL … ON OVERFLOW: REMOVED in 2023** (VCR row 3) — accepted at 85/2002/2014, diagnosed at 2023 (ON EXCEPTION is
  the replacement).
- **VALIDATE / EC-VALIDATE: obsolete in 2023** (VCR row 125; SSOT §18.17) — flag obsolete.
- **NEXT SENTENCE:** edition-flagged per the spec's obsolete/archaic classification (see the edge-case note above).

## ISO citations

- ISO/IEC 1989:2023 §8.8.4.1 — relation conditions (algebraic numeric value comparison; alphanumeric space-extension §8.8.4.1.2; pointer = / NOT = §8.8.4.1.4)
- §8.8.4.2 — abbreviated combined relation conditions (elided subject/operator; NOT is part of the operator)
- §8.8.4.3 / §8.8.4.4 — class conditions (NUMERIC; ALPHABETIC closed set {A-Z,a-z,space}; sign conditions)
- §8.8.4.5 — simple condition-name condition (88-level abbreviates 'conditional variable == one of its values')
- §8.8.4.9 — logical operators and precedence NOT > AND > XOR > OR; logical exclusive-or
- §8.3.1.2 — figurative ZERO as numeric 0; §8.4.3.6 — EXCEPTION-OBJECT predefined object reference
- §13.18.63 — VALUE clause condition-name format (THRU ranges; WHEN SET TO FALSE literal-4; GR20 SET TO FALSE)
- §14.6.13 / §14.6.13.1.1 — exception condition handling; default EC-ALL OFF; last-exception status; per-element indicators cleared at start of each statement
- §14.6.13.1.3 / §14.6.13.1.4 — fatal vs nonfatal exception condition handling order (phrase → PERFORM WHEN → USE declarative → continue/terminate)
- §14.6.13.1.5 — exception objects (RAISE id / RAISING); §14.6.13.1.6 + Table 13 — exception-name hierarchy (level-1 EC-ALL, 23 level-2 names, level-3 + fatality)
- §14.6.13.2 — incompatible data (EC-DATA-INCOMPATIBLE; the reason class conditions exist)
- §14.7.5 — SIZE ERROR phrase and size error condition (receiver unchanged on error; rounding before test; EC-SIZE-OVERFLOW/ZERO-DIVIDE/EXPONENTIATION)
- §14.9.9 CONTINUE; §14.9.13 EVALUATE (§14.9.13.3 syntax incl. Table 15 operand combinations; §14.9.13.4 GR3 subjects-once, GR4 left-to-right first-match, GR5 WHEN OTHER); §14.9.19 NEXT SENTENCE
- §14.9.29 RAISE statement (EXCEPTION ec-name / identifier object; nonfatal-unhandled acts as CONTINUE)
- §14.9.33 RESUME statement (NEXT STATEMENT / procedure-name; GLOBAL declarative ≡ CONTINUE)
- §14.9.39 SET statement (GR6 condition-name TO TRUE → first VALUE literal; switch ON/OFF) ; §14.9.49 USE declaratives (GLOBAL; AFTER EXCEPTION/ERROR; ON file/INPUT/OUTPUT/I-O/EXTEND)
- §9.1.12 input-output exception processing (applicable exception processing statements; first-match selection); §9.1.13 I-O status → EC-I-O-* mapping (1x AT-END, 2x INVALID-KEY, 3x/4x/7x fatal, etc.)
- TURN compiler directive (§4970/§5000-§5024 — default EC-ALL OFF; EC-ALL/level-2 expansion; WITH LOCATION; EC-I-O-WARNING explicit-only); PROPAGATE directive (§4808)
- §15.28–15.33 — EXCEPTION-FILE/-LOCATION/-STATEMENT/-STATUS intrinsic functions

## Resolved questions (settled in `COBOLNET_DESIGN.md` §18 — answers recorded inline per the keep-deep-dives-current rule)

- SETTLED (§18.16): EC checking ships OFF by default (NIST-faithful, fast, ISO §5000), enabled only by >>TURN/phrases; the conformance corpus drives the EC-on paths.
- SETTLED (§18.16): an unhandled fatal EC terminates the run unit with a diagnostic + a nonzero exit (the ISO §14.6.13.1.3 implementor choice).
- PROPAGATE (§4808) and the exception-checking PERFORM WHEN (§14.9.28) are COBOL-2023 constructs (VCR row 79) — in scope for full-2023 (G1: diagnosed at --std=85|2002|2014); they land after the declarative/phrase path (the seams — declarative-returns-ResumeAction, runtime ExceptionState — admit them without rework).
- SETTLED (§18.17): VALIDATE / EC-VALIDATE is implemented minimally for the conformance corpus and flagged obsolete (2023 Table 13; VCR row 125).
- Program collating sequence for alphanumeric comparisons / HIGH-VALUE/LOW-VALUE remap is designed but deferred until the CobolNet collating subsystem lands; the API seam CobolString.Compare(a,b,weights?) is fixed now — confirm that seam is acceptable so call sites never change.
