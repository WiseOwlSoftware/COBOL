# COBOL.NET — OO COBOL -> .NET classes (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §10; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

OO COBOL → idiomatic .NET classes, designed fresh for the C#-native (Roslyn) target. The byte-model legacy kept its whole data engine and only swapped `ldsfld State`→`ldfld State`; the C#-native target instead emits ONE real C# class per CLASS-ID (the driver PROGRAM stays the static `Program` class), instance fields per OBJECT data item, real C# methods per METHOD-ID, and INVOKE → real C# `new`/`obj.M(...)`/`base.M(...)`. This is a major simplification: the one thing the byte model needed Mono.Cecil two-pass resolution for (cross-type method/type binding, virtual dispatch, parameter-conformance checks) Roslyn now gives for free — the emitter's "two-pass" is reduced to building its OWN small symbol table (class → method-name → {param modes, return}) so INVOKE can marshal args (ref/value/copy) and pick the call form; pass-2 emits bodies and lets the C# compiler resolve everything else.

TYPE-EMISSION MODEL: every CLASS-ID Foo → `public class Foo : CobolObject` (or `: Base`); a `CobolObject` runtime base hosts universal/dynamic dispatch + NULL/IS semantics reflection-free (rejected: derive from System.Object → universal dispatch then needs reflection). The current emitter is 100%-static/single-`Program`; refactor "emit fields+paragraphs+statements into a type" into a routine parameterized by (type, instance-vs-static, storage-source) so Program emits static and a class emits instance members on the SAME statement/PC-dispatch machinery. STORAGE SCOPES (two counterintuitive): OBJECT-para WORKING-STORAGE → INSTANCE fields; METHOD WORKING-STORAGE → STATIC fields (ISO §11.7 GR5/§11.8: method WS persists across activations, shared across instances — NOT per-instance); METHOD LOCAL-STORAGE → C# locals (re-init each call); LINKAGE → method parameters; method-local names SHADOW object data (§11.7 GR5). The predefined NEW = the ctor: chain base ctor first (C# default, matches COBOL base-then-derived init), then VALUE-init this class's instance fields. FACTORY → static members/methods on Foo; FACTORY data → static fields.

INVOKE RESOLUTION (each grounded in a conformance .cob): `Class "NEW" RETURNING o`→`o=new Class()`; `obj "M" USING.. RETURNING..`→`obj.M(args)` virtual; `SELF "M"`→`this.M()` (virtual, §8.4.3.8 GR2 — runtime-class dispatch); `SUPER "M"`→`base.M()`; `Class "M"`(non-NEW)→static call. Dynamic/universal (method-name is a data item, or receiver is universal `object?`) cannot be a static C# call → a virtual `object? __CobolInvoke(string name, object?[] args)` on CobolObject whose body is a switch over the class's methods (reflection-free, AOT/WASM-safe); typed-literal INVOKE stays the fast path. INVOKE on a real .NET object (interop) is deferred — §14.9.23.4 GR2b makes non-COBOL INVOKE implementor-defined.

METHOD ATTRS map cleanly because COBOL forbids implicit hiding: instance methods → `virtual` by default (§9.3.6 runtime-class dispatch); OVERRIDE→`override`; FINAL→`sealed override`; ABSTRACT→`abstract`+abstract class; §11.7 SR4a (redefining a base signature without OVERRIDE is an ERROR) means we NEVER emit C# `new`/hiding.

CORRECTNESS BLOCKER (the only one): GOBACK in a method ≠ STOP RUN — the current emitter throws StopRun for BOTH (CSharpEmitter line 204-205). In a method GOBACK returns from the method only; STOP RUN ends the run unit. Define a distinct method-return path (a labeled return / `return` past the PC loop) vs the run-unit StopRun. OWNER QUESTION: COBOL allows MULTIPLE class inheritance (§11.3.2 `INHERITS FROM {name}…`), which `: Base` cannot express in C#.

## Decisions

### D1. One C# type per CLASS-ID; the driver PROGRAM stays the static `Program` class; let Roslyn perform cross-type binding, virtual dispatch, and parameter/return conformance checking.

