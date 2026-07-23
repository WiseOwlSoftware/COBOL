# COBOL.NET — Conditions & Exception Model (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §11; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## The EC-OO slice: the exception-OBJECT channel

The OO exception-object model (ISO §14.6.13.1.5; the OO deep-dive slice 6, brief D-EO1–D-EO10 in
`docs/COBOLNET_OO_SLICE_BRIEFS.md`) rides this engine with ONE signal architecture — no parallel
OO mechanism (feedback_singular_pattern):

- **ExceptionState** carries: `CobolObject? ExceptionObject` (the §8.4.3.6 register — ONE per run unit,
  GR2; typed per the OO D-U1 universal model), the `"EXCEPTION-OBJECT"` **LastName sentinel** (§15.33.3
  r1's literal EXCEPTION-STATUS value — the EXCEPTION-* functions needed ZERO changes; the sentinel is NOT
  a catalog name and `ExceptionCatalog.TryGet` fails on it by design), `SetObject`,
  `SetPropagatingObject`/`TakePropagatedObject` (the object propagation slot — mutually exclusive with the
  named `_propagated` slot; a named `Set` clears it and vice versa), and the `SetPropagatingLast` object
  leg (GOBACK RAISING LAST with an object status re-propagates the OBJECT, §14.9.18.4 GR1b3a).
- **RAISE identifier-1** → `BoundRaiseObject` (grammar takes `objectReference` so NULL/SUPER get the
  targeted 0848, and RAISE SELF parses); NOT TURN-gated (§7.3.25 takes names only); NEVER fatal by itself
  (GR2 — the continue-after-RAISE path is the normal exit).
- **USE Format 4** (`USE AFTER {EXCEPTION OBJECT | EO} class-name`; EO is context-sensitive like EC) →
  `BoundDeclarative.EoClassCsName` → the generated `__EcObjDispatch(object?)`: source-order `is` checks
  (GR14a class-or-subclass), −3 tail. GR3: for an OBJECT raise F4 REPLACES the F1/F3 tiers. GR15 holds
  structurally (the raise site sets the register before dispatching).
- **GOBACK / EXIT PROGRAM / method-return RAISING identifier-1** → `BoundRaising.ObjectSource` (exactly
  one of EcName/IsLast/ObjectSource); SR4d no-universal + SR4a declared-class-in-header (walking the base
  chain) = 0849 at COMPILE time — which statically discharges the activated-side rule-1 check in v1
  (D-EO5; revisit when FACTORY OF / interface RAISING legs land). The stage
  (`ExceptionState.SetPropagatingObject`) has no Enabled/Fatal logic — objects are not TURN-gated.
- **The pickup** (`CallEmitter.EmitPropagationPickup`) has an object branch (slots exclusive): GR1b2
  re-registers in the activator → F4 dispatch (rule 2) → on −3, the rule-4 conversion:
  `Set("EC-OO-EXCEPTION", true)` → the F3 tiers (EC-OO-EXCEPTION/EC-OO/EC-ALL match) → unresumed ⇒
  `CobolFatalException` (fatal per Table 13 — surviving needs RESUME AT NEXT STATEMENT, the standard
  fatal-EC protocol). The SAME pickup runs after every CALL and every Instance/Self/Super/Factory INVOKE
  and every UNIVERSAL dispatch (`OoEmitter.EmitInvokePickup`); NEW needs none (the ctor runs no user statements).
  GR1b's result-before-exception ordering falls out of stage-then-`throw MethodReturn` + the entry catch
  delivering RETURNING/copy-outs before the site's pickup.
- **Headers**: PD-header RAISING partitions into level-3 EC-USER names (SR7; else 0858) + classes of the
  group (the SR4a list); methods carry their own partition (`OoMethodSymbol.RaisingEcNames/RaisingClasses`,
  loaded per-method — a method IS a source element).
- **PROPAGATE ON** (:24606) is an un-implemented directive — the pickup's rule-3 hole is documented in the
  generated comment (residue). Method declaratives, interface/FACTORY-OF/ACTIVE-CLASS legs, and object
  VIEWS (EC-OO-CONFORMANCE) stay 0899-named.


## Summary

Decision-complete design for the conditions + exception subsystem of the greenfield COBOL→C#/Roslyn compiler (src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}; C# namespaces `CobolNet.*`). Covers IF/ELSE/END-IF; EVALUATE (all forms); level-88 condition-names + SET cond TO TRUE/FALSE; class/sign/relational/abbreviated-combined conditions with NOT>AND>XOR>OR precedence; and the COBOL-2002/2023 EC exception model (EC-* hierarchy, >>TURN, RAISE/RESUME, USE…EXCEPTION/ERROR declaratives, EXCEPTION-OBJECT, ON SIZE ERROR/AT END/INVALID KEY/ON OVERFLOW/ON EXCEPTION).

Two C# code shapes — both are the Roslyn backend's rendering of the ONE backend-neutral bound tree (G4/ICodeGenBackend: all semantics live in the BoundCondition/EC bound nodes + the binder-computed TurnState; emitters only render; the future CIL backend lowers the SAME bound nodes to branches with its own private lowering): (1) conditions are PURE C# boolean expressions (ConditionRenderer.Render(BoundCondition)→string, no side effects) so they compose into if/while/?:/EVALUATE-arms and level-88 bool properties; (2) the exception model is stateful runtime (CobolNet.Runtime.Exceptions) plus emitted guards that appear ONLY when a program uses the feature — EC checking is OFF by default (ISO §14.6.13.1.1, §5000), so the typed-native fast path emits zero exception scaffolding in the common case.

