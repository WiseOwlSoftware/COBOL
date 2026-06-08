# OO COBOL → .NET classes — Implementation Design (Stage-4 OO)

> **Canonical reference for the COBOL Object-Orientation subsystem.** This is the **LIVE turnkey design** — defer to
> it. **Owner directive (2026-06-08, DEVLOG 453): OO is built TYPED-NATIVE per ADR §7** — a class's object data is a
> **per-instance typed record** (object `01`/elementary items → per-instance .NET fields), NOT the byte `ProgramState`
> image; the `EnableTypedFields` default-OFF gate is migration-safety for the *legacy* corpus and does NOT apply to OO
> (a new subsystem with no corpus to preserve), so `CollectTypedFields` is **always-on for a CLASS**. **SLICE 1 done
> (DEVLOG 453, char-first):** object CHARACTER data flips to per-instance .NET `string` fields — the typed-field
> machinery gained an instance dimension on the same `StateIsInstance` flag the byte `State` rides, via one chokepoint
> `CilDataEmitter.EmitTypedFieldOwner` (the typed analogue of `EmitLoadBackingArray`). Proven per-instance by
> `oo_instance_data` + `OoTests.TypedObjectData_IsPerInstance`; `oo_hello`'s `MSG` is now `instance String _T_MSG`.
> **OO-TYPED numeric done (DEVLOG 454):** object numerics → per-instance `long`/`decimal` (`TICKER._T_N`, `ACC._T_BAL`
> are `instance Int64`) + the general typed-numeric→byte MOVE cell. **MULTI-METHOD classes + INVOKE SELF done (DEVLOG
> 455):** a class hosts N `METHOD-ID` units (each its own .NET method + exit-bounded dispatch range; per-method
> paragraph scope fixes CBL3104), `INVOKE SELF` → `callvirt` (COBOL0112 lifted); conformance `oo_self` (`N=2`). NEXT:
> subclass own typed OBJECT data (lift COBOL0113), then FACTORY. **Implementation status (DEVLOG 447–455):** the OO **grammar is DONE**
> (`src/CobolSharp.Compiler/Grammar/Core/CobolOO.g4`, `{is2002()}?`-gated) AND **SLICES 1–3a are DONE end-to-end**
> (slice 3a = `INHERITS FROM` + virtual methods + polymorphism: a subclass extends a base, inherits the root's
> per-instance State via ctor-chaining, overrides base methods, dispatched virtually through a base-typed reference;
> conformance `oo_inherit`. **INVOKE SUPER** (an override calling the base — `oo_super`) is also done. Deferred
> forms fail loudly: subclass own data → COBOL0113, INVOKE SELF → COBOL0112 (needs multi-method), SUPER in a root
> class → COBOL0115, unknown base → COBOL0114). Earlier: **SLICES 1–2** —
> `CLASS-ID` → an instance .NET reference type with a per-instance `ProgramState`, `INVOKE class "NEW" RETURNING o`
> (`newobj` + the public `.ctor`), `INVOKE o "method" USING … RETURNING …` (`callvirt` on the CALL `ManagedPointer[]`
> ABI — an OO method is an instance "Entry", static cross-type resolution), `USAGE OBJECT REFERENCE` storage, and
> PERFORM inside an OBJECT method. Per-instance state proven (two objects accumulate independently). All live:
> `EmitClassModule` / `EmitClassMethodBody` / `BoundInvokeStatement` / `IrInvoke` / `EmissionContext.StateIsInstance`;
> conformance `tests/conformance/2002/oo_hello` + `oo_method_perform` + `oo_method_args`. So OO is **slices 1–2
> complete (single-method class, USING/RETURNING args), ~40% end-to-end**; §5 slices 3–6 below are the remaining
> queue. **Known scope limits** (DEVLOG 447–448): single method per class; OBJECT REFERENCE fields and (future)
> typed-native OBJECT fields are emitted *static* (correct for the driver-program INVOKE site, but a class that
> itself holds object refs / typed fields needs per-instance versions); slice-2 USING args are BY REFERENCE data
> references (BY VALUE/CONTENT + literal args later); INHERITS, SELF/SUPER, FACTORY, PROPERTY, polymorphism are
> slices 3–6.
> **Stack:** .NET 10 / C# 14. **Backend:** CIL-only via Mono.Cecil (NO custom VM / NO bytecode interpreter; a Roslyn
> C# backend is a FUTURE additive Stage-5 option, Cecil = oracle). **Object identity** = the .NET reference itself
> (GC-managed; **no handle table, no `unsafe`**). OBJECT-REFERENCE NULL/compare reuses the *shape* of the pointer
> machinery but on a real `object`/class reference (the pointer carrier is `ManagedPointer`; OO uses real references).
> **Plan SSOT:** `docs/MASTER_PLAN.md` (Phase B1). **Parent ADR:** `docs/DATA_MODEL_ARCHITECTURE.md` §7. **Live plan:**
> `docs/ISO2023_CONFORMANCE_PLAN.md` §0.5 (Stage-4, after pointers).
>
> The §0–§7 below (the turnkey vertical-slice roadmap) are authoritative. Sections **§A–§F (Appendix)** at the end
> capture the broader *target-design surface* (properties/indexers, .NET interop type mapping, access modifiers, the
> edge-case behavior catalog, debugger integration); they describe the *eventual* OO feature set (slices 5–6+ and
> beyond), not slice-1 scope.

