# OO COBOL → .NET classes — Implementation Design (Stage-4 OO)

**Status:** Design (kickoff). Parent: `docs/DATA_MODEL_ARCHITECTURE.md` §7 (the settled OO→.NET mapping) and
`docs/ISO2023_CONFORMANCE_PLAN.md` §0.5 (Stage-4, after pointers). Provenance: 3-agent investigation workflow
`oo-investigation` (2026-06-07) + first-hand reading of ISO/IEC 1989:2023 §11.3/§11.4/§11.7/§11.8, §14.9.23 (INVOKE),
§8.4.3.8 (SELF/SUPER), §16.2.1 (NEW).

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

## 7. Definition of done (this subsystem)

A representative OO program — a class with instance + factory methods, inheritance with SELF/SUPER, a property,
invoked polymorphically from a driver program — compiles to real .NET classes (debuggable as `g.SayHello()`),
runs correctly, ships conformance tests, and the full guard (`guard-fast`, verdict-identical to serial) is green at
every commit. The Roslyn C# backend (Stage 5) later emits these classes as steppable `.cs`.