Correct behavior is defined by the ISO spec (specs/ISO_COBOL.md — cite the §); the legacy CobolSharp.Compiler/CodeGen/Lowering/ConditionLowerer.cs and CobolSharp.Runtime/PicRuntime.cs (364 NIST green) are a differential regression net and reference ONLY, never authority; the byte IMPLEMENTATION is rejected and re-derived over native string/long(/Int128)/bool — the numeric design's scaled integers; System.Decimal is rejected (docs/COBOLNET_NUMERIC_DESIGN.md). THIS document is that full prose (condensed view: docs/COBOLNET_DESIGN.md §11; brief overview: docs/COBOLNET_ARCHITECTURE.md). New diagnostics occupy a COBOLNET07xx band. New runtime classes: CobolClass (class-condition predicates over UTF-16 chars), ExceptionCatalog (generated from ISO Table 13: level-3→level-2→EC-ALL hierarchy + fatality), ExceptionState (last-exception register, EXCEPTION-OBJECT, file/location/statement), CobolException/CobolFatalException, ExceptionDispatch (declarative registry). Implementation is mechanical from here.

## The EC model — file map and engine specifics

The conditions half (D1–D8) is implemented as designed. The EC half (D9–D12) is implemented; the file map and
the engine's design specifics:

**File map.** Frontend: `Preprocessor/TurnDirectiveProcessor.cs` (the `>>TURN` stage — runs LAST, after COPY/NIST,
on the FINAL text so `TurnEvent.Line` is directly comparable to token `Start.Line`; line-count preserving, asserted
by `Frontend.Parse`; an emitting-branch TURN survives `ConditionalCompilationProcessor` via `leaveTurnDirectives` —
the legacy pipeline keeps consuming TURN there). Compiler: `Binding/TurnState.cs` (the compile-time §7.3.25.4 fold),
`Binding/Procedure/Verbs/EcBinder.cs` (RAISE/RESUME/SET LAST EXCEPTION/RAISING binds + the per-statement
`EcWrap` fold → `BoundEcChecked`), `CodeGen/EcEmitter.cs` (guards + the generated `__EcDispatch` /
`__IoCheckEc`), `CodeGen/Verbs/CallEmitter.cs` (RAISING staging, the CALL-site propagation pickup, the EC-PROGRAM
catch). Runtime: `Runtime/Exceptions/` — `ExceptionCatalog` (Table 13), `ExceptionState` (last-exception register +
propagation slot + the EC-ARGUMENT-FUNCTION ambient gate), `EcFunctions` (§15.28/30/32/33), `CobolFatalException`,
`ResumeSignal`. `CompilerDriver` hands `Frontend.TurnEvents` to `CSharpEmitter.Bind`; `EmitBound` renders the bound tree via `ProgramEmitter`.