**Rationale.** The C#-native target's defining advantage: the byte model needed Mono.Cecil two-pass resolution (define all type+method signatures, then bodies) purely to bind cross-type INVOKE and emit callvirt; emitting C# source hands all of that to the C# compiler. The emitter's residual 'two-pass' shrinks to building its OWN symbol table (class→method→{param modes,return type}) needed only to marshal INVOKE args (ref/value/copy) and choose the call form (newobj/callvirt/call). ISO §9.3.6 runtime-class dispatch ≡ C# virtual dispatch exactly.

**Rejected alternatives.** Port the legacy per-instance-ProgramState model (instance `State` byte field + `ldarg.0;ldfld State`) — rejected by the OWNER-LOCKED no-byte-substrate rule and because it throws away Roslyn's free type-checking. Emit via Mono.Cecil like legacy — rejected: re-introduces the manual cross-type MethodDefinition registry the source target eliminates.

### D2. Introduce a `CobolObject` runtime base class; every emitted COBOL class is `class Foo : CobolObject` (or `: Base` when it INHERITS, whose own root is CobolObject).

**Rationale.** Gives a single reflection-free home for universal/dynamic dispatch (`__CobolInvoke`), object-identity/NULL/`IS class` semantics, and a future EC-OO surface — AOT/WASM-safe (no Type.GetType, no reflection).

**Rejected alternatives.** Derive each class straight from System.Object — rejected: universal-object-reference dispatch (`INVOKE U name`) and `obj IS Class` would then require reflection or a marker interface, defeating AOT safety. A marker interface ICobolObject instead of a base class — rejected: a base class can carry the default `__CobolInvoke` body and shared state; an interface cannot supply a default reflection-free dispatcher cleanly.

### D3. Storage-scope mapping: OBJECT-paragraph WORKING-STORAGE → INSTANCE fields; METHOD WORKING-STORAGE → STATIC fields; METHOD LOCAL-STORAGE → C# locals (re-init per call); LINKAGE → method parameters. Method-local names shadow object data.

**Rationale.** ISO §11.8/§11.7: object WS is the per-instance object state; a method's WORKING-STORAGE persists across activations and is SHARED across instances (so it is static, NOT per-instance — the counterintuitive case); LOCAL-STORAGE is re-initialized on each entry (locals); §11.7 GR5 makes a method-local name shadow the same name in object data.

