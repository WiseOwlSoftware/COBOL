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

### D4. State→storage matrix: plain → instance fields on a cached singleton (last-used); INITIAL → re-init WS to VALUE each activation; RECURSIVE/function/method → fresh instance per activation; LOCAL-STORAGE → re-init per activation; EXTERNAL → one static run-unit holder per name (not reset by CANCEL); GLOBAL → field on the outer class visible to nested classes; LINKAGE → carrier-bound (never initialized); CANCEL → re-init to initial state on next CALL.

**Rationale.** Directly implements ISO §14.6.2.3 (initial/last-used), §8.6.6 (COMMON/INITIAL/RECURSIVE), §8.6.7 (EXTERNAL sharing), §14.9.5 (CANCEL).

**Rejected alternatives.** All-static fields (legacy) — rejected: cannot represent RECURSIVE per-activation copies or INITIAL re-init cleanly. A per-program ExecutionContext object threading byte StorageBlocks (legacy) — rejected: byte substrate.

### D5. The single sanctioned transient-byte boundary is confined to category-mismatch BY REFERENCE (e.g. a PIC X(4) argument viewed by the callee as PIC 9(4)) and to a data-pointer rebasing a differently-typed BASED item; same-category aliasing is always fully typed.

**Rationale.** `ManagedRef<string>` and `ManagedRef<long>` cannot alias the same typed location, but ISO BY REFERENCE = 'same storage area reinterpreted'. The architecture already permits a transient (never-persisted) byte image at unavoidable boundaries; confining it to category mismatch keeps the overwhelmingly common same-category case fully native.

**Rejected alternatives.** A universal byte window under all BY REFERENCE (legacy) — rejected: byte substrate for the common case. Forbidding category mismatch outright — rejected: it is legal COBOL exercised by NIST.

## C# mapping

