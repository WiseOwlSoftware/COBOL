CobolSharp COBOL Interop Architecture — .NET Types, Assemblies, INVOKE .NET & Type Mapping (CIL‑Only)
=====================================================================================================

> **STATUS BANNER (Consolidated from 2 prior docs, 2026-06-07).**
> This is a **TARGET design reference** for COBOL ↔ .NET interop. **Implementation status: DESIGN-ONLY.**
> The `invokeStatement` / OO **grammar exists and is green** (factored into `src/CobolSharp.Compiler/Grammar/Core/CobolOO.g4`,
> `{is2002()}?`-gated; INVOKE/END-INVOKE lexer tokens present). **There is NO interop runtime yet** — no
> `Interop*`/`*Marshaler` classes exist in `src/`, and OO/INVOKE **emit (semantic → binder → CIL) is pending** (the
> active OO migration slices: emit .NET class + instance methods, then INVOKE newobj/callvirt). Treat every lowering /
> marshaling / exception-translation section below as a specification to build against, not a record of what runs today.
>
> **Stack: .NET 10 / C# 14.** **Backend: CIL-only via Mono.Cecil** (the Roslyn C# backend is a FUTURE additive Stage-5;
> Cecil is the oracle). **No custom VM, no bytecode interpreter.** Data model is migrating to typed-native (CORE done
> through Stage-4; `EnableTypedFields` default OFF; the byte engine is being islanded). **Pointers are ONE
> `ManagedPointer`** (no 8-byte handle, no PointerRegistry).
>
> Plan SSOT: **`docs/MASTER_PLAN.md`**; doctrine: **`PROMPT.md`**. The OO target design is `docs/OO_IMPLEMENTATION_DESIGN.md`.
> The COBOL-to-C# Interop Cookbook (`docs/CobolSharp COBOL-to-C# Interop Cookbook.md`) is a separate singleton and is
> NOT merged here.

