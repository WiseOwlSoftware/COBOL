# COBOL.NET — Interprogram (CALL / cross-program data) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §9; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Decision-complete design for cross-program data + calls in COBOL.NET (COBOL→typed-native C#→Roslyn). The load-bearing problem is BY REFERENCE on typed-native fields with NO byte window. Resolution: ONE managed-reference carrier (the typed-native re-implementation of `ManagedPointer`, internally `ManagedRef<T>`) that serves BY REFERENCE args, LINKAGE items, USAGE POINTER, ADDRESS OF, BASED, ALLOCATE, and SET ADDRESS OF — honoring the owner's singular-pattern rule. Crucially the carrier does NOT box WORKING-STORAGE: an ordinary `01 WS-X PIC 9(4)` stays a native `long`; a carrier is built ONLY at a call site as `ManagedRef<long>.OverField(()=>WS_X, v=>WS_X=v)` (an accessor over the caller's native field) so DISPLAY/MOVE/arithmetic keep zero indirection. The calling convention has two layers: a uniform opaque ABI (`ICobolProgram.Call(CobolArgs)`, the typed analog of the rejected `Entry(ManagedPointer[])`) for dynamic/cross-assembly CALL, and a typed fast path (direct `R=_SUB.Run(...)`) for same-assembly, statically-resolvable, PIC-conforming calls. Each program becomes its own (instantiable, not static) C# class; nested programs are nested classes; recursion/functions/methods get a per-activation instance, plain programs a cached singleton for last-used persistence. RETURNING maps to the C# method return value (idiomatic). The only sanctioned transient-byte boundary is category-mismatch BY REFERENCE (PIC X(4) arg viewed as PIC 9(4)); same-category — the common case — is always fully typed. Pointers are always-typed (never byte-gated).

## Decisions

### D1. ONE managed-reference carrier `ManagedRef<T>` (the typed-native re-implementation of `ManagedPointer`) serves BY REFERENCE args, LINKAGE items, USAGE POINTER, ADDRESS OF, BASED, ALLOCATE/FREE, SET ADDRESS OF — with two construction modes (accessor-over-native-field | standalone cell) plus a Null state.

**Rationale.** Owner singular-pattern + managed-pointer rules mandate exactly ONE carrier for 'a managed reference to a storage location'; BY REFERENCE and pointers are the same concept. Accessor-over-field means WORKING-STORAGE stays native (no boxing), so only genuine aliases (LINKAGE / ALLOCATE) carry indirection — preserving the native-field North Star.

**Rejected alternatives.** (a) `CobolRef<T>` as a NEW parallel reference type — rejected: violates singular-pattern (owner would correct it). (b) Make every aliasable item itself a `CobolRef<T>` heap cell — rejected: re-introduces a uniform indirection layer over all storage (the byte-State sin in new clothes; reads through .Value on a heap object). (c) Legacy `ManagedPointer(byte[],offset,length)` — rejected: that IS the abandoned byte substrate; a managed ref can't serialize to stable bytes anyway. (d) C# `ref` parameters everywhere — rejected: `ref` can't be stored in a field (LINKAGE item must persist for the activation), can't be re-pointed by SET ADDRESS OF, and can't cross the opaque dynamic-CALL ABI.

### D2. Two-layer calling convention: a uniform opaque ABI (`interface ICobolProgram { int Call(CobolArgs args); void Cancel(); }`, `CobolArgs` = ordered (PassMode, caller PicMeta, carrier)) for dynamic + cross-assembly CALL; a typed fast path (direct typed method `R=_SUB.Run(carrier...)`) for same-assembly, statically-resolvable, conforming calls.

**Rationale.** The tightest constraint is `CALL identifier` and CALL to a separate `<name>.dll`: the caller can't see the callee's LINKAGE, forcing a uniform entry signature + uniform carrier array (the typed analog of the rejected `Entry(ManagedPointer[])`). Designing the opaque ABI first means the typed fast path is a pure optimization, not a retrofit.

**Rejected alternatives.** Typed ref-param signatures only — rejected: dynamic CALL and cross-assembly resolution would retrofit badly (the callee signature is unknown at the call site). Trailing-scratch-buffer RETURNING (legacy `InvokeNumericFunction`) — rejected: that is the byte ABI; RETURNING → idiomatic C# method return value instead.

### D3. Each program → its own instantiable (non-static) C# class; nested/contained programs → nested classes; the run unit compiles to one assembly with the first program as entry.

**Rationale.** Recursion, functions, and methods need a per-activation instance (§14.6.2.3); a static class cannot recurse. Plain non-recursive programs use a cached singleton instance to realize last-used persistence. Nested classes mirror COBOL nested-program scope (outer GLOBAL/COMMON visible, outer LOCAL-STORAGE not).