> Dual-backend rule (SSOT §18 #23): ALL semantics in this section live in backend-neutral bound nodes behind `ICodeGenBackend` (`--backend roslyn|cil`); what follows is the RoslynBackend's C# rendering. Bound nodes carry structured forms (pass mode, carrier construction mode, caller PicMeta) — never pre-rendered C#-specific fragments; a future CIL backend renders the same bound semantics with its own private lowering.

PASSING MODES — caller side:
  BY REFERENCE: CALL "INC" USING CTR.  (01 CTR PIC 9(4))  ->  _INC.Run(ManagedRef<long>.OverField(()=>CTR, v=>CTR=v));   // callee mutation visible
  BY CONTENT:   CALL "P" USING BY CONTENT CTR.            ->  _P.Run(ManagedRef<long>.Cell(CTR));                       // copy; not visible
  BY VALUE:     CALL "P" USING BY VALUE N. (2002)         ->  _P.Run(N);                                                // plain value param
  subscripted:  CALL "P" USING TBL(I).   -> int _i=(int)I-1; _P.Run(ManagedRef<long>.OverField(()=>TBL[_i], v=>TBL[_i]=v));  // index captured once (GR3a)
  literal/expr: CALL "P" USING 5.        -> _P.Run(ManagedRef<long>.Cell(5L));                                          // inherently BY CONTENT
  OMITTED:      CALL "P" USING OMITTED.   -> _P.Run(ManagedRef<long>.Null);
CALLEE side — LINKAGE + PROCEDURE DIVISION USING:
  LINKAGE SECTION. 01 LK-CTR PIC 9(4).   PROCEDURE DIVISION USING LK-CTR RETURNING LK-R.
  ->  private ManagedRef<long> LK_CTR;
      public long Run(ManagedRef<long> p0){ LK_CTR = p0; /*proc body*/ return _ret; }   // refs to LK-CTR read/write LK_CTR.Value
  ADD 1 TO LK-CTR.  ->  LK_CTR.Value = CobolNum.Store(LK_CTR.Value + 1L, 0, _P_LK_CTR);   // the one unavoidable indirection
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
RETURNING / GOBACK:
  GOBACK RETURNING WS-R.  ->  _ret = WS_R; return _ret;   // (in a called program) ; in main -> terminate run unit
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

**ODO full-allocation rule (§14.2.3 GR8):** an occurs-depending group crosses the CALL boundary at its FULL maximum allocation, never the §13.18.38 GR8 current-extent window — GR8 says BY REFERENCE "operates as if the formal parameter occupies the same storage area as the argument", and the *storage* is the maximum allocation (the ODO window is a *sending-operand* rule for MOVE/compare/INSPECT, not a storage-aliasing rule). BY CONTENT groups follow the same full-allocation rule (GR9 — the copy is of the record). Mechanically: `CodeGen/Verbs/CallEmitter.cs::CallStringRead/CallStringWrite` are the ONE boundary read/write pair (the BY REFERENCE carrier, the BY CONTENT snapshot, the callee copy-in/copy-out, RETURNING) and bypass `OdoGroupPlace.SendingImage()`/`ReceiveInto` for the full `AsImage()`/`FromImage` forms.

**Boundary-copy limitation (known, accepted — re-architect only if a test ever observes it):** group formals are boundary-copied (`FromImage` at activation entry, `AsImage` copy-out at activation exit), not live-aliased; a STRICT reading of GR8 implies live sharing (a caller-side mutation of the group mid-call — e.g. from a re-entered container — would not be seen by the callee until the next activation). No NIST program observes mid-call group mutation from the caller's side; elementary formals ARE live-aliased (carrier-resident, per-access).

### RECURSIVE programs and functions/methods need per-activation data; a `static class Program` cannot recurse or hold per-activation copies.

Make each program an instantiable class. RECURSIVE/function/method → new instance per activation; plain program → cached singleton (last-used persistence). Non-RECURSIVE re-entry while active → EC-PROGRAM-RECURSIVE-CALL. This forces instance-ization of the program-class shape now (today's static Program is replaced).

### Opaque ABI for dynamic CALL / cross-assembly: the caller cannot see the callee's LINKAGE to build typed args.

Uniform ICobolProgram.Call(CobolArgs) where CobolArgs is an ordered list of (PassMode, caller PicMeta, carrier) — the typed analog of the rejected ManagedPointer[]. Callee maps positionally onto LINKAGE items. Same-assembly + statically-resolvable + PIC-conforming calls specialize to a direct typed method (Run) as a fast path.

### GOBACK / EXIT PROGRAM semantics differ between a called program and the main program; today's `throw StopRun` always unwinds the whole run unit.

In a called program, GOBACK/EXIT PROGRAM = normal method return to the caller (GOBACK RETURNING sets the return value); only in the main program (or STOP RUN anywhere) does it terminate the run unit. Mechanism (settled, SSOT §9.4 + §18 #10): a called program's GOBACK/EXIT PROGRAM raises `ProgramReturn`, caught at that program's `Entry` (carrying the RETURNING value back to the caller); main-program GOBACK / STOP RUN terminates the run unit with the status as the process exit code. The greenfield `StopRun` signal (`src/Cobol.Net.Runtime/Control/StopRun.cs`) remains only the run-unit-termination half — `ProgramReturn` is a distinct called-program-return signal, not a refinement of `StopRun`.

### CANCEL must reset a program to initial state on next CALL, close its open files, cascade to contained programs, and be a no-op for never-called/already-canceled or active programs.

Registry.Cancel(name) drops/marks the singleton for re-init (WS reset to VALUE on next CALL), runs implicit CLOSE on its open files (§14.9.5 GR9), cascades to contained programs in reverse order (GR4), raises EC-PROGRAM-CANCEL-ACTIVE if active (GR5), no-ops if never-called/canceled (GR7); EXTERNAL data is NOT reset (GR8).

### Arguments must be evaluated exactly once at CALL time (§14.9.4.4 GR3a) even though carriers are lazy accessors.

Capture subscript/ref-mod bound expressions into locals BEFORE constructing carriers (e.g. `int _i=(int)I-1;` then close over `_i`), so re-evaluating the accessor inside the callee does not re-evaluate the COBOL subscript expression.

## Edge cases

- Transitive passing mode (§14.9.4.4 GR5): default BY REFERENCE; a BY REFERENCE/CONTENT/VALUE phrase applies to all following args until the next phrase (port the legacy CallBinder mode-threading, drop its byte emission).
- Bare argument resolution (§14.9.4.4 GR9): a bare arg with a BY REFERENCE formal becomes BY REFERENCE if it is a valid receiving operand, else BY CONTENT (e.g. a literal/expression).
- OMITTED / trailing-omitted argument (GR11-12): carrier = Null; omitted-argument condition = IsNull; referencing an omitted param otherwise → EC-PROGRAM-ARG-OMITTED.
- Argument/parameter count mismatch → EC-PROGRAM-ARG-MISMATCH (when checking enabled) or diagnostic; a missing parameter behaves as omitted.
- RETURNING a group item → compile-time error (must be elementary, BY VALUE semantics).
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
- **2002+ (under `--std 85` reject with an "introduced in COBOL-2002" diagnostic):** CALL … BY VALUE; RETURNING (CALL and the procedure-division header); OMITTED arguments + the omitted-argument condition; PROGRAM-ID … RECURSIVE; LOCAL-STORAGE; GOBACK; USAGE POINTER / ADDRESS OF / SET ADDRESS OF; BASED / ALLOCATE / FREE; ANY LENGTH; and the whole EC-PROGRAM-* exception machinery (at `--std 85` CALL failures surface only via the ON OVERFLOW/EXCEPTION phrases / abnormal termination — no EC names, no `>>TURN`).
- **2014+:** `>>TURN` of EC-PROGRAM exceptions in a calling element is FLAG-02-flagged (`VERSION_CHANGE_REFERENCE.md` row 97).
- **2023:** CALL … ON OVERFLOW is REMOVED (row 3, E.2 item 1c) — reject at 2023, accept at 85/2002/2014; EXIT PROGRAM is archaic (rows 89/126) — flag at 2023; GOBACK gains the STOP-style status phrase (row 75) — 2023-only; EXTERNAL-item conformance exception checking is added (row 15).

## ISO citations

- ISO/IEC 1989:2023 §14.9.4 CALL statement (formats 1/2, syntax rules, general rules — incl. GR3a args-evaluated-once, GR3b-h resolution + EC-PROGRAM-NOT-FOUND/PTR-NULL/RESOURCES/ARG-MISMATCH/RECURSIVE-CALL, GR3i NOT ON EXCEPTION, GR5 transitive mode, GR11-12 OMITTED)
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
- Carrier name + representation — **SETTLED (§18 #12):** the carrier is the typed `ManagedRef<T>` (NOT the abandoned `byte[]`+offset+length form); the public name **`ManagedPointer`** is kept over the typed carrier (owner preference). The `feedback_managed_pointers` memory has been updated to the typed form.
- Cross-assembly dynamic CALL discovery — **SETTLED (owner-approved): the `__CobolModule` registrar + sibling-assembly probe.** Every compiled module emits ONE public well-known discovery surface, `public static class __CobolModule { public static void Register() }`, performing the same `ProgramRegistry.Register(...)` calls `Program.Main` uses (Main delegates to it — one canonical registration body; the generated program classes stay `internal`, so the registrar IS the reflection surface — `CodeGen/ProgramEmitter.cs::EmitEntryWrapper`). `ProgramRegistry.ResolveVisible`'s rule-4 fallthrough probes `AppContext.BaseDirectory` for `<name>.dll` (exact name, then a case-insensitive scan — Linux), loads it into `AssemblyLoadContext.Default`, invokes `__CobolModule.Register()`, and retries rule 4 once; probed names are cached hit-or-miss (one I/O probe per name per run unit) and an unloadable/foreign dll is a quiet miss → the ordinary EC-PROGRAM-NOT-FOUND surface. Spec basis: §14.6.1 ("A run unit contains one or more runtime modules"), §14.9.4.4 GR3b (the locate step is implementor-defined beyond §8.4.6.3 name scope). The prebuilt-static-registry profile (AOT/trimming) remains possible — pre-register every name and the probe never fires. Known edge (accepted): a probed module registering a containment PATH that collides with an existing registration overwrites it; per-outermost program-names are unique in a conforming run unit (§8.4.6.3), so collisions arise only from non-conforming compositions.
- Category-mismatch BY REFERENCE byte boundary — **SETTLED (§18 #1; SSOT §9.4):** the transient, never-persisted byte image IS the one sanctioned boundary for this narrow case (category mismatch is legal COBOL exercised by NIST — not a compile error); same-category stays 100% typed.
