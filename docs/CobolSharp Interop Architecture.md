CobolSharp Interop Architecture (COBOL ↔ .NET, CIL‑Only)
========================================================

High‑level goals
----------------
- Provide seamless, type‑safe interoperability between COBOL compiled by CobolSharp and .NET languages (C#, F#, VB).
- Enable COBOL programs to:
  - Call .NET methods
  - Instantiate .NET classes
  - Consume .NET libraries
  - Implement .NET interfaces
  - Expose COBOL classes/methods to .NET callers
- Ensure all interop is:
  - Verifiable CIL
  - Marshaling‑safe
  - Predictable
  - Fully compatible with .NET AOT and WASM (via dotnet publish)
- Preserve COBOL semantics while enabling modern .NET integration.

Interop model overview
----------------------
CobolSharp interop is built on three pillars:

1. **COBOL → .NET Interop**  
   COBOL code calling into .NET assemblies.

2. **.NET → COBOL Interop**  
   .NET code calling into COBOL‑generated assemblies.

3. **Shared Type System**  
   A unified mapping between COBOL data descriptions and .NET types.

All interop is implemented in pure CIL — no VM, no custom runtime.

1. COBOL → .NET Interop
-----------------------
COBOL code can call .NET methods using the CALL syntax:

  CALL "Namespace.Class::Method" USING arg1 arg2

Supported forms:
- Static methods
- Instance methods
- Constructors
- Generic methods (with type inference or explicit types)
- Async methods (awaitable via runtime helpers)

Name resolution:
- If the CALL target contains "::", it is treated as a .NET method.
- Otherwise, it is treated as a COBOL program or entry point.

Examples:
- CALL "System.Console::WriteLine" USING "Hello"
- CALL "MyLib.Math::Add" USING A B GIVING RESULT
- CALL "MyLib.Customer::new" RETURNING CUST-OBJ

Object operations:
- INVOKE method-name ON object
- SET property-name OF object TO value
- GET property-name OF object GIVING value

COBOL syntax extensions:
- INVOKE object "Method" USING args
- INVOKE object::Method USING args
- INVOKE type::StaticMethod USING args

Marshaling rules:
- COBOL alphanumeric → System.String
- COBOL numeric → System.Decimal or System.Int32/Int64 depending on USAGE
- COBOL packed decimal → System.Decimal
- COBOL group items → generated .NET classes with explicit layout
- OCCURS → arrays or List<T>
- REDEFINES → overlapping fields in explicit layout classes

2. .NET → COBOL Interop
-----------------------
COBOL programs compiled by CobolSharp produce standard .NET assemblies.

COBOL classes become:
- Public .NET classes
- With public methods
- With public fields/properties (if declared as such)
- With explicit layout for data divisions

COBOL methods become:
- Public instance or static methods
- With .NET‑compatible signatures
- With marshaling metadata for parameters

COBOL programs become:
- Static classes with a Main‑like entry point
- Callable from C# via:
    CobolProgram.Main(args)

COBOL OO classes:
- Map directly to .NET classes
- Can implement .NET interfaces
- Can inherit from .NET base classes (optional)
- Can be instantiated from C#

Example C# usage:
  var cust = new CobolSharp.Customer();
  cust.SetName("Alice");
  cust.ProcessOrder();

3. Shared Type System
---------------------
CobolSharp defines a unified type mapping:

COBOL → .NET:
- PIC X(n) → string
- PIC 9(n) → int/long/decimal depending on size
- COMP‑3 → decimal
- COMP → int/long
- COMP‑5 → native int
- OCCURS → T[] or List<T>
- REDEFINES → explicit layout struct/class
- 88‑levels → enum‑like constants

Group items:
- Become .NET classes with:
  - Explicit layout
  - FieldOffset attributes
  - Marshaling metadata

Example:
01 CUSTOMER-RECORD.
   05 ID        PIC 9(5).
   05 NAME      PIC X(30).
   05 BALANCE   PIC S9(7)V99 COMP-3.

Becomes:

[StructLayout(LayoutKind.Explicit)]
public class CUSTOMER_RECORD {
    [FieldOffset(0)] public int ID;
    [FieldOffset(4)] public string NAME;
    [FieldOffset(34)] public decimal BALANCE;
}

Interop marshaling engine
-------------------------
CobolSharp.Runtime provides marshaling helpers:

- StringMarshaler
- NumericMarshaler
- PackedDecimalMarshaler
- ArrayMarshaler
- RecordMarshaler
- ObjectMarshaler

Responsibilities:
- Convert COBOL values to .NET values
- Convert .NET values to COBOL storage
- Handle REDEFINES overlays
- Handle OCCURS DEPENDING ON dynamic bounds
- Handle nullable values (optional)

Exception mapping
-----------------
.NET exceptions map to COBOL exceptions:

- ArgumentException → ON EXCEPTION
- InvalidOperationException → ON EXCEPTION
- FileNotFoundException → INVALID KEY (if file operation)
- JsonException → ON EXCEPTION
- XmlException → ON EXCEPTION

COBOL exceptions map to .NET exceptions when thrown across boundaries.

Interop safety rules
--------------------
To ensure predictable behavior:

- No implicit marshaling of unsupported types
- No automatic conversion of complex .NET objects without metadata
- No inheritance from COBOL classes unless explicitly allowed
- No REDEFINES flattening unless safe
- No automatic async → sync bridging without runtime helpers

Interop code generation
-----------------------
CobolSharp generates:

- C# wrappers for COBOL classes (optional)
- COBOL CALL stubs for .NET methods
- Marshaling metadata attributes
- Interop helper classes

Example generated stub:

public static class CobolInterop_MyLib_Math {
    public static int Add(int a, int b) =>
        MyLib.Math.Add(a, b);
}

Tooling support
---------------
The interop system integrates with:

- LSP (hover shows .NET signatures)
- Debugger (shows .NET objects and COBOL storage)
- Modernization toolkit (suggests interop boundaries)
- Data layout visualizer
- IL viewer

Testing strategy
----------------
Interop is validated with:

- Unit tests for marshaling
- Golden tests for generated stubs
- Integration tests calling .NET from COBOL
- Integration tests calling COBOL from C#
- Exception propagation tests
- AOT/WASM compatibility tests

Summary
-------
The CobolSharp Interop Architecture:
- Enables seamless COBOL ↔ .NET integration
- Provides type‑safe marshaling between COBOL data descriptions and .NET types
- Supports calling .NET from COBOL and COBOL from .NET
- Generates clean, verifiable CIL
- Works across CoreCLR, AOT, and WASM (via dotnet publish)
- Is fully aligned with the CIL‑only architecture