Purpose
-------
Define the authoritative **target** architecture for:
- INVOKE of .NET methods, properties, constructors
- Mapping COBOL types to .NET types
- Mapping .NET types to COBOL types
- Assembly loading and binding
- Static vs instance dispatch
- Generic method invocation
- Exception translation
- Object references in COBOL
- AOT/WASM‑safe interop
- CIL‑friendly lowering
- **.NET → COBOL** direction (COBOL-generated assemblies consumed from C#/F#/VB)

This document governs how CobolSharp integrates COBOL programs with .NET assemblies and runtime types.

------------------------------------------------------------
SECTION 1 — INTEROP OVERVIEW
------------------------------------------------------------

CobolSharp interop is built on three pillars:

1. **COBOL → .NET** — COBOL code calling into .NET assemblies.
2. **.NET → COBOL** — .NET code calling into COBOL‑generated assemblies (see Section 13).
3. **Shared Type System** — a unified mapping between COBOL data descriptions and .NET types (Sections 3–4).

CobolSharp supports (COBOL → .NET):
- INVOKE typeName::Method USING …
- INVOKE objectRef Method USING …
- INVOKE typeName NEW RETURNING obj
- Access to .NET properties and fields
- Passing COBOL values to .NET
- Receiving .NET values into COBOL
- Exception translation to COBOL ExceptionState

Interop is:
- Deterministic
- Pure managed
- AOT/WASM‑safe
- No reflection at runtime
- Verifiable, CIL‑only (no VM, no custom runtime)

High‑level goals:
- Provide seamless, type‑safe interoperability between COBOL compiled by CobolSharp and .NET languages (C#, F#, VB).
- Enable COBOL programs to call .NET methods, instantiate .NET classes, consume .NET libraries, implement .NET
  interfaces, and expose COBOL classes/methods to .NET callers.
- Preserve COBOL semantics while enabling modern .NET integration.
- Work across CoreCLR, .NET AOT, and WASM (via `dotnet publish`).

------------------------------------------------------------
SECTION 2 — ASSEMBLY & TYPE RESOLUTION
------------------------------------------------------------

2.1 Compile‑time resolution
---------------------------
CobolSharp resolves at compile time:
- Assembly references
- Type names
- Method signatures
- Property signatures

2.2 No runtime reflection
-------------------------
At runtime:
- No Type.GetType
- No MethodInfo.Invoke
- No dynamic binding

All calls are static or virtual CIL calls.

2.3 Fully qualified names
-------------------------
    INVOKE System.Console::WriteLine
    INVOKE System.Text.StringBuilder NEW

Compiler resolves namespace, type, method overload, and parameter types.

------------------------------------------------------------
SECTION 3 — COBOL → .NET TYPE MAPPING
------------------------------------------------------------

3.1 Elementary types
--------------------
- DISPLAY (alphanumeric) → string
- NATIONAL → string (UTF‑16)
- COMP / COMP‑5 → int / long (per USAGE/size)
- COMP‑3 (packed decimal) → decimal
- DISPLAY numeric → decimal (scaled/rounded per PIC)
- PIC 9(n) → int / long / decimal depending on size
- Boolean → bool
- Object reference → object

> Note: the live data-model migration types these natively (character → `string`, numeric → `long`/`decimal`).
> The legacy byte image remains only as a classifier-scoped fallback. Group/REDEFINES marshaling MUST go through the
> typed-native model and the runtime marshaling helpers (Section 11) — **NOT** raw `StructLayout`/`FieldOffset`
> reinterpretation, which is incompatible with the typed-native record-struct model.

3.2 Group items
---------------
Group items map to a generated .NET record struct / class projecting the group's typed members (one member per
elementary item), with marshaling metadata. Group items that must remain a raw buffer (byte-trigger cases:
REDEFINES/RENAMES/edited/ref-mod/ODO/file/EXTERNAL/LINKAGE) marshal as `byte[]`.

3.3 OCCURS tables
-----------------
- Fixed OCCURS → `T[]` arrays
- Annotated / dynamic → `List<T>`

3.4 88-level condition-names
----------------------------
Map to enum‑like constants / predicate helpers.

3.5 NULL handling
-----------------
COBOL has no null:
- Object references default to null
- DISPLAY/NATIONAL default to spaces
- Numeric defaults to zero

------------------------------------------------------------
SECTION 4 — .NET → COBOL TYPE MAPPING
------------------------------------------------------------

4.1 string → DISPLAY/NATIONAL
-----------------------------
- DISPLAY: ASCII only; non‑ASCII → runtime error.
- NATIONAL: UTF‑16 copied directly.

4.2 int/long → numeric
----------------------
Converted to decimal then to PIC.

4.3 decimal → numeric
---------------------
Scaled and rounded per PIC.

4.4 bool → DISPLAY
-------------------
"TRUE" / "FALSE".

4.5 object → COBOL object reference
-----------------------------------
Stored as a reference in `ExecutionContext.ObjectTable`.

------------------------------------------------------------
SECTION 5 — INVOKE ARCHITECTURE
------------------------------------------------------------

The canonical surface is the `INVOKE` statement (grammar: `Core/CobolOO.g4`,
`INVOKE invokeTarget invokeMethodName invokeUsing? invokeReturning? END-INVOKE?`). The target is a class-name or an
object reference (`dataReference | NULL | SELF | …`).

5.1 Static method
-----------------
    INVOKE System.Math::Sqrt USING x RETURNING y.
Lowering: `y = System.Math.Sqrt(x)`

5.2 Instance method
-------------------
    INVOKE sb Append USING "Hello".
Lowering: `sb.Append("Hello")`

5.3 Constructor
---------------
    INVOKE System.Text.StringBuilder NEW RETURNING sb.
Lowering: `sb = new StringBuilder()`

5.4 Property GET
----------------
    INVOKE obj::Length RETURNING len.
Lowering: `len = obj.Length`

5.5 Property SET
----------------
    INVOKE obj::Capacity = 100.
Lowering: `obj.Capacity = 100`

5.6 Field access
----------------
    INVOKE obj::SomeField RETURNING v.
Lowering: `v = obj.SomeField`

------------------------------------------------------------
SECTION 6 — METHOD OVERLOAD RESOLUTION
------------------------------------------------------------

6.1 Compile‑time resolution
---------------------------
Compiler selects overload based on number of USING parameters, COBOL → .NET type mapping, and best‑match rules.

6.2 Ambiguous overloads
-----------------------
Compile‑time error.

6.3 Generic methods
-------------------
Supported only if type arguments are inferred from parameters, or explicitly specified:
    INVOKE Type::Method<T> USING …
(See `CobolParserGenerics.g4`: `INVOKE MyList::AddItem<INTEGER> USING 5.`) A generic method with no inference and no
explicit type argument is a compile‑time error (Section 12.6).

------------------------------------------------------------
SECTION 7 — PARAMETER PASSING RULES
------------------------------------------------------------

- **BY VALUE** → .NET value parameter.
- **BY REFERENCE** → ref/out parameter (`ref` for input/output, `out` for output‑only).
- **BY CONTENT** → .NET value parameter (copy).
- **Object references** → passed as a .NET object reference.

------------------------------------------------------------
SECTION 8 — EXCEPTION TRANSLATION
------------------------------------------------------------

8.1 .NET exception → COBOL exception
------------------------------------
Caught and translated to `ExceptionState` and surfaced to an `ON EXCEPTION` block.

8.2 ExceptionState fields
-------------------------
- ExceptionType
- Message
- StackTrace (optional)
- TargetMethod

8.3 Rethrow behavior
--------------------
If no `ON EXCEPTION`: `ExceptionState` is set and execution continues.

8.4 .NET exception → COBOL condition mapping (target table)
----------------------------------------------------------
| .NET exception              | COBOL surface                              |
|-----------------------------|--------------------------------------------|
| ArgumentException           | ON EXCEPTION                               |
| InvalidOperationException   | ON EXCEPTION                               |
| FileNotFoundException       | INVALID KEY (if a file operation) else ON EXCEPTION |
| JsonException               | ON EXCEPTION                               |
| XmlException                | ON EXCEPTION                               |

COBOL exceptions map to .NET exceptions when thrown across the boundary.

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

| COBOL operation     | CIL                          |
|---------------------|------------------------------|
| Static call         | `call Type::Method`          |
| Virtual call        | `callvirt obj::Method`       |
| Constructor         | `newobj Type::.ctor`         |
| Property GET        | `callvirt get_Property`      |
| Property SET        | `callvirt set_Property`      |
| ref/out parameters  | `ldloca.s temp` … `call Method` |

All emitted via Mono.Cecil. (The future Roslyn C# backend, Stage-5, will emit equivalent C#; Cecil remains the oracle.)

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

The debugger shows: .NET object references, method parameters, return values, property values, ExceptionState,
type names, and assembly names.

------------------------------------------------------------
SECTION 11 — INTEROP MARSHALING ENGINE  (target — not yet built)
------------------------------------------------------------

`CobolSharp.Runtime` will provide marshaling helpers:
- StringMarshaler
- NumericMarshaler
- PackedDecimalMarshaler
- ArrayMarshaler
- RecordMarshaler
- ObjectMarshaler

Responsibilities:
- Convert COBOL values to .NET values (and back to COBOL storage).
- Handle REDEFINES overlays (no flattening unless safe).
- Handle OCCURS DEPENDING ON dynamic bounds.
- Handle nullable values (optional).

> Status: NO `*Marshaler` types exist in `src/` yet. This is the contract to implement during the OO/interop emit stage.

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

- **INVOKE on null object** → runtime error `OBJECT-REFERENCE-NOT-SET`.
- **Passing NATIONAL to .NET** → converted to UTF‑16 string.
- **Passing DISPLAY with non‑ASCII** → runtime error.
- **Returning null to DISPLAY** → converted to spaces.
- **Returning null to object reference** → stored as null.
- **Generic method with no inference** → compile‑time error.

------------------------------------------------------------
SECTION 13 — .NET → COBOL INTEROP (COBOL assemblies consumed from .NET)
------------------------------------------------------------

COBOL programs compiled by CobolSharp produce standard .NET assemblies.

COBOL classes become:
- Public .NET classes, with public methods, and public fields/properties (if declared as such).
- Typed-native projection of the data divisions (see Section 3 — NOT raw explicit-layout reinterpretation).

COBOL methods become:
- Public instance or static methods with .NET‑compatible signatures and marshaling metadata for parameters.

COBOL programs become:
- A class with a `Main`‑like entry point, callable from C# (e.g. `CobolProgram.Main(args)`).

COBOL OO classes:
- Map directly to .NET classes; can implement .NET interfaces; can (optionally) inherit from .NET base classes; can be
  instantiated from C#.

Example C# usage (target):
    var cust = new CobolSharp.Customer();
    cust.SetName("Alice");
    cust.ProcessOrder();

------------------------------------------------------------
SECTION 14 — INTEROP CODE GENERATION (target)
------------------------------------------------------------

CobolSharp will generate:
- C# wrappers for COBOL classes (optional)
- COBOL CALL/INVOKE stubs for .NET methods
- Marshaling metadata
- Interop helper classes

Example generated stub (target):
    public static class CobolInterop_MyLib_Math {
        public static int Add(int a, int b) => MyLib.Math.Add(a, b);
    }

------------------------------------------------------------
SECTION 15 — INTEROP SAFETY RULES
------------------------------------------------------------

To ensure predictable behavior:
- No implicit marshaling of unsupported types.
- No automatic conversion of complex .NET objects without metadata.
- No inheritance from COBOL classes unless explicitly allowed.
- No REDEFINES flattening unless safe.
- No automatic async → sync bridging without runtime helpers.

------------------------------------------------------------
SECTION 16 — AOT/WASM‑SAFE INTEROP
------------------------------------------------------------

- **No reflection** — all calls static or virtual.
- **No dynamic codegen** — no IL emit at runtime.
- **Deterministic binding** — all types/methods resolved at compile time.
- **WASM interop** supported for: string, numeric types, boolean, arrays, simple objects.

------------------------------------------------------------
SECTION 17 — TOOLING & TESTING (target)
------------------------------------------------------------

Tooling integration:
- LSP (hover shows .NET signatures)
- Debugger (shows .NET objects and COBOL storage)
- Modernization toolkit (suggests interop boundaries)
- Data layout visualizer
- IL viewer

Testing strategy:
- Unit tests for marshaling
- Golden tests for generated stubs
- Integration tests calling .NET from COBOL
- Integration tests calling COBOL from C#
- Exception propagation tests
- AOT/WASM compatibility tests
- A conformance test in `tests/conformance/2002/` ships with each implemented OO/interop feature (per project doctrine).

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Interop Architecture (target):
- Provides full COBOL → .NET interop via INVOKE, NEW, properties, and fields, and .NET → COBOL consumption of
  COBOL‑generated assemblies.
- Maps COBOL types to .NET types deterministically through the typed-native data model + runtime marshaling helpers.
- Resolves assemblies, types, and methods at compile time (no runtime reflection).
- Generates clean, verifiable, AOT/WASM‑safe CIL via Mono.Cecil.
- Integrates with ExecutionContext and the ObjectTable, the debugger, LSP, and the modernization tooling.