**Rejected alternatives.** Today's single `internal static class Program` bound to the first program unit — rejected: cannot host multiple/nested programs and cannot recurse. One assembly per program by default — deferred to an owner flag (separately-compiled `.dll` is supported via the opaque ABI but loses the typed fast path).

**The container's CONFIGURATION SECTION and OPTIONS apply to its containees (§12.3.4 GR1 / §11.9.4 GR1; PB60 / AR-15.67.3-5, 2026-08-17).** A contained program shall not have a configuration section of its own (§12.3.3 SR1 — COBOLNET1643), so it inherits the container's WHOLE configuration-derived state — SPECIAL-NAMES (DECIMAL-POINT IS COMMA, CURRENCY SIGN, CLASS names, ALPHABETs, switch mnemonics/conditions), the OBJECT-COMPUTER PROGRAM COLLATING SEQUENCE, SOURCE-COMPUTER DEBUGGING MODE, and the REPOSITORY specifiers — through the ONE `DataBinder.InheritConfiguration(container)`, called by `BinderDriver.BindUnitData` BEFORE the containee binds (its first literal and PICTURE already see the switches; units bind container-first, so one level carries the ancestry). OPTIONS inherit with clause-by-clause override: `OptionsBinder.Bind(program, edition, baseline)` starts from the container's model. Before this, only the REPOSITORY sets were inherited (after Bind), and a contained program under DECIMAL-POINT IS COMMA parsed `NUMVAL("123,45")` as 0 and `NUMVAL("123.45")` as 123.45 — the exact inversion of §15.67.3 r5, undiagnosed. Pinned by `pb60_nested_configuration_inheritance` (2023: SPECIAL-NAMES + OPTIONS inheritance and an inner OPTIONS override) and `nested_special_names_inheritance` (85: the edition-invariant SPECIAL-NAMES half); the negative `config-section-in-contained-program` carries SR1.

### D4. State→storage matrix (the §13.5.4/§13.6.4/§14.6.2.3 storage-class × unit-kind model): plain program → WS as instance fields on a cached singleton (static data realized by instance caching — the unit can never be concurrently active, §8.6.6, so one cached instance IS the one last-used copy); INITIAL → fresh instance per activation (WS is INITIAL data, §13.5.4 GR2 — instance fields re-initialize by construction); RECURSIVE (and every function/method, §8.6.6) → WS as STATIC C# fields (ONE per-class copy — §13.5.4 GR1 static data, last-used across ALL concurrent/successive activations per §14.6.2.3.3; INDEXED BY cells and Tier-B backings ride the same `StaticRootFields`/`StaticIndexCells` channel as method WS — one mechanism, two producers) over a fresh instance per activation that carries everything per-activation; LOCAL-STORAGE (program/function-level, bound via the ONE `BindEntries(EntrySection.LocalStorage)` path) → instance fields = automatic data (§13.6.4 GR1), in initial state EVERY activation — realized by the fresh instance for INITIAL/RECURSIVE units and by an emitted `Call`-entry re-initialization (the same composed `ValueInitializer` initializers) for cached-singleton units; EXTERNAL → one run-unit holder per externalized name on the `ExternalStore` (not reset by CANCEL, §14.9.5 GR8; never a per-class static); GLOBAL → field on the outer class instance, reached by nested classes through `__outer` ref-bridges; LINKAGE → carrier-bound (never initialized); CANCEL → next CALL finds initial state (§14.9.5 GR3): the instance drops AND — for a RECURSIVE unit — the registered `__ResetStatics` reassigns every static WS field/index cell to its declaration initializer (also invoked at run-unit registration = the §14.6.2.3.2 case-1 trigger, and via an INITIAL container's implicit cancel cascade = case 2).

**Rationale.** Directly implements ISO §13.5.4 (WS = static/initial data), §13.6.4 (LS = automatic data), §14.6.2.3 (initial/last-used: automatic+initial data → initial state every activation; static data → initial only on the three §14.6.2.3.2 triggers; static+external are the ONLY last-used data), §8.6.6 (COMMON/INITIAL/RECURSIVE; functions always recursive), §8.6.7 (EXTERNAL sharing), §14.9.5 (CANCEL). A recursive unit's WS MUST NOT live on the per-activation instance — that would re-initialize it per activation, violating §14.6.2.3.3.

**Rejected alternatives.** All-static fields (legacy) — rejected: cannot represent RECURSIVE per-activation LOCAL-STORAGE/formal copies or INITIAL re-init cleanly. WS-on-the-fresh-instance for RECURSIVE units — rejected: miscompiles shared last-used WS into per-activation initial state (the pre-P10 defect, DEVLOG 864). A per-program ExecutionContext object threading byte StorageBlocks (legacy) — rejected: byte substrate.

