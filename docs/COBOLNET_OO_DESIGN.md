# COBOL.NET — OO COBOL -> .NET classes (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §10; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.
>
> **Design refreshed 2026-07-03 (DEVLOG 583 pending).** The lost off-repo brief
> `/e/tmp/phase2-briefs/oo-plan-brief.md` has been regenerated INTO this doc: the four spec-verified
> 2023 corrections are folded in (see "Spec corrections" below), plus the "Legacy port map" (slices
> 1–6 from DEVLOG 439–456) and "Greenfield seams" sections. **Implementation = roadmap Phase 3**
> (`docs/COMPLETION_ROADMAP_COUNCIL.md`): SPINE first (the BoundMethodReturn/BoundStopRun split — D8 —
> plus the emit-into-a-type parameterization of the emitter), then the ported legacy slices 1–6, then
> FACTORY → PROPERTY → INTERFACE-ID → universal object reference → EC-OO.
>
> **STATUS (2026-07-04, DEVLOG 600/601/602): the SPINE (parts 1–2) AND PORT SLICE 2 are LANDED.**
> Part 1 — CobolObject runtime base (D2) + USAGE OBJECT REFERENCE (universal). Part 2 — ClassUnit
> collection + the pass-1 class symbol table (D1), the emit-into-a-type parameterization, method
> exit-bounded PC ranges + BoundMethodReturn (D8, realized catch-at-entry — see the D8 AS-BUILT note),
> INVOKE NEW / no-arg instance (D5 subset), typed object references, the 0813/0820–0827 diagnostic
> band. **Slice 2 (DEVLOG 602)** — method LINKAGE → typed <c>ref</c> C# parameters over capturable
> locals, LOCAL-STORAGE → C# locals (re-init per activation), method WS → STATIC fields with the
> §13.5.3 SR 1 edition window (`method-working-storage-window`, VCR Table 6 row 130e), per-method DATA
> scopes (§11.7 GR5 shadowing; trap #6 structural), the LOCAL-FUNCTION dispatcher realization (see the
> emitter-seam AS-BUILT), INVOKE USING/RETURNING marshaling under §14.8.2 STRICT conformance (D6 —
> type-preserving crossings, 0828 band, SR 10 object-data auto-CONTENT), and implicit-RECURSIVE
> reentrancy (:12032) proven by test. **5 of the 9 oo_* goldens ENABLED** (+ oo_method_args). Slices 1
> + 2 (and the 455-slice scoping, and the legacy-never-landed multi-method LINKAGE) are DONE.
> **Slices 3a+3b (DEVLOG 603):** INHERITS (`: BASE`, override marking + §9.3.8.2 signature 0829 via the
> ONE shared DescriptionMismatch rule, cycle 0820) and SELF/SUPER (D5: `this.M(…)` virtual GR2 /
> `base.M(…)` non-virtual GR3; 0827 placement band incl. trap #7) — **ALL 9 oo_* goldens byte-exact.**
> The same wave ran a 45-agent adversarial find→verify workflow: 22 CONFIRMED conformance bugs, all fixed
> (see the D6 AS-BUILT correction — the §14.8.2.3.3/§14.8.3.3 mode-dispatch redesign, SIGN/JUST/BWZ
> compares, group by-ref prefix, covariant returns, scope-aware lookups, crossing-form harmonization,
> emitted-name safety); implementation briefs for the remaining slices live in
> `docs/COBOLNET_OO_SLICE_BRIEFS.md`.
> **FACTORY (§11.4) LANDED (DEVLOG 604, per its brief's D11):** the `factoryParagraph` grammar (+ the
> FACTORY token/funnel — user word at 85, 0901 ≥2002), per-class factory SINGLETON classes
> (`FOO__FACTORY : BASE__FACTORY | CobolObject` with `__Instance` + covariant `__New` — NOT statics:
> §8.6.4 per-class copies of inherited factory data, SELF-in-factory polymorphism SR4f/GR2, §9.3.6 chain
> resolution), the separate factory roster (dual dispatch with instance names), `INVOKE Class "M"` →
> `CLS__FACTORY.__Instance.M(…)`, SELF/SUPER roster selection by context (SR4f–i) + SELF|SUPER "NEW"
> active-class creation (§16.2.1, `this.__New()`), factory-WS SR-10 auto-CONTENT free, 0836 (factory NEW
> name), `OBJECT REFERENCE FACTORY OF` staged 0899; oo_factory proves the §8.6.4 per-class copies
> (trap #11). **OVERRIDE/[IS] FINAL LANDED (DEVLOG 605)** — strict §11.7 SR4a/SR3 + the FINAL family
> (0837/0838/0839) and the TOTAL D7 modifier table (see the D7 AS-BUILT).
> **INTERFACE-ID + IMPLEMENTS + PROPERTY declarations LANDED (DEVLOG 606):** C# interface emission
> over the prototypes' signatures (§11.5/§11.6; prototype LINKAGE per §10.6.2 SR4; 0840 structural
> band), the BINDER-authoritative §9.3.11/§9.3.8.2.3 conformance pass over the §11.8.4 GR2 closure
> (**0841** — Roslyn is provably insufficient BOTH directions: 9(4)/9(8)→`ref long` under-reject;
> legal covariant returns over-reject, cured by explicit-interface-implementation ADAPTERS),
> interface-typed receivers (INVOKE over `AllPrototypes()` per §14.9.23.3 SR4e + the widening branch
> on every NEW/RETURNING/arg path), and PROPERTY declarations (§13.18.42: clause-synthesized accessors
> under the PINNED §11.7.4 GR1a names `__GET_/__SET_`, explicit GET/SET PROPERTY methods, the
> SR5/SR6/SR7 + §13.18.42.3 SR4 band = **0842**). Property REFERENCES (`P OF obj` → the §8.4.3.9.4
> implicit-INVOKE desugar) are the NEXT increment — staged LOUD under a NAMED 0899 at the
> ReferenceResolver chokepoint. **NEXT = property references → universal reference → EC-OO** (briefs
> in the LEDGER doc), then the Phase-4 catalog.

## Summary

OO COBOL → idiomatic .NET classes, designed fresh for the C#-native (Roslyn) target. The byte-model legacy kept its whole data engine and only swapped `ldsfld State`→`ldfld State`; the C#-native target instead emits ONE real C# class per CLASS-ID (the driver PROGRAM stays the static `Program` class), instance fields per OBJECT data item, real C# methods per METHOD-ID, and INVOKE → real C# `new`/`obj.M(...)`/`base.M(...)`. This is a major simplification: the one thing the byte model needed Mono.Cecil two-pass resolution for (cross-type method/type binding, virtual dispatch, parameter-conformance checks) Roslyn now gives for free — the residual "two-pass" belongs to the BINDER: pass-1 builds the class/method symbol table (class → method-name → {param modes, return}), pass-2 binds bodies, so each BoundInvoke carries its resolved call form (new/virtual/base/static/dynamic) and per-arg marshal mode (ref/value/copy) as bound-tree facts; BOTH backends (Roslyn primary, future CIL — G4 `ICodeGenBackend`) only render those facts, and the C# compiler merely re-resolves what the binder already validated.

TYPE-EMISSION MODEL: every CLASS-ID Foo → `public class Foo : CobolObject` (or `: Base`); a `CobolObject` runtime base hosts universal/dynamic dispatch + NULL/IS semantics reflection-free (rejected: derive from System.Object → universal dispatch then needs reflection). The current emitter is 100%-static/single-`Program`; refactor "emit fields+paragraphs+statements into a type" into a routine parameterized by (type, instance-vs-static, storage-source) so Program emits static and a class emits instance members on the SAME statement/PC-dispatch machinery. STORAGE SCOPES (two counterintuitive): OBJECT-para WORKING-STORAGE → INSTANCE fields; METHOD WORKING-STORAGE → STATIC fields (pre-2023 editions ONLY — **ILLEGAL in 2023 per §13.5.3 SR 1, see Spec corrections #1**; where legal: method WS persists across activations, shared across instances — NOT per-instance); METHOD LOCAL-STORAGE → C# locals (re-init each call); LINKAGE → method parameters; method-local names SHADOW object data (§11.7 GR5). The predefined NEW = the ctor: chain base ctor first (C# default, matches COBOL base-then-derived init), then VALUE-init this class's instance fields. FACTORY → a REAL sibling singleton class per CLASS-ID (`FOO__FACTORY : BASE__FACTORY | CobolObject` with `__Instance` + a covariant `__New` — brief D11, landed DEVLOG 604; the original static-members sketch is SUPERSEDED: §8.6.4 gives every class its OWN copy of inherited factory data, SELF-in-factory is polymorphic per §14.9.23.3 SR4f + §8.4.3.8 GR2, and §9.3.6 factory resolution walks INHERITS — three facts statics cannot satisfy).

INVOKE RESOLUTION (each grounded in a conformance .cob): `Class "NEW" RETURNING o`→`o=new Class()`; `obj "M" USING.. RETURNING..`→`obj.M(args)` virtual; `SELF "M"`→`this.M()` (virtual, §8.4.3.8 GR2 — runtime-class dispatch); `SUPER "M"`→`base.M()`; `Class "M"`(non-NEW)→static call. Dynamic/universal (method-name is a data item, or receiver is universal `object?`) cannot be a static C# call → a virtual `object? __CobolInvoke(string name, object?[] args)` on CobolObject whose body is a switch over the class's methods (reflection-free, AOT/WASM-safe); typed-literal INVOKE stays the fast path. INVOKE on a real .NET object (interop) is deferred — §14.9.23.4 GR2b makes non-COBOL INVOKE implementor-defined.

METHOD ATTRS map cleanly because COBOL forbids implicit hiding: instance methods → `virtual` by default (§9.3.6 runtime-class dispatch); OVERRIDE→`override`; FINAL→`sealed override`; §11.7 SR4a (redefining a base signature without OVERRIDE is an ERROR) means we NEVER emit C# `new`/hiding. (ABSTRACT is NOT ISO — vendor extension only; dropped from the ISO surface. **Spec corrections #4.**)

CORRECTNESS BLOCKER (the only one): GOBACK in a method ≠ STOP RUN. In a method GOBACK returns from the method only; STOP RUN ends the run unit. Define a distinct method-return path (a labeled return / `return` past the PC loop) vs the run-unit StopRun. NOTE (2026-07-03): the original "conflated at BIND time" citations are STALE — GOBACK and STOP RUN are ALREADY separate signals in the current pipeline (see D8's NOTE and "Greenfield seams"); the remaining gap is the METHOD-context binding (BoundMethodReturn) + EXIT METHOD edition handling. SETTLED (SSOT §18 item 18): COBOL allows MULTIPLE class inheritance (§11.3.2 `INHERITS FROM {name}…`), which `: Base` cannot express in C# — v1 restricts to single inheritance and rejects 2+ bases LOUDLY; a multi-base program triggers the deferred owner decision then.

## Spec corrections (2026-07-03 — regenerated brief)

> Changelog: these four corrections come from the regenerated off-repo brief (the lost
> `/e/tmp/phase2-briefs/oo-plan-brief.md`), each RE-VERIFIED against `specs/ISO_COBOL.md` (ISO/IEC
> 1989:2023) on 2026-07-03 with the line anchors below. The affected decisions/hard problems/edge
> cases carry matching CORRECTION notes.

1. **Method WORKING-STORAGE is ILLEGAL in 2023.** §13.5.3 Syntax rule 1 (specs/ISO_COBOL.md:16461): within a class definition the working-storage section may be specified only in a factory definition or an instance definition, "but not in a method definition"; nor in an interface definition. Corroborated by INVOKE SR 10 (:28443 — object WS exists but cannot be an INVOKE argument). Method definitions still take a full data division (§10.6 :12818) — LOCAL-STORAGE + LINKAGE are the 2023-legal method storage; method-vs-object name shadowing per §11.7.4 GR 5 (:13281). The live grammar still parses a full dataDivision inside methodDefinition (`Core/CobolOO.g4:43-49`), so the G7 EditionValidator must reject method WS at `--std 2023` with a versioned diagnostic; the D3 static-field mapping applies only in editions whose standard permits method WS (pin the exact edition boundary in `docs/VERSION_CHANGE_REFERENCE.md` during G7).
2. **EXIT METHOD was REMOVED in 2023.** §14.9.14.2 has exactly four EXIT formats — simple EXIT / EXIT PROGRAM [RAISING] (archaic) / EXIT PERFORM [CYCLE] / EXIT PARAGRAPH|SECTION — no METHOD or FUNCTION alternative (specs/ISO_COBOL.md:27346-27381); Annex E.2 explicitly lists "EXIT METHOD statement" (:49034) and "EXIT FUNCTION statement" (:49036) among removals since the previous standard. In 2023 methods terminate via GOBACK or by falling out of the procedure division. The shared grammar still accepts `EXIT METHOD [RAISING]` (`Core/CobolControlFlow.g4:213`) — edition-gate it: a method-return synonym at 2002/2014, a removed-feature diagnostic at 2023.
3. **INVOKE has NO ON EXCEPTION phrase (in ANY spec surface designed here).** The §14.9.23.2 general format is exactly `INVOKE {class|identifier} {method} [USING …] [RETURNING identifier-4]` — no ON EXCEPTION / NOT ON EXCEPTION and no handler inside END-INVOKE (specs/ISO_COBOL.md:28376-28390). Failures surface as EC exception conditions instead: null receiver → EC-OO-NULL (GR 5, :28506); method not found / insufficient resources → EC-OO-METHOD (:28528) — routed through the EXISTING §14.6.13 EC machinery (landed DEVLOG 577: declaratives, >>TURN, EXCEPTION-* functions), never a handler phrase. This differs from CALL. The live grammar is already correct (invokeStatement has no exception alternative); the dead sketch `CobolParserOO.g4`'s `invokeOnException` must NOT be revived.
4. **ABSTRACT is NOT ISO.** A grep over the entire spec returns zero matches — ABSTRACT is not a reserved word, clause, or concept anywhere in ISO 1989:2023. CLASS-ID's only modifiers are `[AS literal-1] [IS FINAL] [INHERITS FROM …] [USING …]` (specs/ISO_COBOL.md:12742-12744), and a method definition's only attributes are `[OVERRIDE] [IS FINAL]` (plus the GET/SET PROPERTY selector form, :12798-12821). Any "ABSTRACT class" notion is a vendor extension (Micro Focus/IBM) and must not appear as ISO surface in this design.

## Version gating (G1 — four compilers in one)

The ENTIRE OO subsystem (CLASS-ID/FACTORY/OBJECT/METHOD-ID, INVOKE, USAGE OBJECT REFERENCE, SELF/SUPER, the predefined NEW, INTERFACE-ID/IMPLEMENTS, SET … TO object-reference, repository CLASS entries) is introduced in COBOL-2002 and present in 2014/2023. Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Concretely:
1. `--std 2002|2014|2023` → the full per-edition spec behavior designed in this doc.
2. `--std 85` → every OO construct is REJECTED with a specific edition diagnostic (e.g. "CLASS-ID requires COBOL-2002 or later; compiling as COBOL-1985"), NOT a generic syntax error. The live grammar already gates the parse with `{is2002()}?` predicates (CobolParserCore.g4: classDefinition in compilationGroup, the repository CLASS entry, invokeStatement, PD returningClause, BY VALUE args, SET-to-objectReference, GOBACK RETURNING); a diagnostic pass must map each gated rule to its versioned diagnostic so the version test matrix's negative corpus (`docs/VERSION_TEST_MATRIX_DESIGN.md` — the (construct × edition) matrix; Phase 0 done) can assert it.
3. Edition deltas WITHIN OO (2002→2014→2023) are gated by DialectLevel per `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist — 2002→2023 deltas ONLY; OO's own 2002 introduction has NO row there and derives from the 2002 standard) (e.g. row 97: FLAG-02 flagging of EC-PROGRAM TURN in elements that invoke methods, 2002→2014; row 15: external-item conformance-check ECs, 2014→2023) — never hard-coded to 2023.
Positive conformance tests live in `tests/conformance/2002/oo_*.cob`; the rejected-at-85 cases are matrix cases, never skipped.

## Decisions

### D1. One C# type per CLASS-ID; the driver PROGRAM stays the static `Program` class; the BINDER owns cross-type resolution and parameter/return conformance DIAGNOSTICS (backend-neutral, G4); Roslyn re-checks the rendered C# as a safety net only.

**Rationale.** The C#-native target's defining advantage: the byte model needed Mono.Cecil two-pass resolution (define all type+method signatures, then bodies) purely to bind cross-type INVOKE and emit callvirt; emitting C# source hands all of that to the C# compiler. The residual 'two-pass' is the BINDER's: pass-1 builds the class/method symbol table (class→method→{param modes,return type}); BoundInvoke then carries the chosen call form (new/virtual/base/static/dynamic) and per-arg marshal modes, so both backends (Roslyn now, CIL later — G4) render the same bound facts. A user-visible Roslyn CS error from emitted code is an emitter BUG (loud-failure invariant), never the user diagnostic surface. ISO §9.3.6 runtime-class dispatch ≡ C# virtual dispatch exactly.

**Rejected alternatives.** Port the legacy per-instance-ProgramState model (instance `State` byte field + `ldarg.0;ldfld State`) — rejected by the OWNER-LOCKED no-byte-substrate rule and because it throws away Roslyn's free type-checking. Emit via Mono.Cecil like legacy — rejected: re-introduces the manual cross-type MethodDefinition registry the source target eliminates.

### D2. Introduce a `CobolObject` runtime base class; every emitted COBOL class is `class Foo : CobolObject` (or `: Base` when it INHERITS, whose own root is CobolObject).

**Rationale.** Gives a single reflection-free home for universal/dynamic dispatch (`__CobolInvoke`), object-identity/NULL/`IS class` semantics, and a future EC-OO surface — AOT/WASM-safe (no Type.GetType, no reflection).

**Rejected alternatives.** Derive each class straight from System.Object — rejected: universal-object-reference dispatch (`INVOKE U name`) and `obj IS Class` would then require reflection or a marker interface, defeating AOT safety. A marker interface ICobolObject instead of a base class — rejected: a base class can carry the default `__CobolInvoke` body and shared state; an interface cannot supply a default reflection-free dispatcher cleanly.

### D3. Storage-scope mapping: OBJECT-paragraph WORKING-STORAGE → INSTANCE fields; METHOD WORKING-STORAGE → STATIC fields; METHOD LOCAL-STORAGE → C# locals (re-init per call); LINKAGE → method parameters. Method-local names shadow object data.

**Rationale.** ISO §11.8/§11.7: object WS is the per-instance object state; a method's WORKING-STORAGE persists across activations and is SHARED across instances (so it is static, NOT per-instance — the counterintuitive case); LOCAL-STORAGE is re-initialized on each entry (locals); §11.7 GR5 makes a method-local name shadow the same name in object data.

**Rejected alternatives.** Make method WORKING-STORAGE instance fields (the naive 'it's inside the object' reading) — rejected: violates §11.7 persistence/sharing semantics and would silently miscompile a counter kept in method WS. Make LOCAL-STORAGE static — rejected: LOCAL-STORAGE must re-init each activation.

**CORRECTION (2026-07-03, regenerated brief — Spec corrections #1).** In ISO 2023 a method definition may NOT contain a working-storage section at all (§13.5.3 SR 1, specs/ISO_COBOL.md:16461; interfaces get none either). So the method-WS→static-fields mapping is a pre-2023 behavior only, edition-gated; at `--std 2023` method WS is a compile-time versioned diagnostic from the EditionValidator. The 2023-legal method storage is LOCAL-STORAGE (→ C# locals) + LINKAGE (→ parameters); OBJECT/FACTORY WS is unaffected.

**AS BUILT (2026-07-04, slice 2 — DEVLOG 602).** D3 is LIVE in full: method WS → `private static` fields (the two-instance shared-counter fact is `OoSpineTests.MethodWorkingStorage_StaticSemantics_EditionWindow`); LOCAL-STORAGE → C# locals declared in the method entry (re-init per activation is the language semantics of a local declaration); LINKAGE → `ref` parameters copied into CAPTURABLE locals (see the D6/D8 notes for why locals, not raw params). The 2023 ban is the `method-working-storage-window` registry row (0900 below 2002 / 0902 at 2023; `--permissive` keeps the static semantics — the §10 #1 migration contract, matrix-verified). ⚠ The pre-2023 legality boundary is PINNED PROVISIONAL at 2002–2014 (Annex E.2 does not itemize the removal and the 2002/2014 texts are not in-repo — VCR Table 6 row 130e records the pin and the one-line `removedIn` shift if the 2014 text proves otherwise). Method-data shapes staged loud for later waves: REDEFINES / OCCURS INDEXED / ODO / level-66 / EXTERNAL / GLOBAL inside method data, and FILE/REPORT/SCREEN sections in a method.

### D4. The predefined NEW factory = the generated public ctor: base ctor chains first, then VALUE-initialize this class's own instance fields.

**Rationale.** C# already runs base-then-derived ctor order, which matches COBOL initialize-inherited-then-own-data; §16.2.1 NEW is the built-in factory needing no explicit FACTORY METHOD-ID.

**Rejected alternatives.** A separate static `NEW()` factory method calling a parameterless ctor + an Initialize method (the legacy InitializeState shape) — rejected: redundant in C#; the ctor IS the initializer and field initializers + ctor body cover VALUE.

### D5. INVOKE call-form table: Class "NEW" RETURNING o → `o=new Class()`; obj "M" → `obj.M(args)` (virtual); SELF "M" → `this.M()`; SUPER "M" → `base.M()`; Class "M" (non-NEW) → static call; dynamic/universal → `recv.__CobolInvoke(name,args)`.

**Rationale.** Direct, idiomatic, statically-resolved (AOT-safe) for the literal-method 90% path; matches §9.3.6 (object→instance/factory resolution, SUPER restricted search), §8.4.3.8 (SELF virtual on runtime class, SUPER non-virtual).

**Rejected alternatives.** Route ALL invokes through a single `__CobolInvoke` string-switch (uniform but slow, unidiomatic, defeats Roslyn overload/type checks) — rejected; reserve __CobolInvoke only for the genuinely-dynamic cases. The legacy uniform `callvirt CobolProgramEntry.Invoke(ManagedPointer[])` ABI — rejected: not idiomatic C#, hides types from Roslyn.

### D6. Parameter passing: BY REFERENCE → C# `ref` of the typed field (value-class items) or the reference itself (object/string); BY CONTENT → pass a copy; BY VALUE → value parameter; RETURNING → C# return value; OMITTED → nullable param + omitted-arg condition.

**Rationale.** §9.3.6 match-rule 3c requires a BY REFERENCE argument and its formal parameter to be the same class/category — so a real typed `ref` parameter is fully conformant; this is precisely why the C#-native target can use typed parameters instead of the legacy ManagedPointer[] byte ABI. RETURNING-as-return is the idiomatic and §14.9.23 GR8-faithful mapping.

**Rejected alternatives.** Keep the ManagedPointer[] args ABI (legacy) for uniformity with CALL — rejected: byte-substrate-adjacent, unidiomatic, and unnecessary now that types are native. Box everything to object[] for all params — rejected: loses Roslyn type checking and adds boxing.

**AS BUILT (2026-07-04, slice 2 — DEVLOG 602; CORRECTED by the DEVLOG-603 adversarial review).** Every formal is a `ref` parameter (uniform; the value-param-when-never-written polish is deferred — deciding it needs the body's write-set, which isn't known when signatures must exist). The load-bearing realizations, as CORRECTED by the 45-agent find→verify workflow (22 confirmed findings, all landed):
1. **The conformance rule is selected by the EFFECTIVE passing mode** — the original slice-2 claim ("§14.8.2 strict conformance everywhere") misread the spec: the identical-description rule is **§14.8.2.3.2, BY REFERENCE ONLY** (now incl. SIGN representation via `ImageSignKind`, BLANK WHEN ZERO, JUSTIFIED — the review's silent-image-mismatch fix; and the §14.8.2.2 rule-1 GROUP-PREFIX allowance: a by-ref group formal may be SMALLER than the argument — the callee sees the leading positions, the write-back SPLICES the prefix and preserves the tail). **BY CONTENT** (explicit, SR-10 auto-CONTENT, and all literals) follows **§14.8.2.3.3**: COMPUTE rules for numeric formals (ANY fixed-point numeric argument/literal — converted by rescale+truncate through the OWNER class's `internal` profile, qualified `{OWNER}._P_n`; the cross-class-profile rule), SET rules for object references (WIDENING — same class or subclass; universal receivers accept anything — `OoClassTable.ObjectRefWideningMismatch`), MOVE rules otherwise (pad/truncate; integer-display→alnum legal); §9.3.6 rule 5 makes TRUNCATING literals conforming. **RETURNING delivery** follows §14.8.3.3 rule 1 = SET rules (subclass results deliver into superclass-typed and universal receivers; C# 9+ covariant returns render override covariance, §9.3.8.2.3 5a/5c2). A reference-modified argument conforms by its EFFECTIVE description (elementary alphanumeric of the window length, §8.4.2.4).
2. **§14.9.23.3 SR 10 auto-CONTENT**: a bare OBJECT-data argument cannot cross BY REFERENCE — GR6a2 assumes BY CONTENT (callee writes invisible); explicit BY REFERENCE of object data is 0828 (`DataBinder.OoIsObjectData`).
3. **Call-site lowering**: a plain field of matching storage passes `ref` DIRECTLY (subscripts evaluate once — GR7a for free); everything else is copy-in temp → `ref` temp → copy-out (BY REFERENCE identifiers only), groups crossing as character images, storage-form bridges via caller-side `FormatDisplay`/`ParseDisplay`/`NumericImagePlace`; float formals read the float value directly (never the scaled-integer path — the review's truncation fix). **Emission order per GR8** (the review's overlap fix): the call → the BY REFERENCE copy-outs → the RETURNING store into identifier-4 LAST.
4. **Crossing-form harmonization** (`OoHarmonizeOverrideCrossings`, the review's CS0115-desync fix): after MarkStoreAsImage, override chains UNION the image-stored form across corresponding formal/RETURNING pairs to a fixed point, so base and override always emit the same C# signature.
5. **Emitted-name safety**: a METHOD-ID named like its CLASS-ID renames the SYMBOL (`_M` suffix — §8.3.2.2's implementor-defined externalized mapping); overrides adopt the base slot's CsName verbatim; the one unrepresentable corner (a derived class named like an inherited slot) is a 0820 restriction diagnostic. Numeric profiles emit `internal` so CONTENT conversions qualify them cross-class.
BY VALUE args stage 0828 pending the unparsed header BY-phrases; OMITTED, cross-float CONTENT conversion, and dynamic-length refmod BY REFERENCE args are documented later refinements (loud today).

### D7. Method attributes: instance methods are `virtual` by default; OVERRIDE→`override`; FINAL→`sealed override` (FINAL root → non-virtual); FACTORY methods→`static`. Never emit C# `new`/method-hiding.

**Rationale.** §9.3.6 dispatch is always on the runtime class → virtual by default. §11.7 SR4a makes redefining a base signature WITHOUT OVERRIDE a compile error, so COBOL never expresses C# hiding — the mapping stays clean and total.

**Rejected alternatives.** Emit non-virtual methods and only mark virtual when overridden somewhere — rejected: requires whole-program analysis to know if a class is ever subclassed and breaks separate compilation; virtual-by-default matches the spec directly.

**CORRECTION (2026-07-03, regenerated brief — Spec corrections #4).** This decision originally mapped `ABSTRACT→abstract (+abstract class)`; ABSTRACT is NOT ISO (zero occurrences in the whole spec; CLASS-ID modifiers are only AS/FINAL/INHERITS/USING, :12742-12744; method attributes only OVERRIDE and [IS] FINAL, :12798-12821). ABSTRACT (and STATIC/visibility attributes) are vendor extensions — out of the ISO surface; FACTORY methods emit as `virtual`/`override` members of the FACTORY SINGLETON class (§11.4; brief D11 — NOT C# statics: SELF-in-factory dispatches on the runtime factory, SR4f/GR2), never via a method attribute. If a vendor-dialect ABSTRACT is ever wanted it is a dialect-gated extension, never default 2023 surface.

**AS BUILT (the OVERRIDE/FINAL wave — DEVLOG 605).** The attributes are LIVE: `METHOD-ID … [OVERRIDE] [IS FINAL]` + `CLASS-ID … [IS FINAL]` parse (spec order; the OVERRIDE token via the XOR-recipe — user word at 85, 0901 ≥2002), and STRICT §11.7 is enforced at pass-1: SR4a redefinition-without-OVERRIDE = **0837** through the ONE `EditionContext.Removed` policy seam (error strict; warning + the pre-wave name-match inference under `--permissive` — the documented migration leniency); SR3 OVERRIDE-without-a-base-method = **0838**; the FINAL-violation family (override of a FINAL method, GR3; INHERITS FROM a FINAL class, §11.3 GR3) = **0839**. Both rosters (instance + factory) take identical rules. Emission is the TOTAL D7 table: override → `override` (`sealed override` when itself FINAL in a non-sealed class); FINAL root method or ANY fresh slot in a FINAL class → NON-virtual; FINAL class → `sealed` (both type halves — and a sealed factory's root `__New` is non-virtual: a `virtual` member in a `sealed` type is Roslyn **CS0549** on emitted code, the trap the table exists for, caught live by the oo_override_final golden's first compile). SR2/SR8 (no attributes in method PROTOTYPES) is a FORWARD OBLIGATION of the INTERFACE-ID wave.

**AS BUILT (the INTERFACE/PROPERTY wave — DEVLOG 606).** INTERFACE-ID (§11.5/§11.6) is LIVE end-to-end.
GRAMMAR: `interfaceDefinition` ({is2002()}? in compilationGroup; INTERFACE_ID token; interface INHERITS
repetition supported — C#-native, the deliberate asymmetry with single class inheritance), the
`implementsClause` in both FACTORY/OBJECT paragraphs, `methodPropertySelector` on METHOD-ID, the
`propertyClause` in dataDescriptionClause, and the REPOSITORY INTERFACE/PROPERTY specifiers. WORDS:
GET/PROPERTY/INTERFACE are §8.9-reserved 2002+ via the XOR recipe (user words at 85, 0901 ≥2002);
IMPLEMENTS is §8.10 CONTEXT-SENSITIVE (spec :10853) — a user word at EVERY edition, token + cobolWord but
NEVER CheckedTokenTypes. The VALUE clause needed a loop guard (both the valueItem list AND the
multi-operand `valueClauseOperand+` item): at 2002+ PROPERTY terminates a VALUE clause — reserved, never a
constant-name operand — else `VALUE 100 PROPERTY.` swallows the clause. PASS-1: interfaces build FIRST
(OoInterfaceSymbol; prototypes via TryAddPrototype; the 0840 structural family — one class/interface
namespace §8.3.2.2, END INTERFACE §10.7, SR2/SR8 no prototype attributes, §10.6.2 SR4 header-only +
LINKAGE-only data division); prototype LINKAGE binds through the SAME OoBindMethodData machinery
(`OoBindInterfaceData`), so ValidateImplements compares RESOLVED descriptions. CONFORMANCE: the
§9.3.11-via-§9.3.8.2.3 pass over the §11.8.4 GR2 closure (`ImplementsClosure`: direct + interface-INHERITed
+ class-INHERITed, cycle-safe; instance and factory sides separately) is BINDER-authoritative = **0841**,
because the C# projection is insufficient in BOTH directions: PIC 9(4) and 9(8) formals both emit
`ref long` (Roslyn under-rejects — the identical-description rules 2/3 live only in DescriptionMismatch),
and C# forbids the covariant interface-implementation returns that rules 5a/5c2 PERMIT (Roslyn
over-rejects — cured by `AdapterPairs` → explicit interface implementations
`PROTO_RET IFACE.M(…) => this.M(…);` as headerExtras on the instance half). EMISSION: `public interface
IFOO [: BASES]` with FieldEmitter statics (numeric profiles + group structs — C# 8+ interface statics, so
cross-unit CONTENT conversions qualify `{IFACE}._P_n`) + the prototypes' signatures through the ONE
`OoSignatureOf` builder (shared with class methods and adapters — the no-drift rule); the instance base
list joins direct Implements (the closure arrives transitively at the C# level); the factory half takes
FactoryImplements (D11 singletons made factory IMPLEMENTS EMITTABLE — the brief's validate-only posture is
SUPERSEDED). RECEIVERS: an interface-typed `USAGE OBJECT REFERENCE IFOO` is legal (0813 accepts interfaces,
§13.18.60.2); INVOKE through it resolves over `AllPrototypes()` (§14.9.23.3 SR4e; 0825 on a miss) and emits
static C# interface dispatch behind the same GR5 null guard; `ObjectRefWideningMismatch` gained the
interface branch (class→implemented-interface via the closure; interface→inherited-interface) and ALL
NEW/RETURNING/argument paths route through it. PROPERTY (§13.18.42) DECLARATIONS are LIVE: the clause
synthesizes accessors per GR1/GR2 under the PINNED §11.7.4 GR1a names `__GET_<P>`/`__SET_<P>` — the
"clone" of the subject description is the SUBJECT DataItem itself (identical by construction), and the
emitter renders DIRECT field bodies (`=> subject;` / `{ subject = __V; }`) — observably identical to the
spec's implicit-MOVE methods; WITH NO GET/SET suppresses a side; FINAL carries. Explicit
`METHOD-ID. GET|SET PROPERTY p` methods join the roster under the SAME pinned names (real bodies), so
override/0829/implements machinery applies to accessors UNCHANGED. The 0842 band: SR6/SR7 accessor shapes,
SR5 clause+explicit duplicate, §13.18.42.3 SR4 superclass property collision, no-OCCURS subject, no-FILLER
subject. STAGED (named 0899, never a generic guard): property REFERENCES (`P OF obj` — the §8.4.3.9.4
GR1–GR3 implicit-INVOKE desugar with BoundSequence + temps; detected at the ReferenceResolver
resolution-failure chokepoint by the single-qualifier + roster-property shape) and GET/SET PROPERTY
prototypes in interfaces. REGISTRY: interface-definition-2002, repository-interface-2002,
repository-property-2002, implements-clause-2002, property-clause-2002, method-property-selector-2002
(constructs.json rows for the four independently-reachable gates; W1.5 EditionGateHints for the
INTERFACE-ID unit + both repository specifiers + the property clause). GOLDENS: oo_interface (two classes
implementing one interface, polymorphic dispatch through an interface-typed reference), oo_interface_covariant
(the adapter, compile+run), oo_property, oo_property_methods.

### D8. GOBACK inside a METHOD returns from the method only; STOP RUN ends the run unit. Emit a method-return path (a `return`/labeled break out of the method's PC-dispatch loop) distinct from the `StopRun` exception.

**Rationale.** §14.9.43/method semantics: GOBACK in a method is a normal method return (control + any RETURNING value go back to the INVOKE site); STOP RUN terminates the whole run unit. Fix in the BINDER (G4): STOP RUN → `BoundStopRun`; GOBACK in a method context → a distinct `BoundMethodReturn` (carrying the RETURNING item, if any); each backend renders its own form. This is the single decision that silently miscompiles if missed.

**Rejected alternatives.** Keep throwing StopRun and catch it at each method boundary — rejected: a caught StopRun loses the RETURNING value and is fragile across nested INVOKEs; an explicit method return is correct and idiomatic.

**NOTE (2026-07-03 — the original citations were STALE; decision UNCHANGED).** This decision originally claimed the pipeline "conflates both at BIND time (StatementBinder.cs ~line 91 → one BoundStop → throw new StopRun(), CSharpEmitter.cs ~line 185)". Current reality: the interprogram wave ALREADY un-conflated the two signals — STOP → `BoundStop` (`src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.cs:168`) → `throw new StopRun()` (`src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.cs:333`); GOBACK binds separately via `CallBindGoback` (StatementBinder.cs:169) → `BoundGoback` → `CallEmitGoback` (`CSharpEmitter.Call.cs:803-816`), which moves the RETURNING source, stages RAISING, and throws `ProgramReturn` — caught at the program's own `__Activate` (CSharpEmitter.cs:120), never crossing a CALL boundary (ProgramReturn = the settled SSOT §18 #10 carrier, `ProgramRegistry.cs:28-35`). The REMAINING gap this decision governs: (a) method-context GOBACK must become `BoundMethodReturn` (or reuse the ProgramReturn-style catch-at-entry pattern scoped to the method — the D8 `return` form remains preferred); (b) EXIT METHOD is currently `BoundUnsupported` (StatementBinder.cs:218) and is edition-dependent: a method-return synonym at 2002/2014, REMOVED in 2023 (**Spec corrections #2**, specs/ISO_COBOL.md:27346-27381, :49034) — it maps to the method-return path only in pre-2023 editions and to a removed-feature diagnostic at 2023.

**AS BUILT (2026-07-04, spine part 2 — DEVLOG 601): the catch-at-entry form was chosen over the `return` form, deliberately.** A method-context GOBACK executes inside however many NESTED bounded `__Dispatch(start, end)` frames its out-of-line PERFORMs have stacked — a C# `return` exits ONE frame, resuming the outer PERFORM's loop (a silent wrong-control-flow miscompile; the day-one test `OoSpineTests.MethodGoback_ReturnsFromMethodOnly_UnwindingNestedPerformFrames` reproduces exactly this shape). So the realization mirrors ProgramReturn precisely: `BoundMethodReturn` → `throw new MethodReturn()` (`src/Cobol.Net.Runtime/Control/MethodReturn.cs`), caught at the method's public entry — `public virtual void M() { try { __Dispatch(entry, last); } catch (MethodReturn) { } }` — after which the entry returns the method's RETURNING item (LIVE as of slice 2: the RETURNING LINKAGE local is the C# return value). The RETURNING value is NOT lost (the D8 rejection concerned conflation with StopRun, not the signal mechanism). STOP RUN inside a method deliberately stays `StopRun` — it must NOT be caught at the method boundary (§14.9.43). EXIT METHOD binds to the same `BoundMethodReturn` in a method (`OoBindExitMethod`), 0827 outside one; its 2023 removal rides the pre-existing `exit-method-window` registry row (0900 below 2002 / 0902 at 2023).

### D9. Defer parametric polymorphism (method overloading by signature); v1 requires unique method names per class.

**Rationale.** Parametric polymorphism is an OPTIONAL feature in ISO §12063 ('optional feature in this Working Draft'). COBOL resolves overloads by method-resolution-signature (PICTURE/USAGE/category, §12063) but C# resolves by .NET type — lossy: PIC 9(4) and PIC 9(8) both map to `long`, colliding in C#. No conformance/NIST OO program uses overloading (verified: every METHOD-ID name is unique within its class). Deferring is safe and spec-permitted.

**Rejected alternatives.** Name-mangle each method by its full COBOL resolution signature (e.g. M__9_4) to keep distinct C# methods — rejected for v1: unidiomatic output and unneeded by the corpus; keep as the documented escape hatch if a future program needs overloading.

### D10. Universal/dynamic INVOKE → a virtual `object? __CobolInvoke(string name, object?[] args)` on CobolObject; each class overrides with a switch over its method roster.

**Rationale.** A method-name-in-a-data-item or a universal `object?` receiver cannot bind statically; a per-class generated switch is reflection-free and AOT/WASM-safe, and the literal-typed path stays the fast direct call.

**Rejected alternatives.** Reflection (MethodInfo.Invoke) — rejected: not AOT/WASM-safe, slow, and forbidden by the interop AOT rule. A global Dictionary<(Type,string),Delegate> — rejected: still needs runtime type lookup and is less debuggable than a per-class switch.

## C# mapping

TYPE + NEW + DISPLAY (from oo_hello.cob):
  CLASS-ID. GREETER. / OBJECT. / 01 MSG PIC X(13) VALUE "HELLO, WORLD!". / METHOD-ID. SAYHELLO. ... DISPLAY MSG.
  →
    public class GREETER : CobolObject {
        private string _MSG = CobolString.Store("HELLO, WORLD!", 13);  // instance field (OBJECT WS), VALUE-init in ctor
        public GREETER() { /* base() first; then VALUE inits */ _MSG = CobolString.Store("HELLO, WORLD!", 13); }
        public virtual void SAYHELLO() { System.Console.WriteLine(_MSG); }
    }
  Driver: INVOKE GREETER "NEW" RETURNING G  →  G = new GREETER();
          INVOKE G "SAYHELLO"               →  G.SAYHELLO();

LINKAGE (ref/returning) + per-instance state (from oo_method_args.cob):
  OBJECT WS: 01 BAL PIC 9(4). METHOD ADDTO / LINKAGE 01 LK-AMT PIC 9(4) 01 LK-RES PIC 9(4) / PD USING LK-AMT RETURNING LK-RES / ADD LK-AMT TO BAL / MOVE BAL TO LK-RES.
  →
    private long _BAL = 0L;  // per-instance
    public virtual long ADDTO(long LK_AMT) {        // USING→param (BY REFERENCE default; value-class so pass-by-value safe here), RETURNING→return
        _BAL = CobolNum.Store(_BAL + LK_AMT, 0, _P_BAL);
        return _BAL;                                 // MOVE BAL TO LK-RES then RETURNING LK-RES
    }
  INVOKE A1 "ADDTO" USING AMT RETURNING R  →  R = CobolNum.Store(A1.ADDTO(AMT), 0, _P_R);
  (BY REFERENCE for a typed value item that the method WRITES through → `ref long LK_AMT`; emit `A1.ADDTO(ref AMT)`. Justified by §9.3.6 match-rule 3c: BY REFERENCE requires same class/category, so a real typed `ref` is conformant — this is exactly why we drop the legacy ManagedPointer[] byte ABI.)

INHERITS + SUPER (from oo_super.cob):
  CLASS-ID. DOG INHERITS FROM ANIMAL. / METHOD SPEAK / INVOKE SUPER "SPEAK" / DISPLAY "DOG".
  →
    public class DOG : ANIMAL {
        public override void SPEAK() { base.SPEAK(); System.Console.WriteLine("DOG"); }
    }
  (ANIMAL.SPEAK is `public virtual void SPEAK()`. INVOKE D "SPEAK" on a DOG dispatches to DOG.SPEAK virtually.)

POLYMORPHIC SELF (from oo_self_polymorphic.cob):
  ANIMAL.DESCRIBE (inherited by DOG): INVOKE SELF "SOUND"; DOG overrides SOUND.
  →
    public class ANIMAL : CobolObject {
        public virtual void DESCRIBE() { System.Console.WriteLine("DESCRIBING:"); this.SOUND(); }  // this.SOUND() is virtual
        public virtual void SOUND() { System.Console.WriteLine("GENERIC"); }
    }
    public class DOG : ANIMAL { public override void SOUND() { System.Console.WriteLine("WOOF"); } }
  On a DOG, D.DESCRIBE() → this.SOUND() → DOG.SOUND() → "WOOF". (§8.4.3.8 GR2: SELF resolves on the runtime class.)

OBJECT GROUP + OCCURS per-instance (from oo_object_group.cob):
  OBJECT WS: 01 PERSON. 05 PNAME PIC X(4) VALUE "ANN". 05 PAGE-N PIC 9 VALUE 0. / 01 TBL. 05 SLOT PIC 9 OCCURS 3 VALUE 0.
  →
    private _T_PERSON _PERSON = new();                 // per-instance nested record struct
    private long[] _SLOT = new long[3];               // per-instance array; MOVE n TO SLOT(k) → _SLOT[k-1]=...
  (Group→record struct + fixed OCCURS→T[] mapping is identical to PROGRAM data; the ONLY delta is instance vs static field — no byte image.)

OBJECT REFERENCE storage (universal vs typed):
  01 G USAGE OBJECT REFERENCE GREETER. → private GREETER? G = null;
  01 U USAGE OBJECT REFERENCE.          → private object? U = null;   // universal → object?
  SET G TO NULL → G = null;   IF G = NULL → G is null;   IF G IS GREETER → G is GREETER.

DYNAMIC/UNIVERSAL dispatch (no conformance test yet — design completeness):
  INVOKE U meth-name USING X  →  U.__CobolInvoke(MethName, new object?[]{ X });   // U:CobolObject
  // CobolObject defines `public virtual object? __CobolInvoke(string n, object?[] a){ throw EC_OO_METHOD; }`
  // each class overrides: switch(n){ case "SAYHELLO": SAYHELLO(); return null; ... default: return base.__CobolInvoke(n,a); }

## Hard problems

### COBOL permits MULTIPLE class inheritance — §11.3.2: `CLASS-ID. name … [INHERITS FROM { object-class-name-2 } … ]`. C# has only single class inheritance, so `: Base` cannot express two+ COBOL superclasses.

v1 RESTRICTS to single inheritance (every conformance/NIST program uses single `INHERITS FROM` — verified). Reject 2+ bases LOUDLY with a dedicated diagnostic. Settled in SSOT §18 item 18 (deferred owner decision — triggered only when a multi-base program actually appears). The decided escape hatch when it is needed: linearize the inheritance graph to one C# base chain + extract the secondary supers' instance interfaces into C# interfaces the class IMPLEMENTS, copying/forwarding the secondary supers' members — but that is design-deferred until a program demands it.

### GOBACK vs STOP RUN inside a method (the only correctness blocker). A method-context GOBACK must return from the method (with the RETURNING value), never unwind past the INVOKE caller.

Bind by context (G4 — semantics live in the binder, not an emitter flag): inside a method body the binder binds GOBACK to `BoundMethodReturn` (with the RETURNING item, if any) and STOP RUN to `BoundStopRun`; emitters only render each node. In a method, GOBACK (and, in pre-2023 editions only, EXIT METHOD — **Spec corrections #2**) → emit a C# `return` (with the RETURNING value if any) that exits the method's PC-dispatch loop; STOP RUN keeps throwing the run-unit `StopRun`. The PROGRAM (Main) keeps the existing try/catch(StopRun). NOTE (2026-07-03): the earlier "verified concretely: StatementBinder.cs ~91 conflates both" claim is STALE — GOBACK and STOP RUN already bind to distinct nodes (BoundGoback→ProgramReturn vs BoundStop→StopRun); see D8's NOTE and "Greenfield seams" for the current signal architecture. What remains is exactly the method-context split this problem describes.

### Per-instance vs static field selection must thread through the WHOLE statement/expression emitter (DISPLAY/MOVE/arithmetic/PERFORM read & write data items), not just field declarations — the legacy solved this with one chokepoint (ldarg.0;ldfld vs ldsfld).

Refactor the emitter's 'emit a data item reference' to consult the bound item's owning scope: an instance object item emits `this._FIELD` (or `obj._FIELD` for a qualified receiver — but COBOL OO data is always SELF-relative inside a method), a static/program/method-WS item emits the bare static field. Because the statement emitters all funnel through one ReadAsString/Resolve/field-name path (CSharpEmitter Resolve + CsName), this is a single parameterization, exactly mirroring the legacy chokepoint but in source. NOTE (2026-07-03): the interprogram CallUnit machinery has since made this problem SMALLER than framed here — program classes already emit INSTANCE fields with per-unit emitter state, and inside its own class an instance field needs no prefix at all; the outer-storage case is solved with `private ref T fld => ref __outer.fld` bridge properties (see "Greenfield seams").

### DataBinder/DataItem/PicInfo currently model ONLY PICTURE items and silently ignore USAGE OBJECT REFERENCE (no PicCategory for it), so object-reference fields and INVOKE targets cannot be typed or resolved.

Add an ObjectReference item kind to the bound model carrying {ClassName | universal-flag}: typed → C# field type `ClassName?`, universal → `object?`. The binder reads `objectReferenceUsage` (Core/CobolOO.g4 — `OBJECT REFERENCE className` vs bare `OBJECT REFERENCE`). INVOKE binding resolves the target item to learn its declared class for choosing instance dispatch; a class-name target (not a data item) means NEW/static-factory dispatch.

### Cross-compilation-unit resolution: the driver PROGRAM and each CLASS are separate program units in one compilation group; INVOKE in the driver references a class defined later in the file, and a method body references sibling methods/inherited members.

A genuine two-PASS over the compilation group, owned by the BINDER (G4): pass-1 walk every classDefinition and build the binder's class symbol table (class name, base name, method roster with each method's USING param modes + RETURNING presence/type, FACTORY-vs-OBJECT, attributes); pass-2 emit all class types and the Program into ONE C# source file (one compilation), so the C# compiler resolves all cross-type and inheritance references. The symbol table is used only to marshal INVOKE args and select the call form.

### INVOKE on a NULL object reference must raise EC-OO-NULL (§14.9.23.4 GR5) through the EC engine, not throw a raw NullReferenceException.

For a typed INVOKE emit a guard `recv ?? throw new CobolException(EC.OO_NULL)` (or a small `CobolObject.RequireNonNull(recv)` helper) before the call. **CORRECTION (2026-07-03, regenerated brief — Spec corrections #3):** this problem originally planned to "wire the ON EXCEPTION / NOT ON EXCEPTION phrases … to a try/catch over the call" — WRONG: INVOKE has NO exception phrase in ISO (the §14.9.23.2 format ends at RETURNING, specs/ISO_COBOL.md:28376-28390; the dead sketch's `invokeOnException` must not be revived). INVOKE failures route exclusively through the landed §14.6.13 EC machinery (DEVLOG 577): EC-OO-NULL (:28506) / EC-OO-METHOD (:28528) raised as fatal exception conditions, handled by USE declaratives / >>TURN checking like every other EC — no per-statement handler. The full EC-OO family (Table 13, :24748-24756) is integrated in the EC-OO slice.

### Method LINKAGE + PROCEDURE DIVISION USING/RETURNING: the live grammar carries USING on procedureDivision and RETURNING gated {is2002()} — but mapping positional COBOL params (BY REFERENCE default) to typed C# params requires per-method param-mode info at the INVOKE site, across units.

The pass-1 symbol table records each method's ordered formal parameters with mode (REFERENCE/CONTENT/VALUE/OMITTED-capable) + type, derived from its LINKAGE items and the PD USING/RETURNING. At an INVOKE, marshal each argument per §14.9.23.4 GR6 (BY REFERENCE assumed when the arg qualifies and the formal is REFERENCE; else CONTENT) → emit `ref`/copy/value accordingly; RETURNING → assign the C# return into the receiving item via the normal MOVE/Store path.

## Edge cases

- INVOKE on a null object reference → EC-OO-NULL (§14.9.23.4 GR5, specs/ISO_COBOL.md:28506), raised through the §14.6.13 EC engine. CORRECTED (Spec corrections #3): INVOKE has NO ON EXCEPTION phrase — no inline handler exists or may be designed; USE declaratives / fatal-EC semantics apply.
- Unknown method / method not found at the receiver's class hierarchy → EC-OO-METHOD (§14.9.23.4 GR7b). For the static typed path this is a COMPILE-time COBOL diagnostic raised by the BINDER (method-name lookup over the pass-1 class symbol table; backend-neutral per G4 — a Roslyn CS error here would be an emitter bug); for the dynamic __CobolInvoke path it is the switch default → raise EC-OO-METHOD.
- FACTORY data is one copy PER CLASS (§8.6.4 :8765 — including a class that merely INHERITS it: DOG-without-a-factory-paragraph still owns its OWN copy of ANIMAL's factory data — the anti-static fact, proven by oo_factory's FC=02/FC=01); factory data lives as INSTANCE fields of the per-class factory singleton, factory methods as its virtual members; `INVOKE Class "M"` (non-NEW) → `CLS__FACTORY.__Instance.M(…)` (landed DEVLOG 604 — brief D11 supersedes the static-call sketch).
- Method WORKING-STORAGE (pre-2023 editions ONLY — ILLEGAL in 2023 per §13.5.3 SR 1, Spec corrections #1) persists across activations and is shared across instances → STATIC fields (NOT per-instance) — counterintuitive; the naive instance-field mapping silently miscompiles a method-WS counter. At `--std 2023` it is a versioned diagnostic instead.
- Method-local data-name identical to an object data-name → the method-local declaration wins inside the method; the object data is inaccessible there (§11.7 GR5). Emit the local/static method field, shadow the instance field.
- RETURNING on a method declared void, or a void INVOKE that omits RETURNING when the method has one → compile-time COBOL diagnostic from the binder's signature check against the pass-1 symbol table (G4: the binder owns conformance diagnostics for both backends; Roslyn merely re-enforces in the rendered C#).
- ~~NEW on an ABSTRACT class~~ — DROPPED (Spec corrections #4): ABSTRACT is not ISO; no ISO construct produces an abstract class, so this case cannot arise on the ISO surface (revisit only if a vendor-dialect ABSTRACT extension is ever added).
- OVERRIDE without a matching base signature, or overriding a FINAL method → compile-time error (§11.7 SR3; C# `override` of a non-existent/sealed base is a compile error — reject at bind for a COBOL-worded diagnostic).
- Object data that is a GROUP or fixed OCCURS table → per-instance record struct / array, byte-identical mapping to PROGRAM data except instance-vs-static (proven by oo_object_group: R1 filled, untouched R2 keeps defaults).
- SELF in a FACTORY method resolves on the factory interface; SELF in an instance method on the instance interface (§14.9.23.3 SR4f/g) — pick `this` consistently; SUPER must name object-class-name when INHERITS lists 2+ bases (§8.4.3.8.3 SR5) — moot under the v1 single-inheritance restriction.
- Universal object reference (`OBJECT REFERENCE` with no class) → C# `object?`; INVOKE through it forbids BY CONTENT/BY VALUE (§14.9.23.3 SR6) and uses the __CobolInvoke dynamic path; `identifier-2` (method-name in a data item) is allowed only for a universal reference (§14.9.23.3 SR7).
- `SET o TO NULL` → `o=null`; `IF o = NULL`/`o = q` → reference comparison `o is null`/`ReferenceEquals`; `o IS Class`/`o IS NOT Class` → C# `o is Class`.
- INVOKE arg is a literal or BY CONTENT → synthesize a copy (a temp local) so the method cannot mutate the caller's literal/source — mirrors the §14.9.23.4 GR6 content semantics.
- Same class inherited indirectly twice (diamond, §11.3.2 GR4) → one copy of its data; under single-inheritance v1 this cannot arise via direct INHERITS, but note it for the eventual multiple-inheritance design.
- Method-name AS literal-1 (externalized name, §11.7.4 GR1b) and CALL-CONVENTION/ENTRY-CONVENTION naming → deferred; v1 uses the COBOL method-name directly as the C# method name (sanitized).

## Legacy port map (regenerated brief, 2026-07-03)

> The legacy OO turnkey doc `docs/OO_IMPLEMENTATION_DESIGN.md` (and ADR §7's DATA_MODEL_ARCHITECTURE.md) was
> DELETED in the DEVLOG-523 purge; DEVLOG 438–456 + the legacy code (`src/CobolSharp.Compiler`) are the only
> remaining record. This section IS the regenerated record: the guard-green slice order legacy actually landed,
> what each slice's portable algorithm/decision is, and — explicitly — what is NOT portable (the byte
> substrate). Port ALGORITHMS and slice sequencing; never the ManagedPointer/ProgramState mechanics.

### Slice 1 (DEVLOG 447) — CLASS-ID + one METHOD-ID + NEW + no-arg INVOKE + USAGE OBJECT REFERENCE — ✅ LANDED (subsumed by spine part 2, DEVLOG 601; multi-method scoping from the 455 slice landed with it via per-method scopes)
- **Portable — class-unit routing + group checks.** Compilation collects classDefinition contexts alongside program units; a case-insensitive `classNames` set is built up front and fed into semantic-model construction so class names are whitelisted in reference resolution (no undefined-name diagnostic on an INVOKE class target); after bind the module is tagged IsClass / ClassMethodName / BaseClassName (`CobolSharp.Compiler/Compilation.cs:60,125,140-194,531-548,749-767`). Substrate-free; ports directly (greenfield home: the pass-1 class symbol table after CallCollectUnits — see Greenfield seams).
- **Portable — the object-reference data model.** Usage alt → an Object usage kind + declared class on the symbol (`Semantics/SemanticBuilder.cs:1278,1596`); PIC-less legal → ObjectReference category (`PicUsageResolver.cs:58-60`); occupies NO storage (`StorageLayoutComputer.cs:230-233`); .NET default null IS COBOL initial NULL (no init emitted); usage-mapper default hardened to Unknown so a stray usage keyword can't masquerade as a zero-storage object ref (DEVLOG 447).
- **NOT portable.** The `static <class> _OBJ_<name>` field registry (`CodeGen/Binder.cs:555-572`) — static fields are correct only for driver-program refs and can't live inside a class instance. Greenfield: an object-ref item is just a C# field of the class type (instance field when it lives in OBJECT data) — the limitation dissolves.

### Slice 2 (DEVLOG 448/449) — INVOKE USING/RETURNING + the COBOL0111 arg hardening
- **Portable — INVOKE discrimination + declared-class dispatch.** `CallBinder.BindInvoke` (`Semantics/Bound/Binding/CallBinder.cs:30-101`): SELF/SUPER detected from the objectReference alternatives (receiver = `this`); "NEW" recognized by method name on a class target; instance call resolves the receiver symbol and dispatches on its DECLARED class (`DataSymbol.cs:102`). (A real FACTORY design supersedes the NEW-by-name shortcut.)
- **Portable — the "never silently drop an argument" rule.** A dropped/unbound USING arg SHIFTS the trailing RETURNING slot (the DEVLOG-449 blocker: first USING param bound to the RETURNING pointer, runtime crash, no diagnostic). Legacy rejected literal / BY VALUE / BY CONTENT args loudly (COBOL0111); greenfield must bind them COMPLETELY (D6) — either way, arity/mode mismatch is a loud compile error, never a silent shift.
- **NOT portable.** The whole `ManagedPointer[]` positional ABI, `public virtual void M(ManagedPointer[] args)` signatures, the STATIC `_linkage_<name>` fields (non-reentrant; sibling-method LINKAGE cross-wiring forced the COBOL0117 reject — `CilEmitter.cs:552-591`, DEVLOG 455). Greenfield: real typed C# parameters + a real return value (D6); keep only the ORDERING contract (USING params in declaration order, RETURNING as the distinct out-slot) + the loud diagnostics.

### Slice 3a (DEVLOG 450) — INHERITS FROM + virtual dispatch/override — ✅ LANDED (greenfield slice 3a, DEVLOG 603; the Cecil depth-sort is obsolete — Roslyn resolves declaration order; override-under-exact-name is subsumed by the uppercase CsName convention)
- **Portable — emission ordering.** Classes before programs, base before subclass: sort IsClass first, then InheritanceDepth ascending (cycle-safe walk over BaseClassName with a seen-set), programs last, STABLE so the first program keeps the entry point (`CilEmitter.cs:101-145`). For Roslyn this is "generate all class declarations into one compilation so base types + INVOKE targets resolve"; the depth-sort stays reusable for any per-class ordering need.
- **Portable — override under the base's EXACT name.** COBOL method names are case-insensitive; .NET member matching is case-sensitive — an override whose spelling differs from the base ("speak" vs "SPEAK") silently took a NEW slot and dispatched to the base (DEVLOG 450 miscompile). Rule: detect overrides case-insensitively across the base chain and emit `override` under the base's exact spelling.
- **Portable — group diagnostics.** Unknown INHERITS base → loud (never degrade to a root class); subclass-own OBJECT data unsupported → loud, checked against the OBJECT paragraph's WS PARSE SUBTREE, not the flattened symbol list — methods' LINKAGE OCCURS INDEXED index-names are force-placed into WS (ISO §8.5.1.2) and false-fired the symbol-list check (`Compilation.cs:749-767`, DEVLOG 450).

### Slice 3b (DEVLOG 451) — INVOKE SUPER (non-virtual) + SELF dispatch semantics — ✅ LANDED (greenfield slice 3b, DEVLOG 603: `this.M()` virtual / `base.M()` non-virtual per D5; SUPER-in-root → 0827; generated members are `__`-namespaced so the legacy name-collision wart cannot recur)
- **Portable — the three dispatch rules** (`CilEmitter.cs:1670-1760`): SELF → receiver `this`, resolved on the CURRENT class + chain, called VIRTUALLY (a subclass override wins even when the SELF call sits in an inherited base method — oo_self_polymorphic); SUPER → receiver `this`, resolved starting at the BASE class, called NON-virtually so override-calls-base cannot recurse; plain instance → receiver's declared class, virtual (a base-typed ref holding a subclass dispatches to the override — oo_inherit). C# mapping: virtual call / `base.M(...)` / virtual call — D5 already encodes this.
- **Portable — SUPER in a root class = clean compile diagnostic** (legacy COBOL0115, detected by scanning the module for the IsSuper invoke; `Compilation.cs:760-761`), never an internal error.
- **Legacy wart to NOT port:** by-name-only method resolution could collide with synthesized helper names (Dispatch/InitializeState — DEVLOG 449); greenfield must namespace/escape generated members.

### Slices "typed-native flip" (DEVLOG 453/454/456) + multi-method (455) — instance state + method scoping
- **Portable — the instance-state decisions:** per-instance state lives on the ROOT class only; a subclass adds only its own NEW fields (once subclass data is supported) and its parameterless ctor (= the COBOL NEW factory, D4) just chains base, which allocates+initializes exactly once; the TWO-OBJECT INDEPENDENCE test is the canonical proof (a static/shared field silently passes every single-object test — DEVLOG 453's #1 risk). In Cobol.Net this collapses naturally: OBJECT data = instance fields (record-struct members / natives) per the existing data mapping.
- **NOT portable:** the byte `State` field, `EmissionContext.StateIsInstance` chokepoint flag, `EmitTypedFieldOwner` ldarg.0 plumbing, ctor-time InitializeState — all ProgramState mechanics (`CilEmitter.cs:874-1110`, `Emission/CilDataEmitter.cs:33-46`). By DEVLOG 456 the byte State was already EMPTY for a fully-typed class — the greenfield starts there.
- **Portable — the per-method paragraph-scope algorithm (ports VERBATIM onto the greenfield PC-dispatcher):** (1) each METHOD-ID gets its own child Scope; its paragraphs declare METHOD-LOCALLY (siblings may reuse paragraph names; PERFORM/GO TO resolve method-first, then program-wide) — and EVERY paragraph-resolution pass must be equally method-aware or the feature re-breaks (legacy needed it in three places: `SemanticBuilder.cs:1762-1810`, `ReferenceResolver.cs:41-51`, `ProcedureNameResolver.cs:111-193`); (2) the binder builds a method roster (Name, EntryParagraphIndex, LastParagraphIndex, USING param names in ABI order) by grouping the paragraph dispatch order on the paragraph's declaring scope (`CodeGen/Binder.cs:985-1005`); (3) each public method runs ONLY its contiguous range `Dispatch(entry, last)` with the exit bound = its LAST paragraph (NOT −1/end-of-program), so falling off the end is the implicit GOBACK instead of running into the sibling method's paragraphs (`CilEmitter.cs:370-391`). Guard with the fall-through test (`OoTests.Invoke_MultiMethod_FirstMethodDoesNotFallIntoSecond`). SECTIONs inside methods SILENTLY truncated the range to [first..first] — legacy rejected loudly (COBOL0116); greenfield must reject or implement deliberately.
  **AS BUILT (spine part 2, DEVLOG 601 — two deliberate deltas):** (a) resolution inside a method is CONFINED to the method's own scope with NO program-wide fallback (`StatementBinder.OoMethodScope` + the ResolveProcedure method branch) — the legacy's "then program-wide" leg would have made a cross-method PERFORM resolve instead of failing (trap #10); the greenfield has exactly ONE resolution pass (ResolveProcedure), so the three-places re-break hazard is structural-impossible. (b) SECTIONs inside methods are IMPLEMENTED deliberately (a section registers in the method scope; its paragraphs are contiguous within the method range) — COBOL0116's reject is superseded, and the day-one guard is `OoSpineTests.Trap4_MethodFallThrough_DoesNotEnterSiblingMethod` + `Trap10_CrossMethodPerform_FailsLoud`.
- **Portable — two-phase method emission:** declare all method signatures BEFORE any body so (a) INVOKE SELF resolves a sibling and (b) the public COBOL method names are reserved first, with paragraph-helper names uniquified AROUND them; keep the dispatch map keyed by method identity, not name (`CilEmitter.cs:335-362`, DEVLOG 455).

### Never landed in legacy (net-new design here, NOT a port)
FACTORY (slice 4 — §11.4, D7); PROPERTY (slice 5 — §13.18.42); EC-OO (slice 6 tail — Table 13); ~~per-method LINKAGE with multi-method params (legacy COBOL0117 reject)~~ — **LANDED net-new in slice 2 (DEVLOG 602: per-method data scopes + typed `ref` params; the trap-#6 cross-wiring test is `OoSpineTests.Trap6_SiblingMethodLinkage_NoCrossWiring`)**; ~~method-scoped SECTIONs (COBOL0116)~~ — **landed with the spine (method-scoped ranges)**; ~~INVOKE literal / BY CONTENT arguments (COBOL0111)~~ — **landed in slice 2 (bound fully per D6; BY VALUE stages 0828 pending the header BY-phrases)**; subclass-own OBJECT data; data-name method selector (universal-ref-only per SR 7); SET on object references; object references held BY a class instance (an object-ref item in OBJECT data works today — oo_hello-shaped drivers hold refs in program WS; a CLASS holding refs to OTHER classes rides the same PicInfo machinery). These follow the banner order: spine → slices 1–6 → FACTORY → PROPERTY → INTERFACE-ID → universal reference → EC-OO. (Per §A.4.10, specs/ISO_COBOL.md:40408-40414, only THREE OO items are optional — multiple-inheritance repetition ×2 and parametric polymorphism; everything above is MANDATORY for a conforming 2023 implementation.)

### Adversarial regression traps (each was a REAL caught bug — reproduce as day-one greenfield tests)
1. Two-object independence (shared-static corruption; oo_instance_data pattern b1/b2/b1).
2. Case-mismatched override still dispatches (emit under the base's exact name).
3. Dropped/reordered INVOKE arg shifting the RETURNING slot (bind literal/BY VALUE/BY CONTENT completely or reject loudly).
4. Method fall-through into the next method's paragraphs (exit-bounded range).
5. SECTION inside a method truncating the dispatch range.
6. Sibling-method LINKAGE cross-wiring.
7. SUPER in a root class → clean diagnostic, not an internal error.
8. INHERITS unknown base → loud, never silently a root class.
9. Subclass-own-data detection must not false-fire on synthesized index-names placed in WS (ISO §8.5.1.2).
10. Three-level SUPER chains; state allocated exactly once across ctor chaining; PERFORM/GO TO method-local resolution incl. backward GO TO; cross-method PERFORM rejected.

**Reusable test assets:** `tests/conformance/2002/oo_hello|oo_instance_data|oo_method_perform|oo_method_args|oo_inherit|oo_super|oo_self|oo_self_polymorphic|oo_object_group` (.cob + .out goldens — 9 pairs). AS OF SLICE 2 (DEVLOG 602) **5 of the 9 are ENABLED in the greenfield 2002 manifest run contract** (oo_hello, oo_instance_data [= trap #1 two-object independence], oo_method_perform, oo_object_group, oo_method_args [USING/RETURNING] — compiled strict + run + byte-compared by CorpusRunnerTests); the remaining 4 stay pending on their slices (oo_inherit/oo_super → 3a; oo_self/oo_self_polymorphic → 3b). The greenfield day-one adversarial suite is `tests/Cobol.Net.Tests.Conformance/OoSpineTests.cs` (traps #4/#10, the D8 GOBACK/STOP RUN/EXIT METHOD split, the 0813/0820–0827 diagnostic band). The legacy `OoTests` suite (`tests/CobolSharp.Tests.Integration/OoTests.cs` — 13 tests) remains the port checklist for the later slices.

## Greenfield seams (what the port plugs into — verified 2026-07-03)

### Grammar seam — the OO surface already parses
The LIVE fragment is `src/Cobol.Net.Frontend/Grammar/Core/CobolOO.g4:18-98`: classDefinition (CLASS-ID, single INHERITS FROM), objectParagraph, methodDefinition (full env/data/procedure divisions), invokeStatement (USING BY VALUE/REFERENCE/CONTENT/bare/literal + RETURNING, NO exception phrase — already ISO-correct per Spec corrections #3), and objectReferenceUsage as TWO explicit alternatives (the DEVLOG-438/439 lesson: never an optional `className?` tail — it regressed '85 `IS [NOT] NUMERIC`). `CobolParserOO.g4` is a DEAD unbuilt sketch (regen inputs are only `Grammar/CobolParserCore.g4;Grammar/Core/*.g4`, `Cobol.Net.Frontend.csproj:44`) — its FACTORY/attributes/generics/invokeOnException content is reference-only, and invokeOnException is spec-WRONG. Gate inventory (nine `{is2002()}?` hooks): `CobolParserCore.g4:105` (classDefinition in compilationGroup), `:412` (repository CLASS entry), `:442` (PD returningClause/raisingClause), `:626-627` (ALLOCATE/FREE), `:663` (invokeStatement), `:904` (BY VALUE arg), `:1004` (SET … TO objectReference), `:1061` (GOBACK RETURNING/GIVING), `Core/CobolData.g4:312` (objectReferenceUsage). Missing (updated DEVLOG 606): method-name AS literal and class OF SUPER only — added incrementally per the Version-gating rules (FACTORY landed DEVLOG 604; OVERRIDE/[IS] FINAL DEVLOG 605; INTERFACE-ID/IMPLEMENTS/PROPERTY DEVLOG 606). Edition-gates to ADD per the Spec corrections: method-WS rejection at 2023 (#1) and `EXIT METHOD` removal at 2023 (#2, `Core/CobolControlFlow.g4:213`).

### Binder seam — LIVE as of spine part 2 (DEVLOG 601; the silent-drop hazard is FIXED)
AS BUILT: `CallCollectUnits` collects `classDefinition` units and builds the pass-1 `OoClassTable`
(`src/Cobol.Net.Compiler/Binding/OoClassTable.cs` — class → base link → method roster with HasUsing/HasReturning;
structural diagnostics 0820 duplicate-class/END-mismatch, 0821 unknown-INHERITS-base [trap #8], 0822
duplicate-method [D9]) BEFORE any unit binds, so every DataBinder (typed `USAGE OBJECT REFERENCE class` —
unknown class 0813) and StatementBinder (`OoClasses` property) resolves classes defined anywhere in the group.
INVOKE binds via `OoBindInvoke` (`StatementBinder.Oo.cs`): identifier-1-shadows-class-name resolution, literal
selector, `BoundInvoke(Form, …)` for NEW (RETURNING required + §14.8 receiver conformance — 0826) and the
no-arg instance call (unknown method 0825 — the compile-time GR7b analog); SELF/SUPER (3b), factory calls,
USING/RETURNING marshaling (slice 2), universal/dynamic dispatch (D10) stage LOUD as BoundUnsupported. Method
bodies bind through `BindClassBody` — per-method `OoMethodScope`s confine paragraph/section resolution
(§11.7; a cross-method PERFORM/GO TO fails loud with the method-scope hint — trap #10 structural); method
GOBACK → `BoundMethodReturn` (D8), EXIT METHOD → same in a method / 0827 outside, EXIT PROGRAM in a method →
0827 (§14.9.14.3 SR7). Still staged from the pre-part-2 list: SET obj-ref → BoundUnsupported
(StatementBinder.cs:668-669); RAISE identifier / EXIT PROGRAM RAISING identifier await the EC-OO slice
(`StatementBinder.Exceptions.cs:53-54`).

### Emitter seam — the CallUnit machinery IS the ClassUnit template (AS BUILT in spine part 2, DEVLOG 601)
AS BUILT: `CSharpEmitter.Oo.cs` — `OoClassUnit` (the ClassUnit counterpart of CallUnit), the two-phase bind
(`OoBindClassData` for EVERY class — the synthetic-unit DataBinder + `OoBindMethodData` per method between
`BindDeclarations`/`BindResolve`, so signatures exist before ANY body binds regardless of source order —
then `OoBindClassBody`; class FILE SECTION staged 0899), and `OoEmitClassUnit` — the SAME per-unit
emitter-state switch as `CallEmitProgramClass` renders `public class FOO : CobolObject` with FieldEmitter
INSTANCE fields (VALUE → field initializers; the implicit public ctor IS the predefined NEW — D4) and
method-WS STATIC fields (D3).
**Dispatcher realization (slice 2 supersedes the part-2 shared-instance-`__Dispatch` shape — deliberately):**
each METHOD-ID emits as `public virtual <ret> M(ref …)` whose paragraphs render inside a LOCAL FUNCTION
`__MDispatch` over the method's contiguous slice of the class's ONE pc space (`EmitDispatchMethod(header,
from, to)`; `_dispatchName` threads the PERFORM/SORT emit sites). The local function is what lets paragraph
code address the method's LINKAGE/LOCAL-STORAGE C# LOCALS by capture (a `ref` parameter is NOT capturable —
hence the param→local copy-in/copy-out at the method boundary); captures lower to by-ref frames (zero
allocation), and recursion (methods are implicitly RECURSIVE, :12032) is reentrant because every activation
owns fresh locals. Falling past the slice's last paragraph returns (trap #4); `__N` stays one class-level
const. INVOKE renders in `OoEmitInvoke`: NEW → `place.Write("new FOO()")`; instance → the D6 marshaling
around `CobolObject.RequireNonNull(recv).M(…)` (GR5; see the D6 AS-BUILT note). Classes emit before programs
(source order; Roslyn needs no depth sort — the legacy Cecil ordering is obsolete); a class-only compilation
unit emits classes + an empty Main. The original seam notes follow:
The interprogram wave already emits N instantiable classes per compilation: `CallEmitRunUnit` (`CSharpEmitter.Call.cs:79-144`) binds every unit (per-unit DataBinder, disjoint uid bands, `CallBindUnit` :234-308) and emits one `internal sealed class _PRG_<NAME> : ICobolProgram` per program into ONE .g.cs (`CallEmitProgramClass` :340-438) — each with INSTANCE fields, a ctor taking the container, instance `__Activate()` + `__Dispatch(int,int)` PC-loop methods (`CSharpEmitter.cs:88-153`), and `private ref T fld => ref __outer.fld` bridge properties. The CallUnit model (:30-48) is the natural ClassUnit shape; the per-unit emitter-state switch (:343-347) is exactly the "emit fields+paragraphs+statements into a type parameterized by (type, instance-vs-static, storage-source)" refactor this doc's Summary calls for. Port notes: the pass-1 class symbol table (D1) is built during/after CallCollectUnits, before per-unit binding, so a driver INVOKE sees later classes; methodDefinition contexts (which own their own data/procedure divisions) need the `CallReparent`-style synthetic reshaping (:218-228 — direct-children-only accessor discipline, the IC235A lesson); `CallBindUnit`'s LINKAGE-formal resolution before field emission is where method LINKAGE→parameters slots in; the EC group gate `_ecActive` spans all units (:95) and CALL sites emit EC propagation pickups (:676-689) — INVOKE sites need the analogous decision (per Spec corrections #3 they surface EC-OO conditions, so they participate in the same propagation).

### Registry/GOBACK seam — OO bypasses the registry; the two-signal architecture is already in place
`ProgramRegistry` (`src/Cobol.Net.Runtime/Control/ProgramRegistry.cs:7-120`) owns §8.4.6.3 program-name resolution, §14.6.2.3 instance-state, CANCEL, ExternalStore, and cross-assembly discovery via `__CobolModule.Register()` (`CSharpEmitter.Call.cs:546-584`) — all on the opaque `ICobolProgram.Call(CobolArg[], ManagedPointer?)` ABI, which D6 explicitly REJECTS for methods. So OO INVOKE bypasses the registry entirely (direct typed C# calls); classes need only type emission alongside program classes — registry involvement returns only if dynamic-name class lookup is ever wanted. Signals: STOP RUN → `BoundStop` → `throw new StopRun()` unwinds to Main; GOBACK → `BoundGoback` → `CallEmitGoback` (`CSharpEmitter.Call.cs:803-816`: RETURNING move, RAISING staging, `throw new ProgramReturn()`) caught at the program's own `__Activate` (`CSharpEmitter.cs:120`); EXIT PROGRAM is `__asCalled`-gated (:822-835). The OO spine adds the third context: method GOBACK → `BoundMethodReturn` → C# `return` (D8), with EXIT METHOD edition-gated per Spec corrections #2.

## ISO citations

- ISO/IEC 1989:2023 §11.3 CLASS-ID paragraph (§11.3.2 general format: `INHERITS FROM {object-class-name-2}…` — multiple inheritance; AS literal; IS FINAL; USING parameterized class)
- §11.4 FACTORY paragraph + §8765 (one copy of each static/factory item per class) — FACTORY → static members
- §11.7 METHOD-ID paragraph (SR3 OVERRIDE needs matching non-FINAL base signature; SR4a redefining base signature w/o OVERRIDE is an error → no C# hiding; GR5 method-local shadows object data; SR6/7 GET/SET PROPERTY shape; SR2/8 OVERRIDE/FINAL not in prototypes)
- §11.8 OBJECT paragraph (instance object definition; IMPLEMENTS interface-name list)
- §8.4.3.8 SELF and SUPER (GR2 SELF resolves on the runtime class → virtual; GR3 SUPER restricted search → base.; SR4/5 object-class-name OF SUPER when 2+ bases)
- §9.3.6 Method invocation (object→instance vs factory resolution; SUPER restricted search; match-rule 3c BY REFERENCE same class/category → typed `ref` is conformant)
- §14.9.23 INVOKE statement (general format USING BY REFERENCE/CONTENT/VALUE/OMITTED + RETURNING; SR4 method-name lookup by receiver kind; SR6 universal ref forbids BY CONTENT/VALUE; SR7 identifier-2 method-name only for universal; GR5 null→EC-OO-NULL; GR6 default arg-passing; GR7b not-found→EC-OO-METHOD; GR8 RETURNING)
- §12063 (method resolution signature + parametric polymorphism is OPTIONAL)
- §16.2.1 predefined NEW factory method (no explicit FACTORY needed to construct)
- §13.18.60.4 USAGE OBJECT REFERENCE (typed vs universal object references)
- §14.9.23.4 GR2b non-COBOL INVOKE is implementor-defined (basis for deferring .NET-object interop INVOKE)

### Verified 2023 line anchors (specs/ISO_COBOL.md, re-checked 2026-07-03 for the regenerated brief)

- Structure §10.6: class-definition :12739-12750 (CLASS-ID modifiers :12742-12744 — AS/FINAL/INHERITS/USING ONLY); factory-definition :12752-12760; instance-definition :12762-12770 (both take IMPLEMENTS, :12755/:12765); interface-definition :12783-12796 (NO data division; methods are prototypes, :12823); method-definition :12798-12821 (attrs = [OVERRIDE] [IS FINAL] only; GET/SET PROPERTY selector :12810-12814); end markers §10.7 :12883.
- §11 paragraph map: §11.3 CLASS-ID :12999; §11.4 FACTORY :13069; §11.6 INTERFACE-ID :13157; §11.7 METHOD-ID :13210; §11.8 OBJECT :13295. Every method is implicitly RECURSIVE :12032.
- Spec corrections: method WS ban §13.5.3 SR 1 :16461 (+ INVOKE SR 10 :28443); EXIT formats §14.9.14.2 :27346-27381 + Annex E.2 removals :49034/:49036; INVOKE format §14.9.23.2 :28376-28390 (no exception phrase), EC-OO-NULL :28506, EC-OO-METHOD :28528; ABSTRACT — zero matches in the whole spec.
- PROPERTY clause §13.18.42 :21136-21170 (WS of factory/instance only, SR 1 :21153; elementary, no OCCURS, no ACTIVE-CLASS refs :21165; generated GET/SET :21170; property temps via implicit INVOKE :7393-7395).
- Universal object reference: §13.18.60 USAGE :22636, GR 22b :22912, glossary :2352; INVOKE via universal forbids BY CONTENT/VALUE (SR 6 :28435); identifier-2 method-name ONLY with universal (SR 7 :28437); runtime (not compile-time) conformance :12291, EC-OO-UNIVERSAL :24756; EXCEPTION-OBJECT is implicitly universal :7249; NULL/SELF/SUPER are NOT :7283/:7331.
- EC-OO family: Table 13 under §14.6.13.1.6 :24748-24756 — EC-OO-ARG-OMITTED / -CONFORMANCE / -EXCEPTION / -IMP / -METHOD / -NULL / -RESOURCE / -UNIVERSAL (all Fatal except EC-OO-IMP); object-view raise sites :7211-7229.
- RAISE §14.9.29 :29727-29761 (RAISE identifier-1 = exception object; NULL/SUPER forbidden, SELF legal; unhandled-at-exit → EC-OO-EXCEPTION per §14.6.13.1.5 :24575,:24602-24608) — the landed EC engine gets an object channel, not a new mechanism.
- Conformance is TWO distinct rule sets: §14.8 (:25245, params :25270, returning :25419 — argument↔formal at INVOKE sites, cited by INVOKE SR 5c :28433; ANY LENGTH pairing :25375/:25377/:25414/:25503-25505) vs §9.3.6 (:12105) + §9.3.8.2 (:12294) / §9.3.8.2.3 interfaces (:12309) — method-SIGNATURE conformance for override/implements (:12177,:12247); inline-invocation RETURNING may not be ANY LENGTH/ACTIVE-CLASS :7151; ANY LENGTH clause §13.18.2 :17576 (SR 2 :17593).
- §A.4.10 :40408-40414 — EXACTLY three optional OO items (CLASS-ID INHERITS repetition, INTERFACE-ID INHERITS repetition, parametric polymorphism §9.3.5.3 :12102); ALL other OO surface (FACTORY/OBJECT, interfaces, PROPERTY, universal refs, object views, SELF/SUPER/NULL/EXCEPTION-OBJECT, INVOKE + inline invocation, ACTIVE-CLASS, single-INHERITS, RAISE with objects, EC-OO) is MANDATORY for a conforming 2023 implementation. Single INHERITS is mandatory; only the repetition is optional.

## Open questions (multiple inheritance settled in `COBOLNET_DESIGN.md` §18 item 18; the rest are live here)

- OWNER-LEVEL: COBOL allows MULTIPLE class inheritance (§11.3.2 `INHERITS FROM {object-class-name-2}…`); C# allows only one base class. v1 restricts to single inheritance (sufficient for the entire current corpus). When a multi-base program appears, choose: (a) linearize to one C# base + extract secondary supers as C# interfaces the class IMPLEMENTS (with member forwarding), or (b) declare multiple inheritance unsupported. Needs an owner decision before any multi-base program is targeted.
- Parametric polymorphism (overloading by method-resolution-signature, §12063) is OPTIONAL and currently deferred (no corpus use). If targeted, decide between C# name-mangling by signature vs leaning on the processor's object-management-system methodology the spec permits — an owner/architecture call about generated-name idiomaticity.
- Grammar extension scope/ordering — MOSTLY RETIRED (DEVLOG 604/605/606): FACTORY, OVERRIDE/[IS] FINAL, INTERFACE-ID/IMPLEMENTS, and PROPERTY (clause + GET/SET selector) are ALL LANDED behind `{is2002()}?` gates with paired pre-2002 diagnostics and matrix rows. Still missing: method-name `AS literal` and qualified `object-class-name OF SUPER` (per §A.4.10 :40408-40414 both MANDATORY for a conforming 2023 implementation — sequencing, never optional scope).
- INTERFACE-ID + IMPLEMENTS → C# interfaces — RESOLVED (DEVLOG 606): an explicit BINDER conformance pass is REQUIRED, not optional; Roslyn satisfaction is provably insufficient in both directions (lossy projections under-reject: 9(4)/9(8) both `ref long`; covariant returns over-reject: rules 5a/5c2 permit what C# interface implementations forbid — cured by explicit-implementation adapters). See the INTERFACE/PROPERTY AS-BUILT.
- EC-OO-* exception catalog integration (the full Table-13 family, :24748-24756) rides the LANDED EC subsystem (DEVLOG 577: >>TURN, USE declaratives, RAISE/RESUME, EXCEPTION-* functions); v1 raises EC-OO-NULL/METHOD through that engine. CORRECTED (Spec corrections #3): there is NO inline ON EXCEPTION on INVOKE — the earlier "honors inline ON EXCEPTION" plan is void; handling is declaratives/fatal-EC semantics only. Remaining sequencing: the RAISE identifier-1 exception-object channel (§14.9.29 :29727) + EXCEPTION-OBJECT (implicitly universal, :7249) + unhandled-object→EC-OO-EXCEPTION conversion (:24602-24608) land with the EC-OO slice.