**EC engine specifics:**
- **The declarative dispatch-result protocol.** Declaratives are pc
  RANGES run by the bounded `__Dispatch`, not C# methods — so RESUME throws the runtime `ResumeSignal`, the
  int-returning `__RunUse` (emitted only when the group is EC-active; the void form stays byte-identical otherwise)
  catches it, and every raise site speaks ONE protocol: `-1` normal completion (§14.6.13.1.2), `-2` RESUME AT NEXT
  STATEMENT (suppresses a fatal — §14.6.13.1.3 #5 NOTE 2), `-3` no qualifying declarative, `≥0` RESUME AT
  procedure-name's pc (≡ GO TO, §14.9.33.4 GR3). There is no `ExceptionDispatch` registry class: the F3 selector is
  the GENERATED `__EcDispatch` (source-ordered GR3c–g tiers over the program's own declaratives).
- **The TURN fold runs at BIND time, not emit time:** bound nodes carry no parse context, so the
  binder (which has the statement's line) queries the TurnState and wraps the statement in `BoundEcChecked`
  carrying the decision (`EcStatementInfo`: enabled (name, file) pairs, WITH LOCATION, statement name, the §15.30.3
  r2 location string). Checking-off binds the bare node — zero scaffolding by construction.
- **EcWrap's relevant-family rule (the unimplemented-raise license).** A statement wraps only for names its kind
  can actually raise here: EC-SIZE-* (arithmetic), EC-I-O-* per referenced file, EC-OVERFLOW-STRING/-UNSTRING,
  EC-PROGRAM-* (CALL/CANCEL), EC-ARGUMENT-FUNCTION (intrinsic-bearing statements), EC-BOUND-REF-MOD/-OVERFLOW +
  EC-DATA-NOT-FINITE (any statement — ambient gates, below) and EC-DATA-OVERFLOW (a MOVE), EC-STORAGE-NOT-AVAIL
  (SET SIZE). A name this implementation still cannot raise binds no wrapper — §14.6.13.1.1 sets an indicator only
  "when the associated exception occurs".
- **The EC-ARGUMENT-FUNCTION ambient statement gate.** Intrinsics render inline inside arbitrary expressions;
  threading a checked-mask through every runtime signature would fork each intrinsic into twins. Instead the guard
  wraps the STATEMENT (`ExceptionState.ArgumentFunctionChecking` set/reset + try/catch for the F3 dispatch), and
  EVERY §15.3 default-result site in the intrinsic runtime routes through `ExceptionState.ArgumentError` (raise
  when enabled, the documented default — 0 / one space — when off): `FromDouble` NaN/∞, FACTORIAL, MOD/REM zero
  divisor, NUMVAL/NUMVAL-C malformed, the CobolDate range checks, CHAR/ORD out-of-domain.
- **The float EC-DATA ambient statement gates (EC-DATA-NOT-FINITE / EC-DATA-OVERFLOW).** A float item's value is
  read inline in expressions, so — exactly like EC-ARGUMENT-FUNCTION / EC-BOUND-REF-MOD — the guard wraps the
  STATEMENT (`FloatNotFiniteChecking` / `FloatOverflowChecking` set/reset via `FatalAmbientGates` + the try/catch F3
  dispatch) and the runtime raise sites consult the flag. Both are **always-emitted** (the singular pattern —
  `CobolString.RefMod`, not an emit-time fork) so a directive-free build is byte-identical (the flag defaults OFF ⇒
  the wrap is a pass-through). **EC-DATA-NOT-FINITE (§14.6.13.2 item 3)** is wrapped at the TWO float sending-read
  chokepoints — the numeric-value read (`NumericRenderer.FieldNumCore` line 140 → `RuntimeApi.FloatSending`) and the
  string-image read (`OperandText.FieldAsString` float arm → `RuntimeApi.FloatSending`) — so DISPLAY, STRING,
  MOVE-to-alphanumeric/group, arithmetic, relations, level-88, intrinsic args, and a different-usage float MOVE
  source all raise for a NaN/±Inf content. **`CobolFloat.Sending`** raises the fatal EC. The **four exemptions** are
  realized as a RAW (unwrapped) read at exactly the exempt sites: a **class condition** and a **sign condition** pass
  `floatCheck:false` (a sign operand's whole sub-tree, via the re-entrant `NumericRenderer._floatSendingExempt`), a
  **same-usage MOVE** (source and receiver share the same `Usage` — endianness is a separate phrase, not a Usage
  value) passes the exempt flag, and **VALIDATE** will pass `floatCheck:false` once its emitter lands (documented
  no-op today). **EC-DATA-OVERFLOW (§14.9.25.4 GR4 step 4a)** is MOVE-only: `CobolFloat.StoreSingleChecked` at the
  single-precision-float MOVE receiver raises when a FINITE source casts to ±Inf (cast-based, `double.IsFinite(src)
  && float.IsInfinity((float)src)` — never a MaxValue compare, since a double in `(float.MaxValue, ~3.4028e38]`
  rounds to a finite `float.MaxValue`). An arithmetic ±Inf store (`ArithmeticEmitter.StoreArith`) stays a bare cast
  (a valid §14.6.8.3 GR1 result, never this EC); a double receiver cannot overflow from a finite double. Both ECs
  apply to EVERY floating-point usage in the typed-native model (all map to IEEE binary) — mandatory for the standard
  usages (FLOAT-BINARY/-DECIMAL), an implementor determination for FLOAT-SHORT/-LONG/-EXTENDED and COMP-1/COMP-2 (see
  `CONFORMANCE.md §3`). Goldens `2023/ec_data_not_finite` (both chokepoints + all exemptions) and `2023/ec_data_overflow`.
- **RAISING propagation = a runtime staging slot + a two-tier pickup.** GOBACK/EXIT PROGRAM RAISING stages
  (name, fatal) via `ExceptionState.SetPropagating[Last]` (¶27403 SR2 — an EC-USER name must appear in the
  PD-header RAISING phrase, checked at bind as COBOLNET0717). Per §14.9.18.4 GR1b the staged condition is raised
  in the ACTIVATING runtime element ONLY IF checking for that exception condition is enabled there ("an exception
  condition is raised in the activating runtime element IF checking for that exception condition is enabled in the
  activating runtime element"): an EC-active caller's generated CALL site consumes it (`TakePropagated` →
  `__EcDispatch` → RESUME honored → the §14.6.13 fatal/nonfatal handling) and passes `siteHandlesPropagation:
  true`. An EC-free caller — one that does NOT enable checking for that condition — does not raise it at all: the
  staged condition, fatal or nonfatal, is discarded and execution continues in the caller as if the CALL had
  returned without a RAISING phrase (there is no boundary "terminate loudly" default — §14.6.13.1.3 #8 governs a
  condition that already EXISTS, and GR1b says a condition whose checking is off in the activator is never raised
  there). The site pickup gates on the GROUP's EC participation (not a per-name TURN fold) because RAISING LAST
  EXCEPTION makes the propagated name dynamic. A MAIN program's GOBACK RAISING has no activator, so its RAISING
  phrase is IGNORED and the program terminates as an ordinary STOP (§14.9.18.4 GR3); an EXIT PROGRAM RAISING with
  no calling element raises nothing and acts as CONTINUE (§14.9.14.4 GR2) — in both cases `RunMain` discards the
  staged condition rather than terminating on it.
- **The EC-PROGRAM bridge rides `CobolCallException.EcName`.** The registry latches the Table 13 level-3 name
  (NOT-FOUND / RECURSIVE-CALL / CANCEL-ACTIVE / ARG-OMITTED); a CALL/CANCEL under enabled checking emits a
  name-FILTERED catch (`when (__ce.EcName == …)`) that sets the status and either flags the statement's own
  ON EXCEPTION phrase (it wins — §14.6.13.1.3 #1) or runs the F3 selection + fatal default. A non-enabled name
  falls through — checking-off behavior unchanged.
- **The EC-SIZE bridge rides `CobolSizeError.EcName`.** The runtime kernels latch the precise condition
  (EC-SIZE-ZERO-DIVIDE at the divide kernels; EC-SIZE-TRUNCATION at the TryStore/TryFormat receiver-capacity
  failures; EC-SIZE-OVERFLOW otherwise) and an ENABLED statement routes through the same two-phase TryStore shape
  even WITHOUT the phrase — the phrase, when present, handles (status still set, §14.6.13.1.4 #1).
- **`__IoCheckEc`** is the EC-aware after-verb hook a statement with enabled EC-I-O checking calls INSTEAD of
  `__IoCheck`: same F1 behavior (phrase short-circuits §9.1.13.1, GR3a/b selection, the GR4b outward-GLOBAL walk)
  plus the §9.1.13.1 status→EC raise gated by the per-statement compile-time mask (`ExceptionCatalog.IoMaskNames`
  bit order), the F3 tiers BEHIND the F1 tiers, and the fatal-status default (3x/4x/7x/9x). The GR3g
  outward-GLOBAL continuation is realized only on this I-O path — F3 declaratives are not yet GLOBAL-walkable
  (no corpus or conformance driver exercises it; revisit with the OO/2002 wave).
- **WITH LOCATION (§15.30.3 r1 choice):** without the LOCATION phrase this implementation saves NO location
  information — EXCEPTION-LOCATION returns one space, EXCEPTION-STATEMENT 63 spaces; with it, the bind-time
  pre-rendered "element; paragraph[ OF section]; line" string and the uppercase statement name are recorded at the
  raise site.
- **The catalog is NAME-keyed, not a C# enum:** EC-USER-* / EC-IMP-* are
  OPEN families (§14.6.13.1.1 — user-defined by mention, always nonfatal ¶24505), so the canonical identity is the
  NAME; an enum would need a parallel name channel (two representations — the singular-pattern rule).
- **Grammar continuity:** RAISE/RAISING/RESUME/STATEMENT/CONDITION/EC are context-sensitive tokens mirrored in
  `cobolWord` — legal user-defined words at EVERY edition (pinned by a version-matrix continuity test). The
  RAISE/RESUME statement alternatives are UNgated so `--std 85` gets the targeted COBOLNET0876 diagnostic, not a
  nameless parse error. `displayStatement` gained the `functionCall` operand alternative (§8.4.4.1 — an identifier
  includes a function-identifier; `DISPLAY FUNCTION EXCEPTION-STATUS` is the canonical interrogation shape).
- **Diagnostics band:** COBOLNET0710 (RAISE of a non-level-3), 0711 (unknown exception-name), 0712/0713/
  0714 (RESUME SR1/SR2/SR3), 0715/0716 (USE F3 SR13/SR14), 0717 (RAISING SR2 ¶27403), 0718/0719 (TURN SR3/SR1+SR4),
  0875–0879 (the 2002+ edition gates: TURN / RAISE+RESUME / USE F3 / per-name edition window / SET LAST EXCEPTION+
  RAISING).
- **Verification:** `ExceptionConditionConformanceTests` (48 spec-pinned facts — TURN scoping/expansion, RAISE
  fatal/nonfatal × enabled/off, RESUME both forms, F3 tier selection, the SIZE/OVERFLOW/I-O/PROGRAM/
  ARGUMENT-FUNCTION bridges, RAISING propagation both verbs, the EXCEPTION-* functions, the edition gates, the
  zero-scaffolding invariant asserted on generated source) + `TurnStateTests`/`ExceptionCatalogTests` (13 unit
  facts: the GR2/GR3 EC-I-O-WARNING exclusion, file-scoped events, last-event-wins, strict GR5 lines, Table 13
  fatalities, the §9.1.13.1 status map). The legacy oracle has NO EC model — every expected value derives from the
  cited §.

**Still later waves:** the exception-checking PERFORM WHEN + `>>PROPAGATE` (2023 — VCR row 79/§4808),
RAISE/RAISING identifier (exception OBJECTS — the OO wave; the `ExceptionState.ExceptionObject` slot exists),
EXCEPTION-FILE/EXCEPTION-FILE-N's 2023 file-connector argument (loud by name — VCR rows 68/69, PHASE-13 Step 9),
GLOBAL-walkable F3 declaratives, VALIDATE/EC-VALIDATE (§18.17). *(The national `-N` twins are LIVE — P10 Step 11:
`EcFunctions.FileN`/`LocationN` (§15.29/§15.31) = the base renderings through the ONE `CobolIntrinsics.NationalOf`
repertoire translator, result category National; golden `exception_file_n`, matrix row `exception-file-n-2002`,
85-window negative.)*

## Decisions

### D1. Conditions are bound to backend-neutral BoundCondition nodes (BoundRelational/BoundLogical/BoundNot/BoundCondition88/BoundSignCondition/BoundClassCondition); the Roslyn backend's ConditionRenderer.Render(BoundCondition)→string emits them as pure, side-effect-free C# boolean expressions. The grammar's rule cascade (logicalOr→logicalXor→logicalAnd→unaryLogical→primaryCondition) fixes precedence at parse/bind time. (src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs.)

**Rationale.** COBOL condition precedence NOT>AND>XOR>OR is already encoded by the grammar's rule cascade, so precedence is preserved by construction without us re-grouping. Pure expressions compose into if/while(!(…))/?:/EVALUATE arms/level-88 properties — one translator serves every consumer.

**Rejected alternatives.** Lowering conditions to imperative IR with temporaries (the legacy IrBinaryLogical model) — unnecessary in a C# target where the host language already has boolean expressions; it would also force statement context where an expression is wanted (e.g. ?:).

### D2. Fully parenthesize every emitted binary boolean node: (a && b), (a || b), (a ^ b).

**Rationale.** C#'s bool precedence is ! > & > ^ > | > && > || — so ^ binds TIGHTER than &&/||, which does NOT match COBOL's AND>XOR>OR. Explicit parens make the emitted tree's grouping exactly the COBOL parse tree's grouping, independent of any C# precedence subtlety.

**Rejected alternatives.** Rely on C# operator precedence — the ^-vs-&& ordering mismatch is a genuine correctness trap for logical XOR.

### D3. Emit short-circuiting && / || for COBOL AND / OR — the left-to-right evaluation order ISO §8.8.4.13 rule 1 mandates.

**Rationale.** ISO §8.8.4.13 rule 1: within a hierarchical level "the constituent connected conditions … are evaluated in order from left to right, and evaluation of that hierarchical level terminates as soon as a truth value for it is determined regardless of whether all the constituent connected conditions within that hierarchical level have been evaluated." Short-circuiting `&&` / `||` render exactly this: once the left operand fixes the level's truth value, the right operand is not evaluated. This is the conformant order (and is also idiomatic and faster). Corroborated corpus-safe: a scan of tests/nist/programs found ZERO guard-then-same-variable-subscript idioms; the 44 'AND <subscripted>' cases use a subscript independent of the guard (e.g. IF SUB4 = 6 AND WZ-X-CHAR(SUB2) = SPACE).

**Rejected alternatives.** Eager (non-short-circuit) evaluation as in the legacy ConditionLowerer lines 188–196 (both operands into temporaries, then combine) — non-conforming: §8.8.4.13 rule 1 requires evaluation of a hierarchical level to stop once its truth value is known, so eager evaluation can execute a right operand (a subscript, a function, a side effect) the standard requires be skipped. For example `IF I>0 AND TABLE(I)=X` must not reference TABLE(I) when I ≤ 0 — short-circuit is the only faithful rendering; the legacy oracle's eager evaluation is the behavior to be corrected, not preserved.

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

### D12. The exception-checking PERFORM (ISO §14.9.28 Format 3, COBOL-2023 — VCR row 79; introduction-gated at --std=85|2002|2014 with COBOLNET0900) is a PER-STATEMENT exception interceptor scoped to imperative-statement-1 — NOT a block C# try/catch. FULLY IMPLEMENTED (recognize/validate/diagnose/gate + the pc-RANGE runtime interceptor — the F3 PERFORM compiles and runs); a few sub-forms remain staged at COBOLNET0899 (open-mode WHEN operand, F3-in-a-method, cross-CALL "in range", EC-FLOW-USE/>>PROPAGATE, exception-object raise in imp-1). The as-built implementation SSOT is `docs/rearchitecture/evidence/PHASE-13-c5-perform-format3-DESIGN.md` §9.

**Grammar (greenfield, `CobolControlFlow.g4`).** Formats 2 and 3 merge into ONE inline `performStatement` alternative (`PERFORM performInlineHead? statementBlock* performWhenPhrase* performWhenOther? performWhenCommon? performFinally? END-PERFORM`); ≥1 ordinary WHEN ⇒ Format 3 (enforced at bind, COBOLNET1597). A WHEN operand list's CONTINUATION is bounded by the `whenOperandAhead()` predicate (`CobolParserCoreBase.WhenOperandStopTokens`) so a body verb that is also a `cobolWord` (RESUME/RAISE/VALIDATE/UNLOCK/SEND/RECEIVE/COMMIT/ROLLBACK/ENTER, + GET/PARSE forward) is not annexed as a spurious exception-name; the merged inline arm precedes the out-of-line `PERFORM procedureName` so `PERFORM LOCATION imp… END-PERFORM` disambiguates on END-PERFORM. `LOCATION`/`FINALLY` are new-2023 reserved tokens: LOCATION stays a `cobolWord` (the continuity invariant — a paragraph named LOCATION / `PERFORM LOCATION` parses below 2023; it appears only in the head, so no operand-swallow); FINALLY is a pure reserved keyword (NOT a `cobolWord`) — as a trailing phrase keyword after imperative statements it would be swallowed by a preceding DISPLAY/MOVE operand list, so it is treated as reserved at every edition (a documented, negligible deviation — FINALLY was never a COBOL identifier idiom).

**Binder (`EcBinder.ExceptionPerform.cs` → `BoundExceptionPerform`).** Resolves each WHEN's operands (exception-name at ANY level per the USE GR3a–3g tiers — not the RAISE level-3-only rule; SR16 EC-I-O prefix; the per-name edition window), enforces the §14.9.28.3 syntax rules and the cross-statement bans by lexical region (A = imp-1, B = whole PERFORM, C = WHEN phrases, D = imp-2..5) via parse-subtree walks: COBOLNET1597 (≥1 WHEN), 1599/1600/1601 (SR14/15/16), 1604 (EXIT PERFORM CYCLE), 1605/1606/1607 (INITIATE/TERMINATE/VALIDATE >1), 1608 (GO TO in a WHEN), 1610 (RESUME AT proc in a WHEN), 1611 (RAISE outside imp-1), 1612/1614/1615/1616/1617 (multi-CLOSE / dup-INITIALIZE / MERGE / dup-OPEN / SORT in imp-1). RESUME's SR1 relaxes inside a WHEN (`EcBindState.InF3When`): RESUME NEXT STATEMENT is legal there, RESUME AT proc is COBOLNET1610. GR14 is a bind-time overlay on `TurnState` (`WithImplicitEnable` — a synthetic line-0 enable per WHEN-named EC over imp-1, WITH LOCATION iff the PERFORM specifies LOCATION; a real >>TURN OFF inside imp-1 overrides it). At the end of imp-1 GR14 assumes an implicit PUSH ALL followed by TURN OFF ALL, so imp-2..5 (the WHEN / OTHER / COMMON handler bodies and the FINALLY block) bind with ALL exception checking OFF — no ambient >>TURN state is visible to them, not even checking that was enabled before the PERFORM. Immediately preceding END-PERFORM an implicit POP ALL restores the pre-imp-1 state and issues an implicit TURN … OFF for any exception that was implicitly enabled over imp-1; GR22 then governs what checking carries past the PERFORM (a pre-PERFORM enable that fired stays enabled, a >>TURN within the range is retained, otherwise the WHEN-named ECs are not enabled after the statement).

**Runtime — the pc-RANGE interceptor (IMPLEMENTED; SSOT `PHASE-13-c5-perform-format3-DESIGN.md` §9).** A per-statement interceptor is REQUIRED (not a block try/catch): a block catch unwinds past the remaining imp-1 statements and cannot deliver GR20's nonfatal resume-in-place. As-built: imp-1 emits INLINE inside a `try`; a raise site within it consults an ambient `PerformFrame` stack (`ExceptionEngine`, run-unit-scoped) BEFORE the USE declaratives (GR17 — a matching WHEN ignores USE), via the funnel `EcDispatchExpr` → `__EcPerform` (→ `RunTopFrame`, a top-down walk with a deferred `Handling`-clear that keeps a skipped inner frame transparent while a selected outer handler runs, GR21). The WHEN/OTHER/COMMON handler bodies (imp-2/3/4) are emitted as **synthetic UNREFERENCEABLE pc-range paragraphs** appended above the main pc space (the fall-through walled off at `F3HandlerBasePc − 1`) and run via the reused `__RunUse(id, pc, pc)` — so RESUME reuses `ResumeSignal`→`__RunUse`→`-2` verbatim. The frame's matcher is a closure that does **tier-ordered** WHEN selection (GR17 → §14.9.49.4 GR3c-g: file+L3 → file+L2/bare-file → L3 → L2 → L1/EC-ALL, source order only within a tier — mirrors `__EcDispatch`) and, on match, invokes `__RunF3` (imp-2 then WHEN COMMON imp-4). FINALLY (imp-5) is the INLINE trailing block. **EXIT PERFORM** is region-aware (`DispatchState.F3Cur`): imp-1 → `goto __f3fin`, a handler pc-range → `throw ExitPerformSignal(Id)` (caught at the PERFORM boundary — a handler runs in a nested `__Dispatch` a `goto` cannot leave, the reason the rejected lambda-matcher-body approach could not implement it), FINALLY → `goto __f3end`; a nested inline PERFORM saves/restores `F3Cur=None` so its own EXIT PERFORM breaks the inner loop (§14.9.14.4 GR5a). The fatal/nonfatal split (GR20) is realized by each raise site's existing throw idiom + the `-1/-2` protocol — the matcher carries no `fatal`. Every emission is gated on `EcState.UnitHasF3Perform` so a non-F3 unit is byte-identical.

**Decisions recorded (design panel + §9.6):** (a) **RESUME NEXT STATEMENT in a WHEN SKIPS WHEN COMMON** — GR17 hands to imp-4 "at the completion of imp-2", and a RESUME is a transfer OUT (not a completion), so `__RunF3` runs COMMON only on the handler's `-1` (chosen interpretation; the standard is silent). (b) **FINALLY does NOT run on the fatal abnormal-termination path** (normal / EXIT-path only) — a genuine STANDARD DEFECT (§14.9.28.4 NOTE 8 "the end of the PERFORM includes FINALLY" vs GR20's fatal branch → §14.6.13.1.3 abnormal termination, which never re-enters the end of the PERFORM; the two cannot both hold). Realized because a `CobolFatalException` unwinds past the inline FINALLY block. (c) **the WHEN operand match follows the full §14.9.49.4 GR3a→g priority**: bare file-name (GR3a) > open-mode `WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND` (GR3b, matched at the raise site by `CobolFile.OpenModeOf(__f)`) > file+L3 (GR3c) > file+L2 (GR3d) > L3 (GR3e) > L2 (GR3f) > L1/EC-ALL (GR3g) — file/mode scope OUTRANKS an exception-name, source order only within a tier. The open-mode form is implemented (an OPEN-failure's mode is best-effort — the connector reports its mode only once open).

**Not emitted (spec-fidelity):** COBOLNET1598 ("operand-form exclusivity") — no §14.9.28.3 SR backs it (SR14/15/16 are the only WHEN-operand rules, and the figure's `{exception-name-1 | exception-name-2 FILE file-name-2}…` reading permits interleaving), so it is not enforced. XS-RESUME-PLACEMENT is subsumed by COBOLNET0712 (RESUME may appear only in a declarative or a WHEN phrase). XS-POP/XS-PUSH (would be COBOLNET1602/1603) stay reserved — the >>POP/>>PUSH directives are themselves unimplemented, so their F3 ban rides that directive wave.

**Rejected alternatives.** Model every EC via C# exceptions/try-catch globally — exceptions are for RESUME's declarative unwind (`ResumeSignal`); the common inline phrases and declaratives use status-flag/branch control flow for correctness (resume-after-statement) and zero default cost.

## C# mapping

IF: `IF c [THEN] s1 [ELSE s2] END-IF` → `if (<RenderCondition(c)>) { s1 } else { s2 }`. CONTINUE→empty block. NEXT SENTENCE→lower the sentence as a labeled block + `goto <after_sentence>;` (COBOLNET0701). Nested IF: each branch is fully braced so C# dangling-else is structurally impossible.

RELATIONAL: numeric (both operands numeric) → render as scaled longs, align to larger scale via existing NumX/Align, then `(<l> <op> <r>)` (exact, no truncation). Alphanumeric (either side non-numeric) → `(CobolString.Compare(a,b,weights?) <op> 0)` with space-extension of the shorter operand (ISO §8.8.4.2.7 rule 2). Pointer (= / NOT = only) → `ReferenceEquals(p,q)` / `p is null`. Figurative ZERO vs numeric → numeric 0. Literal-vs-literal constant-folds to true/false. Operator mapping via existing MapOperator (all symbolic + word + NOT-prefixed forms; needs an ~18-form unit-test matrix).

SIGN: `op IS [NOT] POSITIVE|NEGATIVE|ZERO` → `(<num> > 0)` / `(<num> < 0)` / `(<num> == 0)`, NOT wraps in !(…). (NOT POSITIVE = ≤0, includes zero — the !(…) handles it.)

CLASS: `IF X IS NUMERIC` → `if (CobolClass.IsNumeric(X))`; `IS ALPHABETIC`→`CobolClass.IsAlphabetic(X)` ({A-Z,a-z,space} closed set). Numeric long item: folds to `if (true)` (COBOLNET0706). User CLASS HEX → `CobolClass.IsUserClass(X, "0123456789ABCDEF")` (THRU ranges expanded). NOT wraps in !(…).
New runtime: `static class CobolClass { bool IsNumeric(string); bool IsNumericDisplay(string,NumProfile); bool IsAlphabetic(string); bool IsAlphabeticLower(string); bool IsAlphabeticUpper(string); bool IsUserClass(string, ReadOnlySpan<char>); }` — ported verbatim from PicRuntime.IsNumericClass/IsAlphabeticClass (legacy 2379–2464) but over UTF-16 chars, sign-aware (overpunch {,A-I,},J-R / separate +,-).

LOGICAL: AND→`(a && b)`, OR→`(a || b)`, XOR→`(a ^ b)`, NOT→`(!(p))` — all fully parenthesized, short-circuiting.

ABBREVIATED COMBINED (ISO §8.8.4.12): walk keeping a current subject+operator from the last full relation; `op operand`→expand to `subject op operand`; bare `operand`→`subject currentOp operand`; leading NOT negates that relation only. Example `IF A = B OR C OR > D` → `((A==B) || (A==C) || (A>D))`.

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
Single value→`==`; THRU→`>= from && <= to`; multiple→OR; alpha values space-extended to parent width, ALL literal repeated to width. Subscripted cond-name→method `bool COND(long i)=>parent[i-1]==v;` (tables are supported — OCCURS→T[]). SET ACTIVE TO TRUE→`WS_STATE = CobolNum.Store(1L,0,_P_WS_STATE);`. SET DONE TO FALSE→`WS_STATE = CobolNum.Store(0L,0,_P_WS_STATE);`.
DataItem gains `List<ConditionName> ConditionNames`; `record ConditionName(string CobolName, string CsName, IReadOnlyList<CondValue> TrueValues, CondValue? FalseValue)`; `readonly record struct CondValue(string FromLiteral, string? ThruLiteral, bool IsAll)`.

EVALUATE: hoist subjects → chained if/else-if/else. One WHEN clause's match = OR over its WHEN phrases ( AND over ALSO subjects ( per-subject match ) ). Per-subject: ANY→true; value→`==` (scaled/collating); range v1 THRU v2→`>=v1 && <=v2`; partial-expr (item starts with relop/class/sign)→prepend subject; TRUE/FALSE↔condition subject→`_eK == true/false`; group NOT→negate the group's conjunction. WHEN OTHER→final else.
Example `EVALUATE WS-DAY ALSO TRUE / WHEN 1 THRU 5 ALSO WS-OPEN … / WHEN 6 7 ALSO ANY … / WHEN OTHER …` →
```
var _e0 = WS_DAY; var _e1 = true;
if (((_e0>=1L && _e0<=5L) && (WS_OPEN==_e1))) {…} else if (((_e0==6L||_e0==7L) && true)) {…} else {…}
```

ON SIZE ERROR (ISO §14.7.5): the checked store is **`CobolNum.TryStore`** (SSOT §14.7) — one operation that stores plus runs the capacity/inexact check, leaving the receiver unchanged on overflow (returns `bool`; `false` = ON SIZE ERROR). It rounds to scale FIRST, then tests integer-part capacity; division-by-zero / exponentiation-rule → size error. On error the receiver is LEFT UNCHANGED, so stage the value and assign conditionally. Multiple receivers: OR the per-receiver flags; non-overflowing receivers ARE updated. (Signature: `bool TryStore(CobolInt value, in NumProfile receiver, CobolRounding mode, out long stored)`.)
```
bool _se=false; { var _v = new CobolInt(…, _s); if (CobolNum.TryStore(_v, _P_B, _mode, out long _r)) B = _r; else _se = true; }
if (_se) { <ON SIZE ERROR> } else { <NOT ON SIZE ERROR> }
```

AT END / INVALID KEY / ON OVERFLOW / ON EXCEPTION: status flag from the op drives a branch; NOT form = the else (success). `var _st = CobolFile.Read(f,…); if (CobolStatus.IsAtEnd(_st)) {AT END} else {NOT AT END}`. AT END↔EC-I-O-AT-END↔status 1x; INVALID KEY↔2x; ON OVERFLOW↔EC-OVERFLOW-STRING/UNSTRING; ON EXCEPTION (CALL)↔EC-PROGRAM-NOT-FOUND.

EC RUNTIME (CobolNet.Runtime.Exceptions): `enum ExceptionCondition` (level-3 names) + hierarchy map (level3→level2→EC-ALL) + per-name Fatality {Fatal,NonFatal,Imp}, generated from ISO Table 13 in ExceptionCatalog.cs. `static class ExceptionState { string? LastExceptionName; object? ExceptionObject; string? LastExceptionFile/Statement/Location; void Clear(); }`. RAISE EXCEPTION ec→`CobolException.Raise(ec)`; RAISE id→`CobolException.RaiseObject(obj)` (sets EXCEPTION-OBJECT); fatal unhandled→`throw new CobolFatalException(ec)` caught at Main. FUNCTION EXCEPTION-STATUS→`ExceptionState.LastExceptionName ?? "        "`; EXCEPTION-OBJECT→`ExceptionState.ExceptionObject`.

USE declaratives: declarative SECTION→paragraph-method returning `enum ResumeAction {Default, NextStatement, Procedure(string)}`; `ExceptionDispatch.Invoke(ec, file)` selects first match (file>open-mode>ec) and calls it; site inspects ResumeAction (NextStatement→fall through; Procedure→goto label).

## Hard problems

### Short-circuit (C# &&/||) is the ISO-mandated evaluation order — IF I>0 AND TABLE(I)=X must NOT evaluate TABLE(I) when I ≤ 0 (§8.8.4.13 rule 1), so the guarded subscript is never touched (no EC-BOUND, no IndexOutOfRangeException in the typed model).

ISO §8.8.4.13 rule 1 terminates a hierarchical level's evaluation as soon as its truth value is determined, so `&&`/`||` are the faithful rendering: a left operand that fixes the level's truth value skips the right. The legacy oracle's eager (non-short-circuit) evaluation is the non-conforming behavior, and is corrected here. Corroborated corpus-safe by scanning tests/nist/programs: ZERO guard-then-same-variable-subscript idioms (the 44 'AND <subscripted>' cases use independent subscripts).

### Abbreviated combined conditions (IF A > B AND < C OR = D) — the subject and/or operator are elided after the first relation; the LEGACY emitter silently dropped them — the greenfield binder must expand them into full relations.

Expand at BIND time into ordinary full BoundRelational nodes (G4: the expansion is semantics, so it lives in the binder — every backend receives the already-expanded tree): maintain current-subject + current-operator from the most recent full relation; `op operand`→`subject op operand`; bare `operand`→`subject currentOp operand`; reset on each full comparison; leading NOT negates that relation only (it is part of the operator, not the subject). Ships with a dedicated test set; flagged as the single most error-prone condition feature.

### EVALUATE partial expressions (EVALUATE X ALSO Y / WHEN > 5 ALSO "A" THRU "M") — a WHEN object that begins with a relational/class/sign operator must be combined with its subject (ISO §14.9.13.3 GR5/8, §14.9.13.4 GR4a-2).

Binder detects the partial form (leftmost token is a relop/class-name-without-id/sign-word) and synthesizes `subjectK <partial>` as a full condition; the corresponding subject is treated as TRUE. Grammar already admits `condition` as a WHEN item; injection happens in binding.

### ON SIZE ERROR leaving the receiver unchanged + multiple receivers + rounding interaction (ISO §14.7.5).

CobolNum.TryStore (the single settled name — see the C# mapping) computes the candidate, rounds to scale FIRST, then tests integer-part capacity; writes the field only if no overflow (or no SIZE ERROR phrase present). Each receiver tested independently; non-overflowing receivers updated; the phrase fires if ANY overflowed (OR of per-receiver flags). Division-by-zero and exponentiation-rule violations route to the same size-error path (EC-SIZE-ZERO-DIVIDE / EC-SIZE-EXPONENTIATION).

### RESUME control flow (NEXT STATEMENT vs procedure-name vs GLOBAL-declarative≡CONTINUE) requires a declarative to redirect the caller's control after it returns (ISO §14.9.33).

Declarative methods return a ResumeAction enum; the dispatch site at the offending statement inspects it: NextStatement→fall through past the statement (suppress termination for a fatal EC); Procedure(name)→goto that label (as if GO TO); Default→ISO default (continue for nonfatal, terminate for fatal). RESUME inside a GLOBAL declarative is compiled as CONTINUE (§30319).

### >>TURN must gate WHETHER a guard is emitted per statement, in source order, with EC-ALL/level-2 expansion — without a runtime cost when OFF.

A compile-time TurnState walks the procedure division in source order maintaining the enabled-EC set (EC-ALL→all; level-2→its level-3 children; EC-I-O-WARNING explicit-only). The emitter consults TurnState.IsEnabled(ec, atStatement) before emitting any EC check. OFF compiles to nothing — no runtime branch. WITH LOCATION makes the guard pass (file,line,verb) into ExceptionState.

### Whole-group comparison (IF GROUP-A = GROUP-B, or group vs literal) compares the group as one alphanumeric value, but a group is a record struct in the typed model (no character buffer).

The whole-group character-image facility: every group record struct emits AsImage()/FromImage() (GroupImageCodec), and a character-image group operand routes through .AsImage() into CobolString.Compare (OperandText). COBOLNET0708 is retired. This is the one IF/EVALUATE operand kind that native field comparison cannot do alone.

### Distinguishing a bare data-name operand in a condition: level-88 condition-name vs boolean PIC 1 vs mnemonic switch vs numeric-implicit-≠0 vs alphanumeric truthiness.

Binder resolves the name's category: 88→condition-name bool property; PIC 1/boolean→the field itself; SPECIAL-NAMES switch→switch test; numeric→`(num != 0)` (legacy lines 220–228); alphanumeric→`!string.IsNullOrWhiteSpace(item)` (legacy-compatible, COBOLNET0702).

## Edge cases

- NEXT SENTENCE (obsolete) is NOT CONTINUE: it jumps past the next period, so it lowers via a labeled sentence block + goto, unlike a plain if-fall-through (COBOLNET0701).
- ALPHABETIC is the closed set {A-Z,a-z,space} only (ISO §8.8.4.4) — must NOT use char.IsLetter (rejects accented/Unicode letters); legacy comment line 2436.
- IS NUMERIC on a signed numeric-DISPLAY item accepts the overpunch sign ({,A-I positive; },J-R negative) or separate sign (+/-) at the sign position; spaces are NOT digits so a field with embedded/trailing spaces is NOT NUMERIC.
- IS NUMERIC on a pure native long/Int128 item folds to constant true (COBOLNET0706): the fold applies ONLY to a numeric item with no REDEFINES/overlay view; an aliased item routes through the runtime CobolClass check (CobolClass.IsNumeric, §8.8.4.4 GR1/GR2).
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
- Pointer relations support only = / NOT = (ISO §8.8.4.2.2 Format 3 — the message-tag-object-or-pointer-reference general format admits only the EQUAL/=/<> operators; comparison rule §8.8.4.2.16) → ReferenceEquals / is null on ManagedPointer.
- EC-I-O-WARNING can only be turned on/off explicitly in a >>TURN or PERFORM WHEN (§5006); EC-ALL does not include it.
- Whole-group comparison routes through the record struct's AsImage() character image into CobolString.Compare.
- User-defined exceptions EC-USER-<suffix> are always nonfatal (ISO §24505) and only raisable by RAISE / EXIT…RAISING / GOBACK RAISING.
- A condition-name may be qualified/subscripted (cond-name OF grp (i)) → emit a parameterized bool method, not a property (tables are supported).

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

- ISO/IEC 1989:2023 §8.8.4.2 — simple relation conditions (algebraic numeric value comparison §8.8.4.2.4; alphanumeric comparison + space-extension of the shorter operand §8.8.4.2.7 rule 2; pointer = / NOT = §8.8.4.2.16, Format 3 general format §8.8.4.2.2)
- §8.8.4.12 — abbreviated combined relation conditions (elided subject/operator; NOT is part of the operator)
- §8.8.4.4 — simple class condition (NUMERIC; ALPHABETIC closed set {A-Z,a-z,space}); §8.8.4.7 — simple sign condition
- §8.8.4.5 — simple condition-name condition (88-level abbreviates 'conditional variable == one of its values')
- §8.8.4.9 — logical operators AND / OR / EXCLUSIVE-OR / XOR / NOT and their meanings; §8.8.4.11.3 — precedence NOT > AND > XOR > OR; §8.8.4.13 rule 1 — left-to-right order of evaluation with short-circuit termination of each hierarchical level
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
- §14.9.18.4 GR1b/GR3 (GOBACK) + §14.9.14.4 GR2 (EXIT PROGRAM) — RAISING propagation: the condition is raised in the activator only if checking for it is enabled there; with no calling runtime element the RAISING phrase is ignored (GOBACK acts as STOP, EXIT PROGRAM as CONTINUE)
- §14.9.28.4 GR14 — the exception-checking PERFORM's implicit TURN scoping: WHEN-named ECs implicitly enabled over imperative-statement-1, then an implicit PUSH ALL + TURN OFF ALL for the handler bodies / FINALLY, and a POP ALL immediately preceding END-PERFORM (GR20 fatal/nonfatal return, GR22 post-PERFORM checking retention)
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
