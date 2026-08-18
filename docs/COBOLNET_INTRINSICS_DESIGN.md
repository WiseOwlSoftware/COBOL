# COBOL.NET — Intrinsics, Special Registers & Misc (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §12; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.
>
> **SPINE 1 — IMPLEMENTED (every ISO §15 function LIVE — PHASE-11 drove the `Deferred` backlog to ZERO).**
> `IntrinsicCatalog` (the complete §15 2023 table with D8 windows; every row now binds `Runtime` [a runtime
> body], `Fold` [a compile-time fold — LENGTH/BYTE-LENGTH/the ALGEBRAIC family/WHEN-COMPILED], or `Unsupported`
> [the A.4.9 locale module — documented non-support, §4.2.7, → COBOLNET1518]; the `Deferred` enum case remains
> only as the renderer's never-hit backstop), `BoundIntrinsicCall`,
> `Binding/Procedure/Verbs/IntrinsicBinder.cs` (P7 Step 12: FUNCTION arguments are REAL parse trees —
> `functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)?`, each argument a §8.4.3.2 SR8 shape
> bound through the ONE `ExpressionBinder.BindExpr`; the keyword-omitted form re-parses its captured text through
> the SAME `functionArgList` rule via `FunctionArgFragment`; table(ALL) expansion, MAX/MIN resolution, LENGTH
> fold, the §15.68.3 r3 currency injection), `IntrinsicRenderer` (ONE instance channel — numeric + string; the
> static twin is deleted; arguments render through the ONE `NumericRenderer`, `ReceiverContext.None` in the
> string channel), runtime `CobolIntrinsics`/`CobolDate`. The 1989 Intrinsic Function Module (42
> functions) is fully implemented — all 42 NIST IF programs byte-match. **Two design points worth flagging:**
> (1) `BoundIntrinsicCall.Args` is `IReadOnlyList<BoundOperand>`, NOT `BoundExpr` — the string-argument
> functions (NUMVAL, ORD, LOWER-CASE …) take alphanumeric operands the numeric expression tree cannot represent;
> numeric argument expressions wrap as `BoundComputedOperand`. (2) The node carries a binder-set `Collate` flag
> for CHAR/ORD under a NON-identity PROGRAM COLLATING SEQUENCE (§15.15.4 r2/§15.70.4) so the backend passes its
> `__COLLATE` weights only when the field exists (hazard H5) — backend-neutral semantics, not a rendered
> fragment. WHEN-COMPILED is realized as the spec-correct COMPILE-TIME constant (§15.99.3 r2) baked into the
> generated source via the injectable `IntrinsicBinder.CompileClock` (D6).

## Summary

DESIGN FOR THE COBOL.NET (greenfield COBOL→idiomatic-C#/Roslyn) target. The subsystem has TWO graded spines plus several smaller surfaces; everything is designed to fit the backend-neutral bound-tree pipeline (SSOT §1.1: ANTLR parse tree → binder → ONE bound tree → `ICodeGenBackend`, `--backend roslyn|cil`; Roslyn C#-source is the primary backend, Cecil/CIL is future-additive with its own private lowering — NO shared lowered IR). The binder resolves every FUNCTION call to a structured `BoundIntrinsicCall` (the resolved `IntrinsicSig` + typed bound arguments — never a pre-rendered C# fragment); backends only RENDER it, routing through the singular `CobolNum.Store(value,scale,profile)` and `CobolString.Store(value,width)` paths and the native substrate (fixed-point = `long` holding the UNSCALED value; float = `double`; alphanumeric = UTF-16 `string`; bool = `bool`; no byte[], no software `decimal`/`BigInteger` by default).

SPINE 1 — INTRINSIC CATALOG (the graded deliverable). Reject porting the legacy `decimal`-typed signatures. Instead build ONE declarative table `IntrinsicCatalog`: name → {ISO §15.2 function-type, result category, arity model (fixed N / optional trailing / variadic), per-arg category, binding = compile-time-fold | runtime-method}. §15.2 gives exactly six types — alphanumeric / boolean / national / numeric / INTEGER / index — and THAT classification IS the return-type column, mapped to the native substrate: integer-function→`long` (`Int128` past 18 digits, e.g. FACTORIAL); floating-point math (SQRT, SIN/COS/TAN/ASIN/ACOS/ATAN, LOG/LOG10/EXP/EXP10, PI, STANDARD-DEVIATION, VARIANCE, ANNUITY, PRESENT-VALUE, RANDOM)→`double`; exact numeric (SUM, MEAN, MEDIAN, MIDRANGE, RANGE, MAX/MIN-numeric, MOD, REM, INTEGER, INTEGER-PART, FRACTION-PART, ABS, SIGN, NUMVAL, NUMVAL-C, NUMVAL-F)→`NumX` (unscaled `long`+scale) so it flows straight into `CobolNum.Store` with the receiver's ROUNDED; alphanumeric/national (UPPER-CASE, LOWER-CASE, REVERSE, TRIM, CONCAT [§15.18, 2023 — CONCATENATE is NOT an ISO function at any edition and is intentionally absent, PHASE-11 Step 7], SUBSTITUTE, CHAR, CHAR-NATIONAL, NATIONAL-OF, DISPLAY-OF, the date-string functions)→`string`; boolean→`bool` (BOOLEAN-OF-INTEGER on the D-B1 '0'/'1' substrate, PHASE-11 Step 2). Runtime home = `CobolNet.Runtime.CobolIntrinsics` (numeric/string/financial/conversion families) + `CobolNet.Runtime.CobolDate` (date/time). Mine the legacy `IntrinsicFunctions.cs` for BEHAVIOR only (NaN/out-of-domain→EC-ARGUMENT-FUNCTION default result; ORD-MAX/ORD-MIN tie = first; date algorithms; NUMVAL parsing), not for types. The catalog is consulted in the BINDER at the function-call operand cell (`IntrinsicBinder`, the `BindPrimary` hook) and at the DISPLAY/MOVE-source/condition-operand binding paths — it is the single source of result-category truth, replacing the legacy ad-hoc `AlphanumericFunctions` HashSet + special-cases. MAX/MIN are category-polymorphic (resolve by arg category at the call site); `table(ALL)` expansion and variadic `params` port from legacy; FUNCTION LENGTH folds at compile time from PIC metadata.

SPINE 2 — SPECIAL-REGISTER REGISTRY (the other graded deliverable). Model every special register as a SYNTHESIZED `DataItem` (with a NumProfile-or-string type, a storage class, read-only/compile-time-fold flags, and a D8 edition-availability window from `docs/VERSION_CHANGE_REFERENCE.md`) registered in `DataBinder.ByName`, so a register read/store reuses the EXACT SAME `CobolNum`/`CobolString` paths as any data item — zero special-case in the verb emitters. RETURN-CODE/TALLY/SORT-RETURN→static `long`; WHEN-COMPILED→compile-time constant string (timestamp captured at compile, injectable for determinism); LENGTH OF/BYTE-LENGTH→folded `long` byte-size from PIC+USAGE; ADDRESS OF→`ManagedPointer`; LINAGE-COUNTER/LINE-COUNTER/PAGE-COUNTER and XML-*/JSON-* register NAMES are reserved by the registry but attach to their (scope-flagged) subsystems.

SMALLER SURFACES (one approach each, dependency named): figurative constants modeled as a context-materialized sentinel resolved at each use site against the receiver category+width per §8.3.3.6 (ZERO→`0L` or `'0'`-fill; SPACE→space-fill; QUOTE→`'"'`; ALL lit + symbolic-char→repeat-to-width); HIGH/LOW-VALUE on a UTF-16 string = U+00FF/U+0000 (alphanumeric) and U+FFFF/U+0000 (national), which preserves ordinal ordering through `CobolString.Compare` for the corpus (full ALPHABET/CODE-SET fidelity sits on the G6 char↔byte boundary — G6-core is implemented; the custom-ALPHABET/CODE-SET collating tail is still open). INITIALIZE = compile-time tree-walk to per-elementary typed stores (default/VALUE/REPLACING; FILLER skipped). SET = dispatch by target kind (index→long, pointer→ManagedPointer/NULL, switch→bool, cond-name TO TRUE→store the 88's first VALUE) — the 88-level binding (`Condition88`/`BoundSetConditions`) and OCCURS INDEXED BY index fields are bound in DataBinder; only level-66 and the pointer/switch arms remain open. ACCEPT/DISPLAY system sources = a `CobolSystem` runtime with an INJECTABLE clock (DATE/DAY/TIME/DAY-OF-WEEK/YYYYMMDD/YYYYDDD) + console UPON SYSOUT/SYSERR/mnemonic. ALPHABET/CLASS/CURRENCY/DECIMAL-POINT IS COMMA = a SPECIAL-NAMES config object threaded into emit (mostly compile-time). SCREEN SECTION, REPORT WRITER, JSON/XML GENERATE/PARSE are scope-flagged big subsystems — designed only to the seam (reserve their register names, one-paragraph deferral each).

## Decisions

### D1. Do NOT port the legacy decimal-typed intrinsic signatures; type every intrinsic by its ISO §15.2 declared function-type mapped to the native substrate (integer→long/Int128, floating-point math→double, exact numeric→NumX, alphanumeric/national→string, boolean→bool).

**Rationale.** The owner-locked model bans the software decimal substrate; §15.2 is the spec-authoritative classification and it maps cleanly onto the four native types CobolNet already speaks. Exact-numeric→NumX lets intrinsic results flow through the SINGLE CobolNum.Store(value,scale,profile) path with the receiver's ROUNDED, matching the singular-pattern doctrine.

**Rejected alternatives.** (a) Reuse legacy decimal bodies as-is — reintroduces banned software-decimal, less spec-faithful, and forces a decimal↔long boundary at every call site. (b) Make every intrinsic return double — loses base-10 exactness for SUM/MOD/INTEGER-PART and breaks ROUNDED determinism.

### D2. One declarative IntrinsicCatalog table (name → §15.2 type, RESULT-TYPE RULE, arity, arg categories, runtime-method-or-fold) is the single source of result-category truth, consulted during emission.

**Rationale.** Replaces the legacy ad-hoc AlphanumericFunctions HashSet + scattered special-cases with one table that the NumPrimary, DISPLAY, MOVE-source, and condition-operand paths all consult. Decades-sustainable: adding a function is one row. Fits the bound-tree pipeline (the binder consults the catalog once and produces a structured bound node; no lowered IR; backends only render — the dual-backend discipline, SSOT §1.1/§18 #23).

**⛔ THE TYPE COLUMN IS A RULE, NOT A SCALAR — and this decision was silently false for a year (fix-queue PB15).** Twenty §15 functions do not have a constant type: their own §15.x.1 clause carries a table whose left column is an ARGUMENT property and whose right column is the function type ("The type of this function depends on the type of argument-1 as follows"). A scalar column cannot express that, so each such function needed a hand-written exception in the binder — and only five ever got one, in **two separate name lists** (CA25's `RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"`, V54's `Name is "MAX" or "MIN"`). The other ten stayed mislabelled, every one a silent under-rejection: a national result labelled alphanumeric passes the §14.9.25.3 Table-16 MOVE guard instead of being rejected by it. **Three mechanisms decided one property, so "the single source of result-category truth" named a file that was not one.**

The row now carries `IntrinsicResultRule` — seven spec-derived shapes covering all twenty functions (`FollowsArgument1` alone covers ten) — and `IntrinsicResultType.Resolve` is the ONE reader, called from the generic bind path **and from every bespoke one**. That last clause is load-bearing: TRIM and SUBSTITUTE parse phrase keywords and build their own bound node, so a correct catalog row left them wrong until their construction sites read the rule too — the two-arm dispatch in its silent form, with the fix present and every test green.

**Resolve returns the §15.2 TYPE, not the category.** `ResultCategory` folds INTEGER, NUMERIC and INDEX into `PicCategory.Numeric` — correct for every consumer that exists today, and precisely why resolving to a category would have made the four integer-following rules unrepresentable, i.e. dead members reading as coverage. Resolving the type keeps D1's §15.2 classification intact and leaves the answer already correct for the first consumer of integer-ness that arrives.

**`IntrinsicResultTypeDriftTests` re-derives the population from `specs/ISO_COBOL.md` itself** — the structural key is a markdown table in §15.x.1 whose header names the "Function type" column — so a function whose clause grows a table, or a new function that has one, fails the build until its row declares a rule. Without that, this decision decays exactly the way it decayed the first time: silently, because a name list cannot say what it omits.

**Rejected alternatives.** Per-call-site if/else chains (the legacy ad-hoc approach) — violates the refactor-first/canonical-dispatch doctrine and drifts out of sync; PB15 is the measured proof, since that is what the two name lists were. A class-per-function hierarchy — over-engineered for a static catalog. Keying the drift test on the clauses' PROSE rather than on the table — the wordings differ ("depends on the argument type" §15.38.1, "depends on the type of argument-1" §15.39.1, "depends upon the argument types" §15.18.1) and a prose key found ten of twenty, a dead guard inside the guard against a dead lookup.

### D3. Model every special register as a synthesized DataItem registered in DataBinder.ByName (with type, storage class, read-only/fold flags).

**Rationale.** A register read/store then reuses the EXACT CobolNum/CobolString paths every data item uses — zero special-case in the verb emitters. RETURN-CODE/TALLY are static long fields; WHEN-COMPILED/LENGTH OF fold to constants; ADDRESS OF→ManagedPointer.

**Rejected alternatives.** A parallel special-register subsystem with its own read/emit logic — duplicates the data path (two mechanisms for one job, the banned anti-pattern).

### D4. Figurative constants are context-materialized sentinels resolved at the use site against the receiver's category+width (§8.3.3.6), never fixed strings/values of their own.

**Rationale.** A figurative has no intrinsic length/type; ZERO is 0L to a numeric receiver but '0'-fill to alphanumeric; ALL lit + symbolic-char repeat-to-width. Materializing at the site is the only spec-correct model.

**Rejected alternatives.** Pre-expanding a figurative to a fixed-width string at parse time — wrong, since width is only known at the receiver, and numeric vs alphanumeric meaning diverges.

### D5. HIGH-VALUE/LOW-VALUE on the UTF-16 string substrate = U+00FF/U+0000 (alphanumeric) and U+FFFF/U+0000 (national).

**Rationale.** §8.3.3.6 defines them as the highest/lowest ordinal in the collating sequence; U+00FF/U+0000 are the single-octet ordinal extremes and preserve ASCII/Latin-1 ordering through the ordinal CobolString.Compare, which is what the NIST corpus exercises.

**Rejected alternatives.** U+FFFF for alphanumeric HIGH-VALUE — would over-shoot single-byte ordering and mis-collate against Latin-1 data. Storing a byte sentinel — reintroduces the byte substrate. Full custom-ALPHABET fidelity now — sits on the char↔byte boundary the architecture defers to G6.

### D6. Date/time/WHEN-COMPILED nondeterminism handled by an injectable clock (CobolSystem.Clock) in the runtime.

**Rationale.** CURRENT-DATE, ACCEPT FROM DATE/TIME, and WHEN-COMPILED are nondeterministic; an injectable clock makes conformance .out comparison reproducible (the legacy already needs this) and keeps the generated C# testable.

**Rejected alternatives.** Direct DateTime.Now calls — untestable, breaks golden-output conformance.

### D7. Compute LENGTH OF / FUNCTION BYTE-LENGTH from compile-time PIC+USAGE byte-size metadata kept in PicInfo; keep FUNCTION LENGTH (character positions, §15.50) separate from LENGTH OF/BYTE-LENGTH (byte size).

**Rationale.** The byteless data model stores no byte count, but byte-size is a pure function of PIC+USAGE known at compile time, so it folds to a constant. FUNCTION LENGTH counts character positions (already folded in legacy BindLength); LENGTH OF/BYTE-LENGTH count bytes — for COMP/COMP-3 these differ and must not be conflated.

**Rejected alternatives.** Treating LENGTH OF == FUNCTION LENGTH — wrong for COMP/COMP-3/national where byte size != char positions. Storing a runtime byte length — reintroduces byte bookkeeping.

### D8. Per-edition gating (G1): availability is a catalog/registry COLUMN (IntroducedIn/RemovedIn from `docs/VERSION_CHANGE_REFERENCE.md`), enforced by the binder against `--std` with a specific diagnostic — the co-equal obligation to implementing the behavior.

**Rationale.** The intrinsic library is one of the most edition-varying surfaces in the standard: the FUNCTION facility postdates base COBOL-85, and 2002/2014/2023 each add (and occasionally remove or alter) functions, special registers, and figuratives (e.g. NULL). The mission is four compilers in one executable (ISO COBOL 1985 / 2002 / 2014 / 2023). Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed) — "FUNCTION X requires COBOL-yyyy; current --std is yyyy". Tests (NIST etc.) only VERIFY; they never SCOPE. Window data comes from `docs/VERSION_CHANGE_REFERENCE.md`, the 130-row edition-change checklist (2002→2023 deltas ONLY — it has NO 85→2002 rows; derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog). The version test matrix (`docs/VERSION_TEST_MATRIX_DESIGN.md`, the (construct × edition) matrix; Phase 0 done) gains a (function × edition) row per catalog entry: compiles+behaves inside the window, rejected-with-diagnostic outside it.

**Rejected alternatives.** Per-edition grammar gating — availability is a binding-time property of an identical token stream, and the diagnostic must NAME the function and editions. A separate per-edition allowlist file — duplicates the catalog (two mechanisms for one job, the banned anti-pattern).

## C# mapping (the RoslynBackend rendering; the future CilBackend renders the same bound nodes via the same runtime entry points)

RETURN-TYPE DISCIPLINE (§15.2) — the spine, with examples:

  COBOL: COMPUTE R = FUNCTION SQRT(N)            [N is PIC 9(4), R is PIC 9(4)V99]
  C#:    R = CobolNum.Store((long)(System.Math.Sqrt((double)N) * 100m_pow), 2, _P_R);
         // SQRT is a floating-point function → double; result re-scaled into R's NumX at scale 2, stored via the
         // singular CobolNum.Store path. (The emitter wraps the double result back to NumX at the receiver scale.)

  COBOL: COMPUTE T = FUNCTION SUM(A B C)         [exact numeric function → NumX]
  C#:    T = CobolNum.Store(CobolIntrinsics.SumScaled(new[]{(A,0),(B,0),(C,0)}), 0, _P_T);
         // SUM is an EXACT numeric function: arguments flow as (unscaledLong, scale) pairs; result is NumX → Store.
         // NOT decimal. Scale alignment uses the same Align/Combine machinery the arithmetic verbs already use.

  COBOL: MOVE FUNCTION UPPER-CASE(NM) TO OUT     [alphanumeric function → string]
  C#:    OUT = CobolString.Store(CobolIntrinsics.UpperCase(NM), 10);

  COBOL: COMPUTE F = FUNCTION FACTORIAL(20)      [integer function, overflows long]
  C#:    F = CobolNum.Store((long)CobolIntrinsics.FactorialI128(20), 0, _P_F);
         // integer functions → long; FACTORIAL/large products compute in Int128, size-error past Int128 range.

THE CATALOG (declarative, in CobolNet.Binding):

  enum IntrinsicType { Alphanumeric, Boolean, National, Numeric, Integer, Index }  // ISO §15.2
  enum ArgArity     { Fixed, OptionalTrailing, Variadic }                          // §15.3
  enum IntrinsicBind { Fold, Runtime }                                             // compile-time vs runtime call
  readonly record struct IntrinsicSig(string Name, IntrinsicType Type, PicCategory ResultCategory,
      ArgArity Arity, int MinArgs, int? MaxArgs, string RuntimeMethod, IntrinsicBind Bind,
      int IntroducedIn, int? RemovedIn);  // edition window (85/2002/2014/2023) from docs/VERSION_CHANGE_REFERENCE.md;
      // the binder rejects a FUNCTION outside its window for Options.DialectLevel with a per-edition diagnostic (D8)
  static class IntrinsicCatalog {
      static readonly Dictionary<string,IntrinsicSig> _t = new(StringComparer.OrdinalIgnoreCase){ ... ~95 rows ... };
      public static bool TryGet(string name, out IntrinsicSig sig) => _t.TryGetValue(name, out sig);
      // category-polymorphic resolution for MAX/MIN at the call site:
      public static IntrinsicSig Resolve(string name, IReadOnlyList<PicCategory> argCats) { ... }
  }

BINDER HOOK — the function-call operand cell binds to a structured node the
backends render (ALL semantics — availability per D8, arity/category checks, table(ALL) expansion, MAX/MIN resolution —
happen in the binder; the sketch below is the RoslynBackend RENDERING; a future CilBackend renders the SAME node as a
direct call to the same `CobolNet.Runtime` method):

  // Args are BoundOperand (not BoundExpr — string-argument functions take alphanumeric operands, see the status
  // banner) and the binder sets Collate for CHAR/ORD under a non-identity PCS:
  sealed record BoundIntrinsicCall(IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) : BoundExpr;
  // RoslynBackend, numeric result — the function-call operand cell returns:
  if (pe.functionCall() is { } fc) return EmitIntrinsicNum(fc);   // → NumX (numeric/integer/exact)
  // in ReadAsString / SendAsString / OperandAsString: if functionCall && result is alphanumeric → EmitIntrinsicStr(fc)

  private NumX EmitIntrinsicNum(Core.FunctionCallContext fc){
     string name = fc.functionName().GetText();
     if (name.Equals("LENGTH",IC)) return new NumX($"{FoldLength(arg0)}L", 0);     // §15.50 compile-time fold
     var sig = IntrinsicCatalog.Get(name);
     var args = BindIntrinsicArgs(fc, sig);                                         // table(ALL) expanded, typed per sig
     return sig.Type switch {
        IntrinsicType.Integer  => new NumX($"CobolIntrinsics.{sig.RuntimeMethod}({IntArgs(args)})", 0),
        IntrinsicType.Numeric when IsFloatFn(sig) =>
            new NumX($"(long)(CobolIntrinsics.{sig.RuntimeMethod}({DblArgs(args)}) * {Pow10(_targetScale)})", _targetScale),
        IntrinsicType.Numeric  => /* exact: emit a CobolIntrinsics.*Scaled returning unscaled long at a known scale */,
        _ => throw ... };
  }

SPECIAL REGISTERS — synthesized DataItems (in DataBinder bootstrap), reusing the normal read/store paths:

  // RETURN-CODE: a real static long field + a synthesized numeric DataItem in ByName, NumProfile S9(4).
  private static long RETURN_CODE = 0L;        // also written by GOBACK/STOP RUN and read as $? exit code (cross-dep: control-flow/CALL)
  // WHEN-COMPILED: compile-time-folded constant string item (§15.99.3 r1 — the 21-char YYYYMMDDHHMMSShh±hhmm
  // form, same structure as CURRENT-DATE, rendered by the ONE CobolDate.Format21 formatter).
  private const string WHEN_COMPILED = "2026060812000000+0000";  // captured at compile (injectable for determinism)
  // LENGTH OF X / FUNCTION BYTE-LENGTH(X): folded long from PicByteSize(item):
  long _len_of_X = 18;   // PIC byte-size computed from PIC+USAGE metadata (NOT stored anywhere at runtime)
  // ADDRESS OF X → ManagedPointer; LINAGE-COUNTER/LINE-COUNTER/PAGE-COUNTER → owned by file/RW subsystem.

FIGURATIVE CONSTANTS — context-materialized at the use site (CobolNet.Binding.Figurative):

  enum Fig { None, Zero, Space, HighValue, LowValue, Quote, Null, AllLiteral }
  // MOVE ZERO TO X → CobolString.Store(new string('0', width), width)  [alphanumeric receiver]
  // MOVE ZERO TO N → CobolNum.Store(0L, 0, _P_N)                        [numeric receiver]
  // MOVE SPACES TO X → CobolString.Store(new string(' ', width), width)
  // MOVE HIGH-VALUES TO X → CobolString.Store(new string('ÿ', width), width)   [alphanumeric]
  // MOVE LOW-VALUES  TO X → CobolString.Store(new string('\0', width), width)
  // MOVE ALL "AB" TO X(5) → CobolString.Store("ABABA", 5)              [§8.3.3.6 repeat-to-width-then-trim]

INITIALIZE — compile-time walk → typed stores (CobolNet.CodeGen):
  // INITIALIZE REC → for each elementary leaf: numeric→Store(0L,...); alphanumeric→Store(spaces,...)
  // INITIALIZE REC REPLACING NUMERIC BY 1 / TO VALUE → per-leaf override; OCCURS → emitted as a for-loop over the array.

SET — dispatch by target kind (CobolNet.CodeGen.EmitSet):
  // SET IDX TO 3            → IDX = 3;                       (index = long)
  // SET P TO ADDRESS OF X   → P = ManagedPointer.Of(ref X);
  // SET SW TO ON/OFF        → SW = true/false;              (mnemonic switch = bool)
  // SET FLAG-IS-OK TO TRUE  → FLAG = <the 88's first VALUE>; (cond-name; depends on G2-cont 88-level support)

ACCEPT/DISPLAY SYSTEM SOURCES — CobolNet.Runtime.CobolSystem with an injectable clock:
  static class CobolSystem { public static IClock Clock = SystemClock.Instance;
     public static string DateYYMMDD()  => Clock.Now.ToString("yyMMdd");
     public static string TimeHHMMSScc()=> Clock.Now.ToString("HHmmss") + Cs(Clock.Now);
     public static long DayOfWeek()     => ((long)Clock.Now.DayOfWeek + 6) % 7 + 1;  // 1=Mon..7=Sun
     /* DAY (yyDDD), DateYYYYMMDD, DayYYYYDDD likewise */ }
  // ACCEPT X FROM DATE  → X = CobolString.Store(CobolSystem.DateYYMMDD(), width)  (or numeric Store if X is numeric)
  // DISPLAY A B UPON SYSERR → System.Console.Error.WriteLine(imageA + imageB)

## Hard problems

### Intrinsic return type vs the native scaled-long substrate: SUM/INTEGER-PART must stay exact base-10, SQRT/trig are inherently floating, FACTORIAL overflows long.

Three-way split by §15.2 type: exact-numeric intrinsics return NumX (unscaled long + scale) and route through CobolNum.Store with the receiver's scale+ROUNDED; floating-point-math intrinsics compute in double then the emitter rescales the double result back into the receiver's NumX scale; integer intrinsics return long, escalating to Int128 for FACTORIAL/large products with EC-ARGUMENT-FUNCTION/size-error past Int128 range.

### HIGH-VALUE/LOW-VALUE/custom ALPHABET on a UTF-16 string substrate — the spec defines them by collating-sequence ordinal, but there is no byte image.

Map HIGH/LOW-VALUE to U+00FF/U+0000 (alphanumeric) and U+FFFF/U+0000 (national) so ordinal CobolString.Compare reproduces ASCII/Latin-1 ordering for the corpus. Custom ALPHABET/collating, CODE-SET round-trip, and REDEFINES-over-bytes are explicitly the char↔byte boundary deferred to G6; SPECIAL-NAMES ALPHABET config is threaded into a collating table that feeds Compare/CHAR/ORD/figuratives when G6 lands.

### LENGTH OF / BYTE-LENGTH want a byte count that the byteless model never stores.

Keep a compile-time PicByteSize derived from PIC+USAGE in PicInfo (DISPLAY=char count; COMP=1/2/4/8 by digits; COMP-3=digits/2+1; national=2×chars) and fold LENGTH OF/BYTE-LENGTH to that constant. Keep it strictly separate from FUNCTION LENGTH (§15.50 character positions).

### RETURN-CODE has a triple role: a readable/writable special register, the process exit code, and the conduit for CALL RETURNING / GOBACK.

Model RETURN-CODE as a synthesized static long DataItem (normal read/store), have GOBACK/STOP RUN read it for the process exit code, and document the cross-dependency with the control-flow/CALL subsystem so they wire writes into the same field (one canonical location, no second mechanism).

### SET cond-name TO TRUE, SET index, and class/condition-name conditions depend on data-division features (88-level binding + OCCURS INDEXED BY are bound in DataBinder; level 66 is still skipped).

The SET dispatch slots (index→long, cond-name→store the 88's first VALUE into its parent, switch→bool, pointer→ManagedPointer) rely on the 88-level binding and INDEXED BY dependencies (`Condition88`, DataBinder index-name registration), so the cond-name/index arms are implemented directly; only the level-66 and switch-mnemonic dependencies remain stubbed — the catalog/registry spines do not depend on this.

### TWENTY functions are category-polymorphic — the result TYPE follows the arguments, per the §15.x.1 result-type table each one carries.

⚠ **This section used to name MAX/MIN only, and that framing is what let the defect spread** (fix-queue PB15): treating it as a two-function quirk produced a two-function fix, twice. It is a §15-wide rule shape. The catalog row carries `IntrinsicResultRule` and `IntrinsicResultType.Resolve` reads it at bind time; **`IntrinsicResultTypeDriftTests` derives the population from the spec**, so this list is documentation, never the register.

The seven shapes, and the functions each covers:

| Rule | Functions | The table it implements |
|---|---|---|
| `FollowsArgument1` | BASECONVERT §15.12 · FORMATTED-CURRENT-DATE §15.38 · FORMATTED-DATE §15.39 · FORMATTED-DATETIME §15.40 · FORMATTED-TIME §15.41 · LOWER-CASE §15.57 · REVERSE §15.78 · SUBSTITUTE §15.87 · TRIM §15.96 · UPPER-CASE §15.97 | Alphabetic→Alphanumeric · Alphanumeric→Alphanumeric · National→National |
| `IntegerFollowsArgument1` | ABS §15.7 · HIGHEST-ALGEBRAIC §15.43 · LOWEST-ALGEBRAIC §15.58 · SMALLEST-ALGEBRAIC §15.83 | Integer→Integer, every other admitted row→Numeric |
| `IntegerFollowsAllArguments` | RANGE §15.76 · SUM §15.88 | All arguments integer→Integer · otherwise Numeric |
| `FollowsUniformArguments` | MAX §15.59 · MIN §15.63 | the six-row table below |
| `FollowsConcatArguments` | CONCAT §15.18 | keyed on class AND usage |
| `FollowsDestinationFormat` | CONVERT §15.19 | keyed on the destination KEYWORDS, not an argument |
| `Fixed` | everything else | §15.x.1 states one type, or §15.2/§15.6 does |

**MAX/MIN in full** (§15.59.1/§15.63.1, over the uniform §15.59.3/§15.63.3 r2 argument list): alphabetic or alphanumeric → an ALPHANUMERIC result (the selected string); NATIONAL → a NATIONAL result (the selected national string — NOT alphanumeric); INDEX → an INDEX result; all-integer → an INTEGER result; otherwise → NUMERIC. The size of an alphanumeric or national result is the size of the selected argument-1 (§15.59.4/§15.63.4 r3). ⚠ The INDEX row is **not** a `PicCategory` — an index item is PICTURE-less and this model carries it as `PicInfo.IndexItem`, category Numeric with usage Index, so a category-keyed test would route it to the numeric arm and, since its scale is 0, answer INTEGER: a plausible wrong type rather than a visible failure.

**ORD-MAX §15.71 / ORD-MIN §15.72 are `Fixed` and share none of this.** They always return an integer ordinal — their §15.71.1/§15.72.1 clauses carry no result-type table at all — but they DO dispatch their comparison to the string body by the same argument category. That `RuntimeMethod` choice is deliberately kept separate from the result-type resolution in the binder: folding the two together is what made the old code read as if MAX's type and MAX's comparison body were one decision.

**The FORMATTED-\* family is the row most easily read backwards.** Their type follows argument-1 — the FORMAT literal (§15.38.3 r1 and siblings admit "a national or alphanumeric literal") — so `FUNCTION FORMATTED-DATE(N"YYYYMMDD" D)` is a NATIONAL function even though what it renders is an integer date.

### FUNCTION argument shapes: table(ALL) expansion, optional trailing args (e.g. RANDOM seed), and variadic statistical functions — REAL argument parse trees (P7 Step 12).

FUNCTION arguments parse through the grammar (`functionArgList` — the lexer suppresses the SUBSCRIPT push after
`FUNCTION functionName`, keeps the §8.3.5 comma/semicolon separator as `FNARG_SEPARATOR`, and lexes
sign-adjacent-after-separator literals as `SIGNED_*` per §8.7.1/§8.3.3.3.2, so `MAX(A -4)` is two arguments and
`MAX(A - 4)` one). Each argument binds via `BindArgOperand` — non-numeric literals stay categorized literal
operands, a sole data reference stays a `BoundFieldOperand`, anything arithmetic goes through the ONE
`ExpressionBinder.BindExpr` and wraps as `BoundComputedOperand` — with `TryExpandAll` expanding a table(ALL)
argument (its subscript capture is still SUBSCRIPT-mode tokens — the D10/PHASE-15 deferral) to per-occurrence
operands, driven by the catalog's arity model (Fixed/OptionalTrailing/Variadic). The five phrase-keyword functions
(TRIM/FIND-STRING/SUBSTITUTE/CONVERT/MODULE-NAME) classify bare-word arguments by name (`KeywordWordOf`) and each
walks its §15.x.2 general format BY POSITION, never as an order-free keyword harvest — CONVERT's slot 0 is ALWAYS
argument-1 and binds as an operand, so a data item named one of the §8.10 context-sensitive words (NAT/ANUM/HEX/
BYTE, reserved only where CONVERT's format permits them) stays a legal argument-1, and an operand after the format
words is rejected (PB59 / FMT-15.19.2); the §8.9
reserved-word funnel skips a BARE argument word (a §15 phrase-word position is not a provable user-word slot).
The keyword-omitted reference form (D2 — no grammar alternative) re-parses its captured argument text through the
SAME `functionArgList` rule (`Frontend.Parsing.FunctionArgFragment`, lexer primed via `PrimeFunctionArgs`), and
`UdfBinder.UdfBindCall` binds its arguments through the same `BindArgOperand` — ONE argument pipeline; the former
hand-rolled per-segment recursive-descent parser is deleted.

**The §15.3 argument-rule SCREEN (`IntrinsicArgumentRules`, driven from `IntrinsicBinder.CheckArgumentClasses` after
arity and before every per-function arm) is a per-position SCHEMA, not a class column** (PB1 → PB12 → PB31 →
PB35 → PB58): each catalogued function has an `ArgSchema` — per-position `ArgRule`s plus a variadic tail — whose
class KIND (`'n'` numeric, `'i'` §15.3 type-6 integer, `'s'` the string family, `'b'` boolean, `'c'` CONCAT's
all-but-index/object/pointer, `'p'` the MAX/MIN-family negative list, `' '` no arguments) is one predicate among
several: the position may also carry `ArgPredicate`s — `MinWidth`/`ExactWidth` in character positions (§15.57.3 /
§15.97.3 / §15.78.3 r1's "at least one character position", §15.70.3 r1's and §15.96.3 r2's "one character",
decided only on a STATIC width), `DataItemOrLiteralOnly` (§15.37.3 r3's "an integer data item or integer literal",
narrower than type 6 — an expression or nested function is barred outright), `NotStrongGroup` (§15.59.3 /
§15.63.3 / §15.71.3 / §15.72.3 r1's "nor shall it be a strongly-typed group item"), and the zero-length-LITERAL
clause — and the schema a `CrossArgRule` (`AllSameClass` for MAX/MIN/ORD-MAX/ORD-MIN r2/r3, `MatchArgument1` for
FIND-STRING, NUMVAL-C/TEST-NUMVAL-C, INTEGER-OF-FORMATTED-DATE, SECONDS-FROM-FORMATTED-TIME, TEST-FORMATTED-DATETIME,
TRIM, SUBSTITUTE). `IntegerViolation` decides §15.3 type 6 for a data item (declared scale), a nested numeric function
(§8.4.3.2.3 SR11) and a numeric literal with a nonzero fraction; an arithmetic expression fails open ("always
results in an integer value" is not syntactically distinguishable — §4.2.2 discretion). Rules a class kind cannot
carry at all live beside the schema on the same axis (`StaticUsageOf` — CONCAT's §15.18.3 r2/r3 usage halves in
`CheckConcatArgs`, BASECONVERT's and CONVERT's usage screens) or in the function's own binder (SUBSTITUTE's per-pair
§15.87.3 r3 widths, INTEGER-OF-FORMATTED-DATE's "shall be a DATA ITEM" half of §15.48.3 r3, the date/time FORMAT
literal arm — which admits `ALL "…"` per §8.3.3.6.3 SR1). Every catalogued function has a row in `Verified` or a
cited reason in `DeliberatelyUnscreened` (`IntrinsicArgumentClassDriftTests.EveryCataloguedFunction_HasARow`), so a
function can no longer be silently unscreened; the screen is fail-open by construction (an undecidable operand is
never rejected) and its leniency is `--permissive`'s. Pinned by `pb58_argument_predicates_legal` (the shapes that
must NOT be rejected) and the `pb58-*` negatives (one per rule instance).

## Edge cases

- FUNCTION LENGTH of a reference-modified operand x(s:l) is runtime (= l), not a compile-time constant — fold only fixed operands; emit a runtime expression for x(s:l) and x(s:) (defined-size − s + 1).
- FUNCTION LENGTH of a variable-length group (OCCURS DEPENDING ON) uses the current depending value (§15.50.4) — runtime, not the max.
- MOVE ZERO to a numeric receiver = 0L; MOVE ZERO to an alphanumeric receiver = a '0'-filled string of the receiver width — same token, different materialization (must branch on receiver category).
- ALL literal repeat-to-width-then-truncate (§8.3.3.6): ALL "AB" into a PIC X(5) = "ABABA" (repeat until ≥ width, truncate from the right), applied before any JUSTIFIED.
- NUMVAL (§15.67), NUMVAL-C (§15.68), NUMVAL-F (§15.69) parse the spec-defined argument-1 content grammars. NUMVAL admits an optional leading sign (`+`/`-`) OR a trailing sign / `CR` / `DB`, a run of digits with an optional decimal separator; leading and trailing spaces are ignored, and embedded spaces are ignored only where they precede the first digit; a `CR`, `DB`, or minus sign makes the result negative (§15.67.3/§15.67.4). NUMVAL-C additionally admits a currency string and comma grouping separators — the currency string is argument-2 when supplied, else the SPECIAL-NAMES CURRENCY SIGN or the default currency sign (§15.68.3 r3) — and the returned value ignores the currency string and any grouping separators preceding the decimal separator (§15.68.4 r2). NUMVAL-F additionally admits an `E±n` exponent (one-to-four-digit exponent, §15.69.3). The period is the decimal separator; under DECIMAL-POINT IS COMMA the comma is the decimal separator (and, for NUMVAL-C, the period becomes the grouping separator). Under native arithmetic the total number of digits shall not exceed 31 (§15.67.3 r3 / §15.68.3 r6 / §15.69.3 r2). Each family's TEST- validator and value function are projections of ONE positional scan (`NvScan` / `NvfScan`, PB60) — the value path never pre-normalizes. Under the STANDARD modes the value is the scan lifted EXACTLY to the SDIDI at the parsed scale (`NumvalDec/NumvalCDec/NumvalFDec`, §15.4.1 + §15.67.4 r1 / §15.68.4 r1 / §15.69.4 r3); under NATIVE arithmetic NUMVAL/NUMVAL-C carry the item-92 working scale, and NUMVAL-F — whose native value §15.69.4 r2 makes an approximation — follows the float family: binary64 (`NumvalFDouble`) in a receiver-less or float-receiver context, the exact Int128 parse for a fixed arithmetic receiver or a MOVE sender (`ReceiverContext.MoveSender`). CONFORMANCE.md item 92 records the determination.
- BASECONVERT (§15.12) and CONVERT's HEX source (§15.19.3 r4) trim ONLY the trailing fixed-width image pad, and only the SPACE character: a LEADING space is content and reaches the digit screen (EC-ARGUMENT-FUNCTION), and a DIGITLESS argument (all spaces) raises rather than fabricating "0"/empty — the guard counts digits consumed, never the accumulator value (PB59). BASECONVERT's digits are `0-9` + the UPPERCASE `A-F` only (§15.12.3 r2 names "the basic-letters A to F"; §8.1.3.1 Table 1 makes basic letters the capitals — §8.1.3.2 GR3a's case-insensitivity governs source text, not runtime data); CONVERT's HEX source deliberately admits both cases (§15.19.3 r4 says "hexadecimal digits", the term the hex-literal format defines over both). CONVERT's §15.19.3 r1 zero-length screen has a RUNTIME arm at the top of `Convert` (ISO 8.5.4's runtime zero-length shapes — a bind screen only sees the literal). CONVERT's source-format keys what argument-1's STORAGE must hold, and the static half of each rule is its REPRESENTATION, screened at bind through the ONE shared `IntrinsicArgumentRules.StaticUsageOf` (the usage axis class cannot express — Table 2 collapses the pointer usages, and class NUMERIC spans display digits and binary words): r4 = display or national usage, r5 = display (its NOTE — "distinct from simply requiring the string to be of class alphanumeric" — admits numeric and edited DISPLAY items), r6 = national, r7 = any usage EXCEPT index/message-tag/object-reference/pointer/function-pointer/program-pointer; the VALUE halves (digit validity, coded-set membership) are the runtime screens' territory, and §15.12.3 r1's display-or-national rule reuses the same axis when BASECONVERT's screen set lands (PB59 family 5a). CONVERT's ANY source reads the item's RAW STORAGE bytes (§15.19.3 r7 — "it is not necessary for the contents to be valid according to the usage") through the ONE storage channel `OperandText.AsStorageImage` (char==byte; each leaf shape delivers its documented representation — CONFORMANCE.md items 205/208/209/215, D-N1 UTF-16BE via `CobolBits.NatBytes`, packed bits via `CobolBits.Pack`; float/COMP-5 leaves have no defined image and stage loud), and the §15.19.4 r2/r4 HEX legs hex THE SOURCE'S OWN BITS padded trailing with zero bits to a whole destination character — r2's 8-bit pad is materialized by the packed BIT image itself, r4's 16-bit pad is the odd-byte zero append (`"A" ANUM NAT HEX` = 4100; the destination keyword picks only the digit repertoire, never a re-encoding). Returned values are capped at the documented §15.4 maximum, 8,191 character positions (CONFORMANCE.md item 93), raising EC-ARGUMENT-FUNCTION past it.
- Out-of-domain math (ACOS/ASIN of |x|>1, SQRT of negative, LOG of ≤0) → EC-ARGUMENT-FUNCTION with a defined default result (legacy returns 0) rather than a .NET exception.
- FACTORIAL(n) for n≥21 overflows long and n large overflows Int128 → size-error / EC-ARGUMENT-FUNCTION (legacy clamped at 28! for decimal). The Int128 boundary: 33! ≈ 8.68e36 FITS (Int128.Max ≈ 1.70e38); **34! is the first overflow**; the runtime returns the EC default 0 for n > 33 or n < 0.
- RANDOM with vs without a seed argument — seeded form is deterministic per-call, unseeded shares one generator; the optional-trailing-arg arity must be modeled so the no-arg and one-arg forms both bind.
- FUNCTION WHEN-COMPILED (§15.99.3 r1) and CURRENT-DATE (§15.21.3) share the SAME 21-character structure: `YYYYMMDDHHMMSShh` in positions 1-16 followed by the UTC-offset subfield in positions 17-21 (position 17 = `+`/`−`/`0`, positions 18-19 = offset hours, positions 20-21 = offset minutes). WHEN-COMPILED carries the compilation timestamp, CURRENT-DATE the run-time clock; both are nondeterministic and both route through the injectable clock (the ONE `CobolDate.Format21` formatter), so conformance tests must inject a fixed clock.
- ADDRESS OF and SET … TO ADDRESS OF produce a ManagedPointer (managed ref), never a numeric address — and NULL/NULLS figurative sets it to the null ManagedPointer, semantically distinct from LOW-VALUE.
- DAY-OF-WEEK ordinal: COBOL is 1=Monday..7=Sunday, .NET DayOfWeek is 0=Sunday — the (+6)%7+1 remap is easy to get wrong.
- HIGH-VALUE used in a comparison vs in a MOVE: as a MOVE source it fills the receiver width with U+00FF; in a comparison it must compare as the highest ordinal — both fall out of the U+00FF mapping + ordinal Compare, but a national receiver needs U+FFFF.
- INITIALIZE skips FILLER by default but INITIALIZE … REPLACING / WITH FILLER changes that; REDEFINES subordinates and items with the wrong category for the REPLACING clause are skipped (§14.9.20.4 GR5).
- Nested intrinsic calls as arguments (e.g. ACOS(FUNCTION ACOS(D/D))) — natural grammar recursion since P7 Step 12 (`functionCall` is a `primaryExpression` alternative inside the argument's `arithmeticExpression`).

## ISO citations

- ISO/IEC 1989:2023 §15 Intrinsic functions (the function library)
- §15.2 Types of functions — alphanumeric / boolean / national / numeric / integer / index (THE return-type classification, the spine of the binding model)
- §15.3 Arguments — number of arguments may be zero, one, more, or variable (the arity model)
- §15.3.1 Format arguments to international date and time functions (date/time intrinsic formats)
- §15.3 — table(ALL) subscript ("When ALL is specified as a subscript, the effect is as if each table element … were specified"): each occurrence passed as a separate argument
- §15.50 FUNCTION LENGTH — number of character positions in argument-1 (distinct from LENGTH OF byte size)
- §15.50.4 — FUNCTION LENGTH of a variable-length (ODO) group uses the current depending value
- §8.3.3.6 Figurative constant values — ZERO/SPACE/QUOTE/HIGH-VALUE/LOW-VALUE/ALL/symbolic, including the repeat-to-width-then-truncate rule and the highest/lowest-ordinal definition of HIGH/LOW-VALUE
- §8.4.2 — data category of a PICTURE (drives the result-category and figurative materialization)
- §14.9.20 INITIALIZE — REPLACING / TO VALUE / DEFAULT / FILLER handling
- §14.9.25 MOVE — alphanumeric/numeric move rules reused by intrinsic results and figurative stores
- §14.9.42 STOP RUN / run-unit termination — RETURN-CODE as exit code
- §8.8.1 — arithmetic operates on the algebraic VALUE of operands (intrinsic numeric results align scales through the same engine)

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- Confirm the return-type discipline: should ANY intrinsic internal use decimal, or is long (exact)/double (float)/Int128 (wide) strictly sufficient? Recommendation: never decimal — exact-numeric via unscaled long+scale, float via double, wide via Int128. Owner already banned default decimal/BigInteger; confirming this extends to intrinsic INTERNALS.
- Confirm LENGTH OF / FUNCTION BYTE-LENGTH semantics under the byteless model: fold to a compile-time PIC+USAGE byte size kept in PicInfo, kept distinct from FUNCTION LENGTH (character positions). Is a synthesized compile-time byte-size table acceptable (vs declaring LENGTH OF unsupported until G6's byte boundary)?
- Confirm the HIGH-VALUE/LOW-VALUE character mapping: U+00FF/U+0000 (alphanumeric) and U+FFFF/U+0000 (national) for the corpus now, with full custom-ALPHABET collating deferred to G6. Acceptable, or does any near-term target require true byte 0xFF semantics sooner?
- Scope confirmation: SCREEN SECTION, REPORT WRITER, and JSON/XML GENERATE/PARSE are designed only to the seam (registers reserved, deferred to their own subsystems). Confirm they are NOT part of this subsystem's graded deliverable.
- RETURN-CODE ownership: this design models it as a synthesized static long; the control-flow/CALL subsystem must write it on CALL RETURNING/GOBACK and the process exit code must read it. Confirm that cross-subsystem contract so two designs do not each create a RETURN-CODE field.