**Parent:** `docs/DATA_MODEL_ARCHITECTURE.md` §7 (the settled OO→.NET mapping) and
`docs/ISO2023_CONFORMANCE_PLAN.md` §0.5 (Stage-4, after pointers). Grounded in ISO/IEC 1989:2023
§11.3/§11.4/§11.7/§11.8, §14.9.23 (INVOKE), §8.4.3.8 (SELF/SUPER), §16.2.1 (NEW).

> This document is the engineering roadmap for COBOL Object-Orientation. It does **not** re-open the ADR's settled
> vision (OO → real .NET classes; object identity is a managed reference; object instance data is a per-instance
> record). It turns that into a concrete, staged, guard-green build.

## 0. Target model (ISO → .NET, ADR §7)

| COBOL OO | .NET |
|---|---|
| `CLASS-ID. Foo [INHERITS FROM Bar].` | `public class Foo [: Bar]` |
| `OBJECT.` instance data | instance fields of `Foo` (a per-instance `ProgramState` — see §4) |
| instance `METHOD-ID. M.` | `public virtual` instance method `Foo.M(…)` |
| `FACTORY.` data / `METHOD-ID. M.` | `static` fields / `static` method `Foo.M(…)` |
| `USAGE OBJECT REFERENCE Foo` / universal | a `Foo` reference field / `object?` |
| `INVOKE obj "M" USING …` | `obj.M(…)` (`callvirt`) |
| `INVOKE Foo "NEW" RETURNING o` | `o = new Foo()` (`newobj`) |
| `SELF` / `SUPER` | `this` / `base` |
| `SET o TO NULL` / `o = NULL` | `o = null` / `o == null` (reuse the pointer NULL machinery shape) |
| `PROPERTY` / `GET`/`SET PROPERTY` | C# property |

## 1. Minimal vertical slice (the first observable OO program)