**Named stages (honest subset, loud):** a RECURSIVE program that directly CONTAINS programs and declares WS **or a FILE SECTION** stages 0899 `recursive-contained-working-storage` (a containee's GLOBAL `__outer` ref-bridge aliases container-INSTANCE fields, which cannot reach class statics — and an FD may be GLOBAL, §13.18.27; the FILE SECTION arm joined with kb/Work PB168); BASED / ADDRESS-OF-taken records in a RECURSIVE unit's WS stage 0899 `recursive-working-storage-pointer-backed` (their `StorageCell`/address-pointer storage is per-instance). ~~Recorded residue: per-activation file connectors~~ — **CLOSED (kb/Work PB168):** a childless RECURSIVE unit's internal file connectors AND FD record areas are unit-scoped last-used state (§8.6.4 — one copy per run unit; §14.6.2.3.2 action 3 re-initializes them only on the static initial-state cases 1–3): the `__filesRegistered` guard emits STATIC (`DataBinder.UnitStaticFiles`), `__ResetStatics` clears it on exactly those cases, FILE SECTION records route through `RouteStaticUnitStorage`'s per-root routine like WS, report-engine construction rides its own per-INSTANCE `__reportsConstructed` guard, and the LINAGE evaluator installs unguarded per activation so the shared connector always evaluates the current instance's geometry.

### D5. The single sanctioned transient-byte boundary is confined to category-mismatch BY REFERENCE (e.g. a PIC X(4) argument viewed by the callee as PIC 9(4)) and to a data-pointer rebasing a differently-typed BASED item; same-category aliasing is always fully typed.

**Rationale.** `ManagedRef<string>` and `ManagedRef<long>` cannot alias the same typed location, but ISO BY REFERENCE = 'same storage area reinterpreted'. The architecture already permits a transient (never-persisted) byte image at unavoidable boundaries; confining it to category mismatch keeps the overwhelmingly common same-category case fully native.

**Rejected alternatives.** A universal byte window under all BY REFERENCE (legacy) — rejected: byte substrate for the common case. Forbidding category mismatch outright — rejected: it is legal COBOL exercised by NIST.

## C# mapping

> Dual-backend rule (SSOT §18 #23): ALL semantics in this section live in backend-neutral bound nodes behind `ICodeGenBackend` (`--backend roslyn|cil`); what follows is the RoslynBackend's C# rendering. Bound nodes carry structured forms (pass mode, carrier construction mode, caller PicMeta) — never pre-rendered C#-specific fragments; a future CIL backend renders the same bound semantics with its own private lowering.

PASSING MODES — caller side:
  BY REFERENCE: CALL "INC" USING CTR.  (01 CTR PIC 9(4))  ->  _INC.Run(ManagedRef<long>.OverField(()=>CTR, v=>CTR=v));   // callee mutation visible
  BY CONTENT:   CALL "P" USING BY CONTENT CTR.            ->  _P.Run(ManagedRef<long>.Cell(CTR));                       // copy; not visible
  BY VALUE:     CALL "P" USING BY VALUE N. (2002)         ->  new CobolArg(CobolPassMode.Value, ManagedPointer<long>.Cell((long)(N)), …);   // value copy allocated at call initiation and conformed to the formal (§14.2.3 GR10)
  subscripted:  CALL "P" USING TBL(I).   -> int _i=(int)I-1; _P.Run(ManagedRef<long>.OverField(()=>TBL[_i], v=>TBL[_i]=v));  // index captured once (GR3a)
  literal/expr: CALL "P" USING 5.        -> _P.Run(ManagedRef<long>.Cell(5L));                                          // inherently BY CONTENT
  OMITTED:      CALL "P" USING OMITTED.   -> _P.Run(ManagedRef<long>.Null);
CALLEE side — LINKAGE + PROCEDURE DIVISION USING:
  LINKAGE SECTION. 01 LK-CTR PIC 9(4).   PROCEDURE DIVISION USING LK-CTR RETURNING LK-R.
  ->  private ManagedRef<long> LK_CTR;
      public long Run(ManagedRef<long> p0){ LK_CTR = p0; /*proc body*/ return _ret; }   // refs to LK-CTR read/write LK_CTR.Value
  ADD 1 TO LK-CTR.  ->  LK_CTR.Value = CobolNum.Store(LK_CTR.Value + 1L, 0, _P_LK_CTR);   // the one unavoidable indirection
  header BY VALUE (ISO §14.2.2 using-phrase; 2002): PROCEDURE DIVISION USING BY VALUE LK-V.
  ->  __lnkp0 = CobolArgAdapt.NumValue(__args, 0, _P_LK_V, scale);   // a DETACHED cell conformed to the formal — the §14.2.3
      // GR10 "COMPUTE without ROUNDED" value copy; stores hit only the cell (NO copy-out — never the caller). Modes thread
      // per §14.2.3 GR4 (transitive; BY REFERENCE assumed first); LinkageFormal.ByValue carries the resolution. §14.2.2 SR2
      // restricts BY VALUE formals to class numeric/message-tag/object/pointer (COBOLNET1553); the carried leg is fixed-point
      // numeric — object/pointer/float stage loud (0899 by-value-formal-carrier). A UDF activation's arguments take BY VALUE
      // whenever the formal says so (§8.4.3.2.4 GR5c; argument class per §8.4.3.2.3 SR10 = COBOLNET1554) — ONE ABI, both paths.
UNIFORM ABI (dynamic / cross-assembly):
  CALL identifier WS-PGM USING A.  ->
      var _p = Registry.Resolve(WS_PGM);
      if (_p is null) { /* ON EXCEPTION s1 | throw EC-PROGRAM-NOT-FOUND */ }
      else { int _rc = _p.Call(new CobolArgs{ Args=[ new CobolArg(PassMode.Reference, PicMeta.A, ManagedRef<...>.OverField(...)) ] }); /* RETURNING; NOT ON EXCEPTION s2 */ }
POINTERS (2002):
  01 P USAGE POINTER. 01 B PIC X(5) BASED.
  SET P TO ADDRESS OF X.       -> P = ManagedRef<string>.OverField(()=>X, v=>X=v);
  SET ADDRESS OF B TO P.       -> _B_ref = P;                          // B has no own storage; refs to B go via _B_ref.Value
  ALLOCATE B.                  -> _B_ref = ManagedRef<string>.Cell(new string(' ',5));
  ALLOCATE 5 CHARACTERS RETURNING P. -> P = ManagedRef<string>.Cell(new string(' ',5));
  FREE P.                      -> P = ManagedRef<string>.Null;         // GC reclaims
  IF P = NULL                  -> if (P.IsNull)
RETURNING / GOBACK (the result is the PROCEDURE DIVISION header RETURNING item, §14.9.18.4 GR2 — in ISO it is NOT a GOBACK operand):
  PROCEDURE DIVISION … RETURNING LK-R.  GOBACK.  ->  return _ret;         // _ret tracks the header RETURNING item; called program → result to the activator, main → run unit ends
  GOBACK RETURNING WS-R.  (accepted vendor extension, gated 2002+)  ->  _ret = WS_R; return _ret;   // moves the operand into the header RETURNING item first
EXTERNAL / GLOBAL:
  01 SHARED PIC 9(4) EXTERNAL.  ->  long SHARED { get=>ExternalStore.GetL("SHARED"); set=>ExternalStore.SetL("SHARED",value); }   // one copy per run unit
  01 G PIC 9 GLOBAL. (outer)    ->  field on the OUTER class; nested classes read via the enclosing-instance reference
NESTED:
  PROGRAM-ID Inner inside Outer ->  private sealed class Inner : ICobolProgram { ... }  ; CALL "Inner" -> typed fast path to the nested class

## Hard problems

### BY REFERENCE with category mismatch (arg PIC X(4) seen as formal PIC 9(4)) — same storage reinterpreted, but ManagedRef<string> cannot alias a ManagedRef<long>.

Confine to the sanctioned transient-byte boundary: materialize the arg's value into a scratch byte image, hand the callee a ManagedRef<long> whose get/set decode/encode that buffer, write bytes back through the caller field's codec on return. Same-category (the common case) is always fully typed and never touches this path.

### BY REFERENCE of a group item — `record struct` is a value type; a closure `()=>G` copies on read, so naive aliasing loses callee mutations.

The group carrier round-trips the WHOLE struct per access: get reads a copy, set writes the entire struct back to the caller field, so subordinate-item mutations propagate as a unit at each store. OCCURS table args pass the `T[]` reference directly (element writes are naturally visible).

**ODO full-allocation rule (§14.2.3 GR8):** an occurs-depending group crosses the CALL boundary at its FULL maximum allocation, never the §13.18.38 GR8 current-extent window — §14.2.3 GR8 says BY REFERENCE "operates as if the formal parameter occupies the same storage area as the argument", and the *storage* is the maximum allocation (the ODO window is a *sending-operand* rule for MOVE/compare/INSPECT, not a storage-aliasing rule). BY CONTENT groups follow the same full-allocation rule (GR9 — the copy is of the record). Mechanically: `CodeGen/Verbs/CallEmitter.cs::CallStringRead/CallStringWrite` are the ONE boundary read/write pair (the BY REFERENCE carrier, the BY CONTENT snapshot, the callee copy-in/copy-out, RETURNING) and bypass `OdoGroupPlace.SendingImage()`/`ReceiveInto` for the full `AsImage()`/`FromImage` forms.

**THE VARIABLE-LENGTH CROSSING — a THIRD carrier form (§8.5.1.12; kb/Work PB204).** §14.9.4.3 SR12's flat
prohibition is FORMAT 1's; a Format-2 CALL, a CALL RETURNING and an INVOKE are governed by SR25 → §14.8.2.2 and
§14.8.3.2, which **admit** a variable-length group "subject to compatibility as described in 8.5.1.12". So the
boundary carries three forms, not two:

| the argument's storage | carrier | read / write |
|---|---|---|
| a native fixed-point leaf | `ManagedPointer<T>` over the field's own carrier type | `PlaceRenderer.Read`/`Write` |
| any fixed-window character storage, a group included | `ManagedPointer<string>` — the record image | `CallEmitter.CallStringRead`/`CallStringWrite` |
| a VARIABLE-LENGTH group | `ManagedPointer<CobolVarGroup>` | `PlaceRenderer.VarGroupImage`/`WriteVarGroupImage` |

`CobolVarGroup` is `(string Fixed, string[] Dynamic)` and is the §8.5.1.12 model itself, not an encoding:
`Fixed` is the group's image with every variable-length component collapsed to nothing — the exact accounting
§8.5.1.12.3 states the relation in, which is why two COMPATIBLE groups lay it out identically — and `Dynamic`
carries each component's current content in declaration order, which §8.5.1.12.2's positional correspondence puts
one-for-one on both sides. A receiving dynamic-capacity table recovers its capacity by dividing its component by
its OWN element width, legitimate because §8.5.1.12.3 admits corresponding tables only "when the byte length of
their elements is equal". Nested variable-length groups FLATTEN into the same carrier (`CobolVarGroup.Slice`
hands one its window back), because the relation is stated over relative byte positions and is blind to the
declaration tree. The emitted pair is `AsVarImage()`/`FromVarImage()`, gated on `DataItem.CurrentExtentImageCapable`
— **the same** capability the §14.9.11.4 GR7 DISPLAY format uses, so a group that displays is a group that
crosses. The one crossing-form predicate is `CallEmitter.CallPlaceIsVarGroup` (CALL/RETURNING) and
`OoEmitter.OoCrossingType` (the INVOKE signature, box lanes and marshaling); the ADMISSION is decided once, at
bind, by `VariableLengthCompatibility.Mismatch` through `OoConformance.DescriptionMismatch` — the same comparator
the argument, RETURNING and override/implements checks all read.

**Tier-C at the boundary, in BOTH halves.** A group with no boundary image at all (a pointer/object-class leaf,
or a variable-length shape outside the current-extent gate — an OCCURS DEPENDING member, a runtime-length item
inside a table element; `DataItem.BoundaryImageCapable`) stages the documented Tier-C loud rather than
crossing. ⛔ The WRITE half does not test that predicate itself: `CallStringWrite` hands **every** non-`RedefViewPlace`
group to `PlaceRenderer.WriteFullGroupImage`, whose arm order owns the guard, so the read/write lockstep this
paragraph asserts is a STRUCTURAL fact rather than a coincidence two guards have to maintain. It was not, once:
the write half carried its own `IsImageCapable` conjunct and an imageless group fell through to a raw
`PlaceRenderer.Write`, rendering `_G = <string>;` — a backend CS0029 — while the read half correctly staged the
loud (kb/Work PB177 arm B, the eighth two-arm-dispatch instance). The exposure was not only at a CALL site: the
CALLEE's own LINKAGE formal copy-in (`ProgramEmitter`) and Report Writer's `CONTROL IS <group>` both call this
pair, and separate compilation means the caller-side `ArgText` screen gives the callee nothing.

**Boundary-copy limitation (known, accepted — re-architect only if a test ever observes it):** group formals are boundary-copied (`FromImage` at activation entry, `AsImage` copy-out at activation exit), not live-aliased; a STRICT reading of GR8 implies live sharing (a caller-side mutation of the group mid-call — e.g. from a re-entered container — would not be seen by the callee until the next activation). No NIST program observes mid-call group mutation from the caller's side; elementary formals ARE live-aliased (carrier-resident, per-access).

### RECURSIVE programs and functions/methods need per-activation data; a `static class Program` cannot recurse or hold per-activation copies.

Make each program an instantiable class. RECURSIVE/function/method → new instance per activation; plain program → cached singleton (last-used persistence). Non-RECURSIVE re-entry while active → EC-PROGRAM-RECURSIVE-CALL. This forces instance-ization of the program-class shape now (today's static Program is replaced).

### Opaque ABI for dynamic CALL / cross-assembly: the caller cannot see the callee's LINKAGE to build typed args.

Uniform ICobolProgram.Call(CobolArgs) where CobolArgs is an ordered list of (PassMode, caller PicMeta, carrier) — the typed analog of the rejected ManagedPointer[]. Callee maps positionally onto LINKAGE items. Same-assembly + statically-resolvable + PIC-conforming calls specialize to a direct typed method (Run) as a fast path.

### GOBACK and EXIT PROGRAM terminate a unit differently by activation context — and the two statements DIVERGE in the not-under-a-caller (main) case.

Distinguish by BOTH the statement and the activation context (§14.9.18.4 / §14.9.14.4):
- In a CALLED program (under the control of a calling runtime element) GOBACK and EXIT PROGRAM both terminate the program and return control to the activator (§14.9.18.4 GR2 / §14.9.14.4 GR3); the activation result is the current value of the PROCEDURE DIVISION header RETURNING item (§14.9.18.4 GR2), and a RAISING phrase stages its exception condition for the activator.
- In the MAIN program (not under the control of a caller) the two DIVERGE: GOBACK operates as a STOP statement and terminates the run unit (§14.9.18.4 GR3 — a RAISING phrase is then ignored, and a WITH NORMAL/ERROR STATUS reaches the OS as the exit code); EXIT PROGRAM is treated as a CONTINUE — a no-op that falls through to the next statement, raising no exception condition even when RAISING is specified (§14.9.14.4 GR2).

Mechanism (settled, SSOT §9.4 + §18 #10): GOBACK raises `ProgramReturn`; EXIT PROGRAM raises `ProgramReturn` ONLY while the unit is active as a callee (guarded on the `__asCalled` activation flag) and is inert otherwise. A `ProgramReturn` is caught at the unit's own activation/dispatch boundary: for a called unit it returns control (and the header RETURNING value) to the activator; for the main unit's GOBACK it returns to that boundary and the run unit then ends, the main-program status flushing as the process exit code. The distinct `StopRun` signal (`src/Cobol.Net.Runtime/Control/Signals/StopRun.cs`) is the run-unit-termination half (STOP RUN, and the STOP-status exit-code flush); `ProgramReturn` is the called-program-return signal, not a refinement of `StopRun`.

### CANCEL must reset a program to initial state on next CALL, close its open files, cascade to contained programs, and be a no-op for never-called/already-canceled or active programs.

Registry.Cancel(name) drops/marks the singleton for re-init (WS reset to VALUE on next CALL), runs implicit CLOSE on its open files (§14.9.5 GR9), cascades to contained programs in reverse order (GR4), raises EC-PROGRAM-CANCEL-ACTIVE if active (GR5), no-ops if never-called/canceled (GR7); EXTERNAL data is NOT reset (GR8).

### Arguments must be evaluated exactly once at CALL time (§14.9.4.4 GR3a) even though carriers are lazy accessors.

Capture subscript/ref-mod bound expressions into locals BEFORE constructing carriers (e.g. `int _i=(int)I-1;` then close over `_i`), so re-evaluating the accessor inside the callee does not re-evaluate the COBOL subscript expression.

## Edge cases

- Transitive passing mode: at the CALL site the BY CONTENT and BY REFERENCE phrases are transitive across the following arguments until the next such phrase, defaulting to BY REFERENCE (§14.9.4.4 GR5 — a Format-1 CALL has no BY VALUE); BY VALUE (only in the Format-2 program-prototype CALL) rides the argument-correspondence BY REFERENCE/BY VALUE transitivity (§14.2.3 GR4). One CallBinder mode-threading pass threads all three, drop its byte emission.
- Bare argument resolution (§14.9.4.4 GR9): a bare arg with a BY REFERENCE formal becomes BY REFERENCE if it is a valid receiving operand, else BY CONTENT (e.g. a literal/expression).
- OMITTED / trailing-omitted argument (GR11-12): carrier = Null; omitted-argument condition = IsNull; referencing an omitted param otherwise → EC-PROGRAM-ARG-OMITTED.
- Argument/parameter count mismatch → EC-PROGRAM-ARG-MISMATCH (when checking enabled) or diagnostic; a missing parameter behaves as omitted.
- RETURNING a group item: an **image-form** group — every leaf `DataItem.ElementImageCapable`, i.e. character-stored OR any pinned numeric byte form (zoned DISPLAY, binary, packed, COMP-5, IEEE float, INDEX) — is carried; the caller temp deep-clones the description and the image crosses via AsImage/FromImage (§8.4.3.2.4 GR1; §14.2.2 SR5 places no category restriction, and none on usage either). Only the strong-typed / internal-REDEFINES / variable-length shapes and a **pointer- or object-class LEAF** stage loud (the per-shape COBOLNET1510 residues in `UdfBinder.UdfReturningResidue`). A byte-form numeric leaf was listed here as a residue until PB164's F8 widened the screen off its hand-rolled DISPLAY-only usage union onto the derived predicate (kb/Work PB199).
- RETURNING with no caller target → value discarded.
- CALL to a NULL program-pointer → EC-PROGRAM-PTR-NULL; unresolvable name → EC-PROGRAM-NOT-FOUND; both route to ON EXCEPTION if present.
- ON EXCEPTION / ON OVERFLOW edition surface: `[NOT] ON EXCEPTION` is ANSI X3.23-1985 CALL Format 2 surface (CCVS-85 IC222A tests both phrases), valid at EVERY edition; `ON OVERFLOW` is the 74-carried synonym, valid 85–2014 and REMOVED at 2023 (`VERSION_CHANGE_REFERENCE.md` row 3 / E.2 item 1c → COBOLNET0882). NOT ON EXCEPTION runs only on a successful, non-EC-propagating return (§14.9.4.4 GR3i).
- Variable-occurrence (OCCURS DEPENDING ON) BY CONTENT arg: copy the maximum size; callee honors the DEPENDING-ON value (§14.2.3 GR9).
- BASED item with no associated pointer (unallocated / after FREE): reference is undefined → IsNull guard; double FREE / use-after-free guarded.
- EXTERNAL items survive CANCEL (§14.9.5 GR8) and are shared across all programs describing the same external name (§8.6.7).
- COMMON nested program is callable by sibling contained programs (§8.4.6.3); non-COMMON only by its direct container.
- Recursive COMMON program sharing state — deterministic, allowed.
- REDEFINES-a-pointer-as-bytes or writing a pointer to a file → ISO implementor-defined; reject as undefined (a managed ref cannot serialize to stable bytes).
- ADDRESS OF as a sending operand passed BY REFERENCE across a CALL (address-identifier, §14.9.4 SR3-4) passes the carrier itself.
- ANY LENGTH formal parameter (excluded from BY REFERENCE Format-1 outermost; relevant for functions/methods) — defer to UDF/method slices but reserve carrier metadata to carry length.
- SET ADDRESS OF on a LINKAGE item re-points the callee's formal mid-execution — the carrier field is reassigned; subsequent refs read the new target.

## Edition gating (G1 — four per-`--std` compilers in one executable)

Interprogram constructs vary heavily by edition. Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. At each `DialectLevel` (85/2002/2014/2023) the lacks-it diagnostic is a targeted `COBOLNET-` diagnostic — never a generic parse error. Every row gets (construct × edition) coverage per `docs/VERSION_TEST_MATRIX_DESIGN.md` (the (construct × edition) matrix; Phase 0 done); verify rows against `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist — 2002→2023 deltas ONLY; it has NO 85→2002 rows, so derive 85↔2002 gating from the 2002 standard) and the per-edition spec text.

- **All editions (85+):** CALL USING BY REFERENCE / BY CONTENT, CALL … ON OVERFLOW (removed at 2023 — below), CALL … [NOT] ON EXCEPTION (X3.23-1985 CALL Format 2 — CCVS-85 IC222A tests both phrases), CANCEL, nested/contained programs, COMMON / INITIAL, GLOBAL / EXTERNAL, EXIT PROGRAM.
- **2002+ (under `--std 85` reject with an "introduced in COBOL-2002" diagnostic):** CALL … BY VALUE AND the procedure-division-header USING BY VALUE phrase (twin registry rows `call-by-value-2002` / `pd-header-by-value-2002`); RETURNING (CALL and the PROCEDURE DIVISION header — the ISO §14.9.18 GOBACK/EXIT PROGRAM carry NO RETURNING operand; a `GOBACK`/`EXIT PROGRAM` `RETURNING`/`GIVING` operand is an accepted vendor extension over that header-only mechanism, `GobackReturning2002`); OMITTED arguments + the omitted-argument condition; PROGRAM-ID … RECURSIVE (`program-id-recursive-2002`); LOCAL-STORAGE (`local-storage-section-2002`); GOBACK; USAGE POINTER / ADDRESS OF / SET ADDRESS OF; BASED / ALLOCATE / FREE; ANY LENGTH; and the whole EC-PROGRAM-* exception machinery (at `--std 85` CALL failures surface only via the ON OVERFLOW/EXCEPTION phrases / abnormal termination — no EC names, no `>>TURN`).
- **2014+:** `>>TURN` of EC-PROGRAM exceptions in a calling element is FLAG-02-flagged (`VERSION_CHANGE_REFERENCE.md` row 97).
- **2023:** CALL … ON OVERFLOW is REMOVED (row 3, E.2 item 1c) — reject at 2023, accept at 85/2002/2014; EXIT PROGRAM is archaic (rows 89/126) — flag at 2023; GOBACK gains the STOP-style status phrase (row 75) — 2023-only; EXTERNAL-item conformance exception checking is added (row 15).

## ISO citations

- ISO/IEC 1989:2023 §14.9.4 CALL statement (formats 1/2, syntax rules, general rules — incl. GR3a args-evaluated-once, GR3b-h resolution + EC-PROGRAM-NOT-FOUND/PTR-NULL/RESOURCES/ARG-MISMATCH/RECURSIVE-CALL, GR3i NOT ON EXCEPTION, GR5 transitive BY CONTENT/BY REFERENCE (Format 1), GR11-12 OMITTED)
- §14.2 Procedure division structure: §14.2.2 SR1 (formal param = level 01/77 in LINKAGE, no BASED/REDEFINES), §14.2.3 GR2 positional correspondence, GR4 transitive BY REFERENCE/VALUE, GR6-7 RETURNING (storage in caller), GR8 BY REFERENCE = same storage area, GR9 BY CONTENT = allocated copy then by-reference, GR10 BY VALUE = allocated value copy
- §14.8.2 Parameters / §14.8.3 Returning items / §14.8.4 External items (conformance; EC-EXTERNAL-DATA-MISMATCH / FORMAT-CONFLICT / FILE-MISMATCH)
- §14.6.2.3 Initial and last-used states of data (§14.6.2.3.2 initial, §14.6.2.3.3 last-used); §14.6.2.4 initial state of object data
- §8.6.4 Automatic/initial/static internal items; §8.6.5 Based entries and based data items (ADDRESS OF, association lifetime); §8.6.6 Common, initial, and recursive attributes; §8.6.7 Sharing data items (EXTERNAL)
- §14.9.5 CANCEL statement (GR3 initial state on next CALL, GR4 cascade to contained, GR5 EC-PROGRAM-CANCEL-ACTIVE, GR7 no-op never-called/canceled, GR8 EXTERNAL not reset, GR9 implicit CLOSE)
- §14.9.3 ALLOCATE statement / §14.9.15 FREE statement (managed dynamic storage; INITIALIZED)
- §14.9.39 SET statement — pointer/address forms (SET p TO ADDRESS OF x; SET ADDRESS OF based-item TO p)
- §14.9.14 EXIT statement (the EXIT PROGRAM format — archaic in 2023, Annex F.1 item 1). NOTE: ISO 1989 defines NO ENTRY statement — alternate entry points are a vendor extension outside `--std` conformance; if ever supported, gate as an extension.
- §13.18.5 BASED clause; §8.4.3 ADDRESS OF identifier; §13.18.60.3 USAGE clause SR8–SR9 (program-pointer / data-pointer reference restrictions)
- §8.4.6 / §8.4.6.3 Scope of names / Scope of program-names (nested + COMMON visibility, program-name resolution)

## Open questions — ALL SETTLED (`COBOLNET_DESIGN.md` §18 + the owner-approved cross-assembly probe)

- Program model — **SETTLED (§18 #8):** ONE `.g.cs` + ONE assembly per compilation; multiple/contained programs → nested classes with same-assembly direct CALL (the typed fast path everywhere). A per-unit/by-name separately-compiled `.dll` option is a later need and, when used, crosses the uniform opaque ABI (typed fast path unavailable across assemblies).
- Carrier name + representation — **SETTLED (§18 #12):** the carrier is the typed `ManagedRef<T>` (NOT the abandoned `byte[]`+offset+length form); the public name **`ManagedPointer`** is kept over the typed carrier (owner preference). The `feedback_one_mechanism_per_job` memory has been updated to the typed form.
- Cross-assembly dynamic CALL discovery — **SETTLED (owner-approved): the `__CobolModule` registrar + sibling-assembly probe.** Every compiled module emits ONE public well-known discovery surface, `public static class __CobolModule { public static void Register() }`, performing the same `ProgramRegistry.Register(...)` calls `Program.Main` uses (Main delegates to it — one canonical registration body; the generated program classes stay `internal`, so the registrar IS the reflection surface — `CodeGen/ProgramEmitter.cs::EmitEntryWrapper`). `ProgramRegistry.ResolveVisible`'s rule-4 fallthrough probes `AppContext.BaseDirectory` for `<name>.dll` (exact name, then a case-insensitive scan — Linux), loads it into `AssemblyLoadContext.Default`, invokes `__CobolModule.Register()`, and retries rule 4 once; probed names are cached hit-or-miss (one I/O probe per name per run unit) and an unloadable/foreign dll is a quiet miss → the ordinary EC-PROGRAM-NOT-FOUND surface. Spec basis: §14.6.1 ("A run unit contains one or more runtime modules"), §14.9.4.4 GR3b (the locate step is implementor-defined beyond §8.4.6.3 name scope). The prebuilt-static-registry profile (AOT/trimming) remains possible — pre-register every name and the probe never fires. Known edge (accepted): a probed module registering a containment PATH that collides with an existing registration overwrites it; per-outermost program-names are unique in a conforming run unit (§8.4.6.3), so collisions arise only from non-conforming compositions.
- Category-mismatch BY REFERENCE byte boundary — **SETTLED (§18 #1; SSOT §9.4):** the transient, never-persisted byte image IS the one sanctioned boundary for this narrow case (category mismatch is legal COBOL exercised by NIST — not a compile error); same-category stays 100% typed.