**Rejected alternatives.** Make method WORKING-STORAGE instance fields (the naive 'it's inside the object' reading) — rejected: violates §11.7 persistence/sharing semantics and would silently miscompile a counter kept in method WS. Make LOCAL-STORAGE static — rejected: LOCAL-STORAGE must re-init each activation.

### D4. The predefined NEW factory = the generated public ctor: base ctor chains first, then VALUE-initialize this class's own instance fields.

**Rationale.** C# already runs base-then-derived ctor order, which matches COBOL initialize-inherited-then-own-data; §16.2.1 NEW is the built-in factory needing no explicit FACTORY METHOD-ID.

**Rejected alternatives.** A separate static `NEW()` factory method calling a parameterless ctor + an Initialize method (the legacy InitializeState shape) — rejected: redundant in C#; the ctor IS the initializer and field initializers + ctor body cover VALUE.

### D5. INVOKE call-form table: Class "NEW" RETURNING o → `o=new Class()`; obj "M" → `obj.M(args)` (virtual); SELF "M" → `this.M()`; SUPER "M" → `base.M()`; Class "M" (non-NEW) → static call; dynamic/universal → `recv.__CobolInvoke(name,args)`.

**Rationale.** Direct, idiomatic, statically-resolved (AOT-safe) for the literal-method 90% path; matches §9.3.6 (object→instance/factory resolution, SUPER restricted search), §8.4.3.8 (SELF virtual on runtime class, SUPER non-virtual).

**Rejected alternatives.** Route ALL invokes through a single `__CobolInvoke` string-switch (uniform but slow, unidiomatic, defeats Roslyn overload/type checks) — rejected; reserve __CobolInvoke only for the genuinely-dynamic cases. The legacy uniform `callvirt CobolProgramEntry.Invoke(ManagedPointer[])` ABI — rejected: not idiomatic C#, hides types from Roslyn.

### D6. Parameter passing: BY REFERENCE → C# `ref` of the typed field (value-class items) or the reference itself (object/string); BY CONTENT → pass a copy; BY VALUE → value parameter; RETURNING → C# return value; OMITTED → nullable param + omitted-arg condition.

**Rationale.** §9.3.6 match-rule 3c requires a BY REFERENCE argument and its formal parameter to be the same class/category — so a real typed `ref` parameter is fully conformant; this is precisely why the C#-native target can use typed parameters instead of the legacy ManagedPointer[] byte ABI. RETURNING-as-return is the idiomatic and §14.9.23 GR8-faithful mapping.

**Rejected alternatives.** Keep the ManagedPointer[] args ABI (legacy) for uniformity with CALL — rejected: byte-substrate-adjacent, unidiomatic, and unnecessary now that types are native. Box everything to object[] for all params — rejected: loses Roslyn type checking and adds boxing.

### D7. Method attributes: instance methods are `virtual` by default; OVERRIDE→`override`; FINAL→`sealed override` (FINAL root → non-virtual); ABSTRACT→`abstract` (+abstract class); FACTORY/STATIC→`static`. Never emit C# `new`/method-hiding.

**Rationale.** §9.3.6 dispatch is always on the runtime class → virtual by default. §11.7 SR4a makes redefining a base signature WITHOUT OVERRIDE a compile error, so COBOL never expresses C# hiding — the mapping stays clean and total.

**Rejected alternatives.** Emit non-virtual methods and only mark virtual when overridden somewhere — rejected: requires whole-program analysis to know if a class is ever subclassed and breaks separate compilation; virtual-by-default matches the spec directly.

### D8. GOBACK inside a METHOD returns from the method only; STOP RUN ends the run unit. Emit a method-return path (a `return`/labeled break out of the method's PC-dispatch loop) distinct from the `StopRun` exception.

**Rationale.** §14.9.43/method semantics: GOBACK in a method is a normal method return (control + any RETURNING value go back to the INVOKE site); STOP RUN terminates the whole run unit. The current emitter throws StopRun for BOTH (CSharpEmitter.cs lines 204-205) — correct for a top-level program, WRONG inside a method (it would unwind past the caller). This is the single decision that silently miscompiles if missed.

**Rejected alternatives.** Keep throwing StopRun and catch it at each method boundary — rejected: a caught StopRun loses the RETURNING value and is fragile across nested INVOKEs; an explicit method return is correct and idiomatic. EXIT METHOD also maps to the method-return path.

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

v1 RESTRICTS to single inheritance (every conformance/NIST program uses single `INHERITS FROM` — verified). Reject 2+ bases LOUDLY with a dedicated diagnostic. Escalate as an OWNER-LEVEL open question. The decided escape hatch when it is needed: linearize the inheritance graph to one C# base chain + extract the secondary supers' instance interfaces into C# interfaces the class IMPLEMENTS, copying/forwarding the secondary supers' members — but that is design-deferred until a program demands it.

### GOBACK vs STOP RUN inside a method (the only correctness blocker). The current single-`Program` emitter throws `StopRun` for both; inside a method that would unwind past the INVOKE caller and drop the RETURNING value.

Track an emission context flag 'in method body'. In a method, GOBACK/EXIT METHOD → emit a C# `return` (with the RETURNING value if any) that exits the method's PC-dispatch loop; STOP RUN keeps throwing the run-unit `StopRun`. The PROGRAM (Main) keeps the existing try/catch(StopRun). Verified concretely against CSharpEmitter.cs lines 204-205 which currently conflate them.

### Per-instance vs static field selection must thread through the WHOLE statement/expression emitter (DISPLAY/MOVE/arithmetic/PERFORM read & write data items), not just field declarations — the legacy solved this with one chokepoint (ldarg.0;ldfld vs ldsfld).

Refactor the emitter's 'emit a data item reference' to consult the bound item's owning scope: an instance object item emits `this._FIELD` (or `obj._FIELD` for a qualified receiver — but COBOL OO data is always SELF-relative inside a method), a static/program/method-WS item emits the bare static field. Because the statement emitters all funnel through one ReadAsString/Resolve/field-name path (CSharpEmitter Resolve + CsName), this is a single parameterization, exactly mirroring the legacy chokepoint but in source.

### DataBinder/DataItem/PicInfo currently model ONLY PICTURE items and silently ignore USAGE OBJECT REFERENCE (no PicCategory for it), so object-reference fields and INVOKE targets cannot be typed or resolved.

Add an ObjectReference item kind to the bound model carrying {ClassName | universal-flag}: typed → C# field type `ClassName?`, universal → `object?`. The binder reads `objectReferenceUsage` (Core/CobolOO.g4 — `OBJECT REFERENCE className` vs bare `OBJECT REFERENCE`). INVOKE binding resolves the target item to learn its declared class for choosing instance dispatch; a class-name target (not a data item) means NEW/static-factory dispatch.

### Cross-compilation-unit resolution: the driver PROGRAM and each CLASS are separate program units in one compilation group; INVOKE in the driver references a class defined later in the file, and a method body references sibling methods/inherited members.

A genuine two-PASS over the compilation group: pass-1 walk every classDefinition and build the emitter symbol table (class name, base name, method roster with each method's USING param modes + RETURNING presence/type, FACTORY-vs-OBJECT, attributes); pass-2 emit all class types and the Program into ONE C# source file (one compilation), so the C# compiler resolves all cross-type and inheritance references. The symbol table is used only to marshal INVOKE args and select the call form.

### INVOKE on a NULL object reference must raise EC-OO-NULL (§14.9.23.4 GR5) and route to ON EXCEPTION, not throw a raw NullReferenceException.

For a typed INVOKE emit a guard `recv ?? throw new CobolException(EC.OO_NULL)` (or a small `CobolObject.RequireNonNull(recv)` helper) before the call; wire the ON EXCEPTION / NOT ON EXCEPTION phrases (grammar already has invokeOnException in the dead sketch; the live grammar's invokeStatement lacks it — see edge cases) to a try/catch over the call. Full EC-OO catalog is deferred to the EC subsystem; v1 raises the condition and supports the inline ON EXCEPTION handler.

### Method LINKAGE + PROCEDURE DIVISION USING/RETURNING: the live grammar carries USING on procedureDivision and RETURNING gated {is2002()} — but mapping positional COBOL params (BY REFERENCE default) to typed C# params requires per-method param-mode info at the INVOKE site, across units.

The pass-1 symbol table records each method's ordered formal parameters with mode (REFERENCE/CONTENT/VALUE/OMITTED-capable) + type, derived from its LINKAGE items and the PD USING/RETURNING. At an INVOKE, marshal each argument per §14.9.23.4 GR6 (BY REFERENCE assumed when the arg qualifies and the formal is REFERENCE; else CONTENT) → emit `ref`/copy/value accordingly; RETURNING → assign the C# return into the receiving item via the normal MOVE/Store path.

## Edge cases

- INVOKE on a null object reference → EC-OO-NULL (§14.9.23.4 GR5), routed to ON EXCEPTION if present; v1 raises + supports inline handler, full EC catalog deferred to the EC subsystem.
- Unknown method / method not found at the receiver's class hierarchy → EC-OO-METHOD (§14.9.23.4 GR7b). For the static typed path this is a COMPILE error (Roslyn won't find the method); for the dynamic __CobolInvoke path it is the switch default → raise EC-OO-METHOD.
- FACTORY (static) data is one copy per class (§8765); FACTORY methods are static; `INVOKE Class "M"` (non-NEW) is a static call — distinct from an instance INVOKE through an object reference.
- Method WORKING-STORAGE persists across activations and is shared across instances → STATIC fields (NOT per-instance) — counterintuitive; the naive instance-field mapping silently miscompiles a method-WS counter.
- Method-local data-name identical to an object data-name → the method-local declaration wins inside the method; the object data is inaccessible there (§11.7 GR5). Emit the local/static method field, shadow the instance field.
- RETURNING on a method declared void, or a void INVOKE that omits RETURNING when the method has one → compile-time error (signature mismatch; Roslyn enforces once the symbol table picks the right overload-free method).
- NEW on an ABSTRACT class → compile-time error (`new` on an abstract C# class is illegal — Roslyn enforces; also reject at bind for a clear diagnostic).
- OVERRIDE without a matching base signature, or overriding a FINAL method → compile-time error (§11.7 SR3; C# `override` of a non-existent/sealed base is a compile error — reject at bind for a COBOL-worded diagnostic).
- Object data that is a GROUP or fixed OCCURS table → per-instance record struct / array, byte-identical mapping to PROGRAM data except instance-vs-static (proven by oo_object_group: R1 filled, untouched R2 keeps defaults).
- SELF in a FACTORY method resolves on the factory interface; SELF in an instance method on the instance interface (§14.9.23.3 SR4f/g) — pick `this` consistently; SUPER must name object-class-name when INHERITS lists 2+ bases (§8.4.3.8.3 SR5) — moot under the v1 single-inheritance restriction.
- Universal object reference (`OBJECT REFERENCE` with no class) → C# `object?`; INVOKE through it forbids BY CONTENT/BY VALUE (§14.9.23.3 SR6) and uses the __CobolInvoke dynamic path; `identifier-2` (method-name in a data item) is allowed only for a universal reference (§14.9.23.3 SR7).
- `SET o TO NULL` → `o=null`; `IF o = NULL`/`o = q` → reference comparison `o is null`/`ReferenceEquals`; `o IS Class`/`o IS NOT Class` → C# `o is Class`.
- INVOKE arg is a literal or BY CONTENT → synthesize a copy (a temp local) so the method cannot mutate the caller's literal/source — mirrors the §14.9.23.4 GR6 content semantics.
- Same class inherited indirectly twice (diamond, §11.3.2 GR4) → one copy of its data; under single-inheritance v1 this cannot arise via direct INHERITS, but note it for the eventual multiple-inheritance design.
- Method-name AS literal-1 (externalized name, §11.7.4 GR1b) and CALL-CONVENTION/ENTRY-CONVENTION naming → deferred; v1 uses the COBOL method-name directly as the C# method name (sanitized).

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

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- OWNER-LEVEL: COBOL allows MULTIPLE class inheritance (§11.3.2 `INHERITS FROM {object-class-name-2}…`); C# allows only one base class. v1 restricts to single inheritance (sufficient for the entire current corpus). When a multi-base program appears, choose: (a) linearize to one C# base + extract secondary supers as C# interfaces the class IMPLEMENTS (with member forwarding), or (b) declare multiple inheritance unsupported. Needs an owner decision before any multi-base program is targeted.
- Parametric polymorphism (overloading by method-resolution-signature, §12063) is OPTIONAL and currently deferred (no corpus use). If targeted, decide between C# name-mangling by signature vs leaning on the processor's object-management-system methodology the spec permits — an owner/architecture call about generated-name idiomaticity.
- Grammar extension scope/ordering: the reused live Core/CobolOO.g4 lacks FACTORY, INTERFACE-ID/IMPLEMENTS, PROPERTY (GET/SET), method attributes (OVERRIDE/FINAL/ABSTRACT/STATIC/PUBLIC/PRIVATE/PROTECTED), method-name `AS literal`, and qualified `object-class-name OF SUPER`. These must be added incrementally (guard-fast after EACH per the prior LL-regression lesson) before the corresponding emit slices. Which OO features are in v1 scope vs later (FACTORY and PROPERTY are the next two natural slices) is a prioritization call.
- INTERFACE-ID + IMPLEMENTS → C# interfaces: the spec's interface-conformance model (§9.3.8.2.3) is richer than C# structural/nominal interface satisfaction. Confirm whether v1 maps IMPLEMENTS to plain C# interface implementation (Roslyn-checked) or needs an explicit conformance pass.
- EC-OO-* exception catalog integration (EC-OO-NULL/METHOD/UNIVERSAL/ARG-OMITTED) depends on the cross-cutting EC/exception subsystem; v1 raises EC-OO-NULL/METHOD and honors inline ON EXCEPTION, but full EC handling + the >>TURN/CHECKING enable machinery is owned by that subsystem — needs sequencing with it.