One source file, two compilation units (a PROGRAM that drives, a CLASS that's invoked) — ISO §11.2 allows both in
one compilation group:

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OODEMO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS GREETER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE OBJECT REFERENCE GREETER.
       PROCEDURE DIVISION.
           INVOKE GREETER "NEW" RETURNING G.
           INVOKE G "SAYHELLO".
           STOP RUN.
       END PROGRAM OODEMO.

       IDENTIFICATION DIVISION.
       CLASS-ID. GREETER.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(13) VALUE "HELLO, WORLD!".
       PROCEDURE DIVISION.
       METHOD-ID. SAYHELLO.
       PROCEDURE DIVISION.
           DISPLAY MSG.
       END METHOD SAYHELLO.
       END OBJECT.
       END CLASS GREETER.
```
→ `HELLO, WORLD!`. `NEW` is the implicit built-in factory method (§16.2.1) — no explicit FACTORY needed.

## 2. Current state (grounded by the investigation)

- **Lexer tokens PRESENT** (`CobolLexer.g4`): `CLASS_ID`, `METHOD_ID`, `END_METHOD`, `END_INVOKE`, `INVOKE`,
  `INTERFACE_ID`, `SELF`, `SUPER`, `NULL_`, `REFERENCE`, `OBJECT`(?), `FACTORY`(?). **MISSING** (corpus-clean — safe
  to add): `INHERITS`, `ABSTRACT`, `OVERRIDE`, and attribute words `STATIC`/`PRIVATE`/`PROTECTED`/`PUBLIC` (FINAL
  exists for Report Writer — reuse the token). `FACTORY`/`OBJECT`/`END-CLASS`/`END-OBJECT`/`END-FACTORY` to verify.
- **Grammar:** `CobolParserOO.g4` is a DEAD separate parser (not generated/referenced) — a useful *sketch* to merge,
  not reuse. Live entry: `compilationUnit → compilationGroup → programUnit+` (`CobolParserCore.g4:73-88`);
  `identificationBody` accepts only `programIdParagraph | functionIdParagraph` (`:111-114`). A **stub**
  `invokeStatement` is already gated `{is2002()}? invokeStatement` in the statement list (`:537`,
  CobolExtensionsJsonXml.g4:23) — replace the stub with the real rule.
- **Pipeline:** `Compilation.cs` `CollectProgramContexts` iterates `group.programUnit()` only; each unit → its own
  `SemanticModel` + `IrModule`; `EmitAssembly` already emits **multiple types into one assembly** (so a CLASS unit +
  a PROGRAM unit in one file is already structurally supported once both are collected). `CilEmitter.EmitModule`
  creates ONE `_programType` per module. The `CALL` path emits `callvirt CobolProgramEntry.Invoke` — the pattern
  `INVOKE` mirrors (but as a direct `callvirt` on the emitted class).
- **Symbols:** `SymbolKind.Class`/`SymbolKind.Method` are reserved but unused; no `ClassSymbol`/`MethodSymbol`.
  (`SemanticBuilder`'s existing "ClassDefinition" is SPECIAL-NAMES character classes — unrelated.)

## 3. Foundational integration (lands with slice 1)

1. **Lexer** (`Core/CobolLexer.g4`): add `INHERITS`, `ABSTRACT`, `OVERRIDE`, `STATIC`, `PRIVATE`, `PROTECTED`,
   `PUBLIC` (+ `END-CLASS`/`END-OBJECT`/`END-FACTORY`, `FACTORY`, `OBJECT`, `OBJECT-REFERENCE` if absent). Corpus-check
   each (grep NIST/conformance for a standalone data-name occurrence) before adding — same discipline as ALLOCATE.
2. **Grammar** (`CobolParserCore.g4`): `compilationGroup : (programUnit | classUnit)+`; add `classUnit`
   (CLASS-ID + ENVIRONMENT? + factoryParagraph? + objectParagraph? + `END CLASS`), `classIdParagraph`,
   `factoryParagraph`/`objectParagraph` (each: DATA DIVISION? + methodDivision?), `methodDivision : methodDeclaration+`,
   `methodDeclaration` (`METHOD-ID. name attrs. … END METHOD name.`), the real `invokeStatement`
   (`INVOKE invokeTarget literal/id [USING …] [RETURNING id] [onException] END-INVOKE?`), and
   `objectReference : dataReference | SELF | SUPER | NULL`. Add `USAGE OBJECT REFERENCE [class-name]` to the data
   `usageClause`. **Regenerate the parser** (the build does this from the .g4).
3. **Pipeline** (`Compilation.cs`): `CollectProgramContexts` collects `group.classDefinition()` too, tagged with a
   `UnitKind` (Program | Class). Each class unit → its own `SemanticModel` + `IrModule` (an `IrClassType`), emitted
   into the shared assembly. Multi-unit files already work.
4. **Symbols:** add `ClassSymbol` (name, InheritsName/resolved base, instance-data scope, factory-data scope, method
   roster, IsAbstract/IsFinal) and `MethodSymbol` (owning class, IsStatic/IsFinal/IsOverride, params, returning,
   local scope). Deferred INHERITS resolution (like REDEFINES). `ScopeKind` += ObjectData/FactoryData/MethodData.

## 4. Key architectural decision — per-instance `ProgramState` (maximal reuse)

**Each emitted class carries an instance `ProgramState State` field** (the same type the program uses as a static).
The constructor (`NEW`) allocates it and runs the class's `InitializeState` (VALUE clauses, etc.); instance methods
read/write `this.State`. This means **the entire byte + typed data engine works per-instance unchanged** — the only
delta is that, inside an instance method, the location emitter loads `ldarg.0; ldfld State` instead of the static
`ldsfld State`. So OBJECT data classifies (REDEFINES/refmod/typed flips) exactly as program data (ADR §7), and the
whole `PicRuntime`/`StorageHelpers`/typed-field machinery is reused with one addressing tweak. Factory (static) data
→ static `State` on the class (or static fields). Object **identity** is the .NET reference itself (GC-managed) —
no handle table.

- **OBJECT REFERENCE field** → a .NET field typed as the class (slice 1 can store `object`/the concrete class).
  `SET o TO NULL` → null store; `o = NULL` / `o = q` → reference compare (the `ManagedPointer` NULL/compare *shape*,
  but on a real object reference). Reuse the pointer-field-pass *pattern*, not the `ManagedPointer` type.
- **INVOKE** → `BoundInvokeStatement` → `IrInvoke` → `newobj`(NEW) / `callvirt`(instance) / `call`(static), mirroring
  `EmitCallProgram`'s structure but on the emitted class's `MethodDefinition` (resolve method by name+arity).
- **Method params/RETURNING:** USING args BY REFERENCE → `ManagedPointer` (reuse the CALL ABI) or, simpler for
  slice 1, by-value primitives; RETURNING → the method's .NET return value. Start minimal (no args), widen in slices.

## 5. Staged slices (each guard-green + a `tests/conformance/2002/` test)

1. **Class + instance method + NEW + no-arg INVOKE** (the §1 MVP). Lands ALL of §3 (grammar/tokens/pipeline/symbols)
   + the per-instance `ProgramState` emission + `EmitClassType`/instance method emission + `BoundInvokeStatement`/
   `IrInvoke` (newobj + callvirt, no args) + OBJECT REFERENCE storage + `SET o TO NULL`. Largest slice (the
   scaffolding). Conformance: `oo_hello.cob` → `HELLO, WORLD!`.
2. **INVOKE … USING / RETURNING** — method parameters + return value (reuse the CALL `ManagedPointer` ABI or by-value
   primitives). Conformance: a method that takes an arg and returns a value.
3. **INHERITS FROM + SELF/SUPER** — base class (`: Bar`), `this`/`base` dispatch, inherited data/methods.
   Conformance: a subclass overriding + calling SUPER.
4. **FACTORY (static) methods + factory data**, `INVOKE Class "M"` static dispatch. Conformance: a factory method.
5. **PROPERTY (GET/SET)** → C# properties. Conformance: a property round-trip.
6. **Polymorphism + universal object reference + `EC-OO-*`** (NULL/conformance exceptions, with the EC subsystem).
   Conformance: a polymorphic dispatch over a base reference.

## 6. Risks / decisions

| # | Risk | Mitigation |
|---|---|---|
| 1 | Grammar ambiguity: `classUnit` vs `programUnit` (both start IDENTIFICATION DIVISION) | Disambiguate on the id-paragraph (`CLASS-ID` vs `PROGRAM-ID`); ANTLR adaptivePredict on the paragraph keyword. |
| 2 | Per-instance `ProgramState` addressing — instance method must load `this.State` not static `State` | A per-method "instance context" flag in the emitter selects `ldarg.0; ldfld State` vs `ldsfld State`; one chokepoint in `CilLocationEmitter`'s backing-array load. |
| 3 | Method resolution (name→`MethodDefinition`) across compilation units in one assembly | Two-pass emit: define all class types + method signatures first, bind bodies second (mirrors the existing paragraph two-pass). |
| 4 | Object data that's byte-backed vs typed (the §3 classifier) | Reuse the classifier unchanged — OBJECT data is just another `ProgramState`; the per-instance state holds both byte areas and (later) typed instance fields. |
| 5 | NIST/Default dialect must be unaffected | Every OO rule is `{is2002()}?`-gated; OO keywords are corpus-clean. NIST stays byte-identical (verify with guard-fast + verdict-diff). |
| 6 | Scope creep (interfaces, parameterized classes, ACTIVE-CLASS) | Out of scope until slice 6+; slices 1–5 cover the 90% path. |

## 6.5 Findings from the first grammar attempt (DEVLOG 438 follow-up — read before retrying slice 1)

A first slice-1 grammar pass was prototyped and **validated to parse the full §1 OO program correctly** (CLASS-ID /
OBJECT / `METHOD-ID … END METHOD` / `USAGE OBJECT REFERENCE class` / `INVOKE … "m" … RETURNING`) — then **reverted**
because integrating those rules into the shared `CobolParserCore` caused **deterministic LL regressions** in ~9
class-condition-heavy tests (NC174A/211A/225A/250A compile-failed at `IS [NOT] NUMERIC` constructs, + 5 integration).
Concrete lessons for the retry:

1. **Add the OO grammar rules INCREMENTALLY, running `scripts/guard-fast.sh` after EACH addition** (not all at once),
   so the regressing rule is pinpointed immediately. The all-at-once approach hid which of {`(programUnit |
   classDefinition)+`, the class/method rules, `OBJECT REFERENCE className?` in `usageKeyword`, moving
   `invokeStatement` into the main grammar} caused the break. Prime suspects: the top-level
   `(programUnit | classDefinition)+` alternation (both start `IDENTIFICATION DIVISION.` — needs left-factoring so
   the program-vs-class decision is made on the id-paragraph, not via SLL bail→LL) and the **optional trailing**
   `className?` after `REFERENCE` (a classic ANTLR optional-tail ambiguity — make it non-optional or
   left-factor a distinct `objectReferenceUsage` rule).
2. **`invokeStatement` was a red herring twice over:** (a) the `{is2002()}?` gate works fine on an imported-grammar
   rule (ALLOCATE proves it); (b) "unexpected INVOKE" in my minimal tests was because the **test programs lacked a
   paragraph header** — statements directly under `PROCEDURE DIVISION.` make the parser read the verb as a paragraph
   *name*. **All OO conformance test programs MUST have a `MAIN.` (and per-method) paragraph header.** A clean ANTLR
   regen requires touching `CobolParserCore.g4` (the build's timestamp check) — a stale partial generate shows as a
   spurious `CS1513` in the generated `.cs`.
3. The validated grammar SHAPE is correct (matches ISO §12739 composition): `classDefinition : (IDENTIFICATION
   DIVISION DOT)? classIdParagraph environmentDivision? objectParagraph? endClassHeader`; `objectParagraph :
   (IDENTIFICATION DIVISION DOT)? OBJECT DOT … dataDivision? (PROCEDURE DIVISION DOT methodDefinition*)? END OBJECT
   DOT`; `methodDefinition : … METHOD_ID DOT methodName DOT … procedureDivision? END METHOD methodName? DOT`. Tokens
   `CLASS`/`END`/`METHOD`/`REFERENCE`/`CLASS_ID`/`METHOD_ID`/`INVOKE` exist; only `OBJECT` (and later `FACTORY`/
   `INHERITS`) must be added (corpus-clean; longest-match keeps `OBJECT-COMPUTER` intact).

## 6.6 Slice-1 semantic/emit — turnkey implementation map (DEVLOG 439 follow-up, investigation done)

The grammar is landed (Core/CobolOO.g4, gated, green). The semantic/emit is a multi-hour vertical; this is the exact
plan from the investigation so the next push is zero-rediscovery. **Key reuse finding:** route a `classDefinition`
through the existing `BuildSemanticModel` (it just calls `semanticBuilder.Visit(tree)`, an ANTLR visitor that
recurses into children) → the OBJECT data is collected as WORKING-STORAGE and the method's paragraphs via recursion,
so the **whole semantic + binder layer is reused** and a single-method class compiles to a normal `IrModule`
(`ProgramSymbol`-based) — **no new ClassSymbol needed for slice 1**. The class-specific work is only emission + INVOKE.

**A. Routing (low-risk, additive) — `Compilation.cs`:**
- `CollectProgramContexts` (`:168`): also `foreach (group.classDefinition()) { result.Add(c); parents[c]=null; }`.
- `ExtractProgramIdFromContext` (`:660`): add `CobolParserCore.ClassDefinitionContext c => c.classIdParagraph()`
  → `className()` for the unit name.
- Phase-4 loop (`:103`): tag each `CompiledProgram` with a `UnitKind` (Program | Class) so emission branches. The
  class's `objectParagraph.dataDivision()` + each `methodDefinition.procedureDivision()` are visited by recursion;
  the bare `PROCEDURE DIVISION.` tokens in `objectParagraph` are harmless (no visit method). Verify the method's
  paragraph(s) land in the procedure model.

**B. Class emission — `CilEmitter` (the invasive part):** add `EmitClassModule` parallel to `EmitModule`:
- The `State` field becomes an INSTANCE field; `InitializeState` becomes an instance method; the `NEW` ctor allocates
  `this.State` + calls instance `InitializeState`. The paragraph `IrMethod`s + the `Dispatch` helper become INSTANCE
  methods. Each COBOL method (`METHOD-ID`) → a `public` instance method that runs the dispatch from that method's
  entry paragraph index.
- **The State-load chokepoint:** add `EmissionContext.StateIsInstance`. In `CilLocationEmitter:476` (and
  `CilEmitter:369/730/883`) emit `ldarg.0; ldfld State` when `StateIsInstance`, else `ldsfld State`. This is the one
  byte-engine change that makes the entire data engine work per-instance. Object **identity** = the .NET reference.

**C. INVOKE — bind + emit (cross-type resolution):**
- Bind `invokeStatement` (BoundTreeBuilder dispatch → a new `BoundInvokeStatement{ Target, MethodName, Args,
  Returning, IsNew }`). The reference resolver must NOT flag the class-name operand (GREETER) as CBL3128 — recognize
  an INVOKE class-name target. `INVOKE class "NEW"` → IsNew.
- Lower → `IrInvoke`. Emit: `IsNew` → `newobj <class>..ctor` → store RETURNING (an object-ref field); instance →
  `ldsfld <objref>; castclass <class>; callvirt <class>::<method>`. **Cross-type resolution:** the target class is a
  *separate* emitted type/`IrModule` in the same assembly → emit needs a two-pass (define all class types + method
  signatures first, bind bodies/INVOKE second) OR a post-emit fixup, resolving `TypeDefinition`/`MethodDefinition`
  by class-name+method-name across modules (a shared registry on the assembly emit).

**D. OBJECT REFERENCE storage:** an `01 G USAGE OBJECT REFERENCE C` item → a `static <C-or-object>` reference field
(mirror the pointer-field pass shape, but a real .NET reference, not `ManagedPointer`). `SET o TO NULL` → `ldnull;
stsfld`; `o = NULL`/`o = q` → reference compare; `INVOKE … RETURNING o` stores into it.

**Conformance:** `tests/conformance/2002/_pending_oo/oo_hello.cob` (already authored + parse-validated) → `HELLO,
WORLD!`. Move it into the corpus in the same commit the vertical goes green. Then slices 2–6 (§5).

## 6.7 Slice 2 — INVOKE USING/RETURNING (method params + return) — DONE (DEVLOG 448)

**Implemented as described below** — every gap is closed: OO methods take `ManagedPointer[] args` (the CALL ABI),
`CreateLinkageFields` is shared, `EmitMapArgsToLinkage` binds args→LINKAGE, and `EmitInvoke` marshals USING args +
the trailing RETURNING pointer. Conformance `oo_method_args` proves per-instance independence. The original gap
analysis is kept below for provenance and as the model for the same pattern in later slices.

**Scope (adversarial review, DEVLOG 449):** slice 2 supports **BY REFERENCE data-reference** USING args only. The
other grammar-legal forms — a bare `literal`, `BY VALUE arithmetic-expression`, `BY CONTENT` — need a synthesized
value location (a scratch buffer for the copy), which `ResolveExpressionLocation` does not yet provide (the same gap
the CALL path has); they are a later slice. `CallBinder.BindInvoke` REJECTS them with **`COBOL0111` (Error)** rather
than silently dropping the arg (a dropped arg would shift the trailing RETURNING slot and miscompile). When a later
slice adds value-arg support, route INVOKE args through the CALL `IrCallArgument` (Mode + Source) machinery + a
literal/expression scratch-location synthesizer, and lift the COBOL0111 guard. Regression net:
`tests/CobolSharp.Tests.Integration/OoTests.cs`.

Slice 1 landed (DEVLOG 447). A slice-2 probe (a class method with `PROCEDURE DIVISION USING LK-AMT RETURNING
LK-RES`, invoked `INVOKE A "ADDTO" USING AMT RETURNING R`) **compiles** — the grammar + binder already carry a
method's USING/RETURNING, and the method body's LINKAGE access already emits — but **crashes at runtime**
(`DecodeNumeric bufferLength=0`): the emitted public method is `void ADDTO()` (no params), so the LINKAGE
`ManagedPointer` fields are never populated. The gaps are precise and reuse-heavy:

1. **Unify the OO method ABI to the CALL/Entry ABI: `void <Method>(ManagedPointer[] args)`** (instance). A no-arg
   method (slice 1's SAYHELLO) takes an empty array. This changes slice-1's emit — update `EmitClassMethodBody` →
   `EmitClassMethod` so EVERY OO method takes `ManagedPointer[] args`, and slice-1 INVOKE passes `Array.Empty`.
2. **Create the LINKAGE fields for a class** — `EmitClassModule` currently SKIPS `CreateEntryMethodSignature`
   (CilEmitter), so `_linkageFields` is empty and `FindLinkageField` returns null (→ the empty-buffer crash). Factor
   the LINKAGE-field-creation loop out of `CreateEntryMethodSignature` into a `CreateLinkageFields(ir)` helper and
   call it from `EmitClassModule`. (`module.UsingParameterNames` is ALREADY populated for a class — Binder
   PopulateModuleMetadata runs for all units — and it INCLUDES the RETURNING name as the last entry, so RETURNING is
   just the trailing LINKAGE param, exactly as in CALL.)
3. **Public method body = an instance "Entry"**: map `args[i] → _linkage[UsingParameterNames[i]]` (reuse the mapping
   loop from `EmitEntryMethodBody`, minus the FileRuntime.Init / reinit / INITIAL / RegisterFiles program-activation
   logic), then run `this.Dispatch(entry, -1)`. The RETURNING param is mapped like any other (the method writes
   LK-RES; the caller reads it back through the trailing BY-REFERENCE pointer).
4. **INVOKE marshalling** — extend `BoundInvokeStatement`/`IrInvoke` to carry the USING arg IrLocations + the
   RETURNING IrLocation (mirror `BoundCallStatement`/`IrCallProgram`'s `IrCallArgument` + ReturningTarget); resolve
   them in `BindInvoke`/`LowerInvoke`; in `EmitInvoke` marshal them into a `ManagedPointer[]` exactly like
   `EmitCallProgram` (BY REFERENCE → `ManagedPointer.CreateByReference`, RETURNING as a trailing BY-REFERENCE
   pointer), then `ldsfld receiver; ldloc args; callvirt <class>::<Method>(ManagedPointer[])`. NEW stays `newobj
   ..ctor` (the ctor takes no args in slice 2; constructor parameters are a later slice).

This makes per-instance state observable (a setter/getter method mutating OBJECT data) — author that as the slice-2
conformance test. Slice-1's `oo_hello`/`oo_method_perform` must stay green under the new (empty-args) ABI.

## 7. Definition of done (this subsystem)

A representative OO program — a class with instance + factory methods, inheritance with SELF/SUPER, a property,
invoked polymorphically from a driver program — compiles to real .NET classes (debuggable as `g.SayHello()`),
runs correctly, ships conformance tests, and the full guard (`guard-fast`, verdict-identical to serial) is green at
every commit. The Roslyn C# backend (Stage 5) later emits these classes as steppable `.cs`.

---

# Appendix — Target-design surface

> These sections describe the *eventual* full OO feature set (slices 5–6+ and beyond), not slice-1 scope. They reflect
> CURRENT TRUTH (CIL-only via Mono.Cecil; .NET 10 / C# 14; object identity = GC reference, no handle table). The
> §0–§7 turnkey roadmap above is authoritative where it overlaps.

## §A. The full ISO/IEC 1989:2023 OO surface (target catalog)

CobolSharp targets the complete COBOL OO model, mapped directly onto .NET:

| COBOL OO construct | .NET mapping | Slice |
|---|---|---|
| `CLASS-ID. Foo.` | `public class Foo` | 1 |
| `FACTORY.` section | static members / static methods / class-level utilities | 4 |
| `OBJECT.` section | instance members / instance methods / object state | 1 |
| `END CLASS` / `END FACTORY` / `END OBJECT` | scope terminators | 1 |
| `METHOD-ID. M.` | `public [virtual]` .NET method (FACTORY→static, OBJECT→instance) | 1 |
| `INVOKE obj "M" USING … RETURNING …` | `callvirt` (instance) / `call` (static) | 1–2 |
| `INVOKE Class "NEW" RETURNING o` | `newobj Class::.ctor` | 1 |
| `INHERITS FROM Base` (a.k.a. `EXTENDS`) | `: Base` (single inheritance) | 3 |
| `IMPLEMENTS I1 I2` | `: I1, I2` (multiple interfaces allowed) | 6+ |
| `SELF` / `SUPER` | `this` / `base` | 3 |
| `PROPERTY` (GET/SET) | C# property `T Foo { get; set; }` | 5 |
| `PROPERTY … USING index` | C# indexer `this[int index]` | 6+ |
| `USAGE OBJECT REFERENCE Class` / universal | a `Class` reference field / `object?` | 1 |
| `OVERRIDE` / `FINAL` / `ABSTRACT` | `override` / `sealed override` / `abstract` | 3 / 3 / 6+ |
| `PUBLIC` / `PRIVATE` / `PROTECTED` | `public` / `private` / `protected` (default = `public`) | 6+ |
| `obj IS Class` / `obj IS NOT Class` | `obj is Class` | 6+ |
| `.NET TYPE` reference (`01 dt TYPE DateTime`) | `System.DateTime dt` (interop) | 6+ |

ISO references: §11.3/§11.4/§11.7/§11.8 (class/method/factory/object), §14.9.23 (INVOKE), §8.4.3.8 (SELF/SUPER),
§16.2.1 (NEW). OO execution must remain deterministic, pure-managed, and AOT/WASM-safe.

## §B. Properties and indexers (slice 5 / 6+)

A COBOL `PROPERTY` lowers to a C# property. Two source forms:

- **Inline declaration:** `PROPERTY-ID. Value GET SET.` → `public T Value { get; set; }` (auto-property when no
  explicit GET/SET bodies). The getter lowers to `get_Value`, the setter to `set_Value`.
- **Explicit GET/SET bodies:**
  ```cobol
  PROPERTY-ID. Value.
      GET.  ... END GET.
      SET.  ... END SET.
  END PROPERTY.
  ```
  → `public T Value { get { … } set { … } }`.
- **INVOKE-as-property access:** `INVOKE obj "Property"` (no args) → getter; `INVOKE obj "Property=" USING value` →
  setter. (This is the property-via-INVOKE spelling; direct `obj::Property` access is the other.)
- **Indexers:** `PROPERTY-ID. Item USING index GET SET.` → `public T this[int index] { get; set; }`.

Edge cases: a `PROPERTY` over `OCCURS` is illegal unless explicitly indexed; SET on a read-only (GET-only) property
and GET on a write-only (SET-only) property are compile-time errors.

## §C. .NET interop from OO COBOL (slice 6+ / interop subsystem)

Note: the dedicated interop architecture lives in the INTEROP cluster; the OO-relevant slice is summarized here.

- **INVOKE on any .NET object:** `INVOKE obj "ToString"` works for any .NET object as well as any COBOL object —
  the dispatch is uniform `callvirt`.
- **Declaring a .NET-typed item:** `01 dt TYPE DateTime.` → `System.DateTime dt;`.
- **Constructing a .NET object:** `INVOKE TYPE DateTime "NEW" USING args RETURNING dt` → `dt = new DateTime(args)`.
- **AOT/WASM safety:** INVOKE must use static or virtual calls only — **no `Type.GetType`, no reflection, no dynamic
  codegen**. All method dispatch is resolved statically at emit time so virtual dispatch is byte-identical across
  CoreCLR, AOT, and WASM.

## §D. Inheritance, overriding, polymorphism (slice 3)

- **Single inheritance only:** `INHERITS FROM Base` / `EXTENDS Base` → `: Base`. Multiple base classes are illegal
  (COBOL supports single inheritance); multiple `IMPLEMENTS` interfaces are allowed.
- **`OVERRIDE`:** `METHOD-ID. Foo OVERRIDE.` → `public override Foo(…)`. Overriding a method with no matching base
  method is a compile-time error.
- **`FINAL`:** `METHOD-ID. Foo FINAL.` → `sealed override Foo(…)`. Overriding a FINAL method is a compile-time error.
- **`ABSTRACT`:** an abstract class cannot be instantiated — `INVOKE AbstractClass "NEW"` is a compile-time error.
- **SUPER:** `INVOKE SUPER "Foo"` → `base.Foo(…)`.
- **Polymorphism:** `INVOKE obj "Foo"` over a base-typed reference is virtual dispatch (`callvirt`), identical across
  platforms.

## §E. Object references, NULL, lifetime (slices 1, 6)

- **Reference type:** `01 obj USAGE OBJECT REFERENCE ClassName.` → a `ClassName obj;` field (slice 1 may store as
  `object`/concrete class).
- **NULL handling:** `SET o TO NULL` → `o = null`; `IF o = NULL` / `o = q` → reference comparison. This reuses the
  *shape* of the pointer NULL/compare machinery, but operates on a real .NET reference (NOT a `ManagedPointer`).
- **Type checking:** `obj IS ClassName` / `obj IS NOT ClassName` → `obj is ClassName`.
- **Lifetime:** objects are GC-managed; no explicit disposal unless the class implements `IDisposable`. Object
  identity is the .NET reference itself — **no handle table, no native heap, no `unsafe`**.

## §F. Edge-case behavior catalog

| Case | Behavior |
|---|---|
| INVOKE on a null object | Runtime exception → routed to `ON EXCEPTION` if present (later: `EC-OO-*`, slice 6). The diagnostic surface is `OBJECT-REFERENCE-NOT-SET`. |
| RETURNING on a void method | Compile-time error |
| Missing / unknown method | Compile-time error |
| INVOKE with wrong parameter count | Compile-time error |
| Overloaded methods | Resolved by parameter count/type |
| `NEW` on an abstract class | Compile-time error |
| OVERRIDE without a matching base method | Compile-time error |
| Overriding a FINAL method | Compile-time error |
| SET on a read-only property / GET on a write-only property | Compile-time error |
| `METHOD-ID` inside DECLARATIVES | Illegal |
| `PROPERTY` over OCCURS | Illegal unless explicitly indexed |
| `INHERITS` with multiple bases | Illegal (single inheritance) |
| `IMPLEMENTS` with multiple interfaces | Allowed |

## §G. Debugger integration (design-only, Phase E)

The OO debugger surface (cross-references the DEBUGGER cluster, which is design-only). When OO emission lands,
sequence points are emitted at: METHOD-ID entry, INVOKE, RETURNING, and PROPERTY GET/SET. The debugger should
surface: class name, method name, the `SELF`/THIS object, parameters, RETURNING value, FACTORY-vs-OBJECT members,
static vs instance fields, the inheritance hierarchy, the virtual-dispatch target, the OO call stack, and INVOKE
targets. (No work until the OO vertical is emitting and the debugger subsystem is built.)
