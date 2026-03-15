CobolSharp COBOL Interop Architecture — .NET Types, Assemblies, INVOKE .NET & Type Mapping (CIL‑Only)
=====================================================================================================

Purpose
-------
Define the authoritative architecture for:
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

This document governs how CobolSharp integrates COBOL programs with .NET assemblies and runtime types.

------------------------------------------------------------
SECTION 1 — INTEROP OVERVIEW
------------------------------------------------------------

CobolSharp supports:
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

------------------------------------------------------------
SECTION 2 — ASSEMBLY & TYPE RESOLUTION
------------------------------------------------------------

2.1 Compile‑time resolution
---------------------------
CobolSharp resolves:
- Assembly references
- Type names
- Method signatures
- Property signatures

All resolution occurs at compile time.

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

Compiler resolves:
- Namespace
- Type
- Method overload
- Parameter types

------------------------------------------------------------
SECTION 3 — COBOL → .NET TYPE MAPPING
------------------------------------------------------------

3.1 Elementary types
--------------------
DISPLAY → string  
NATIONAL → string (UTF‑16)  
COMP/COMP‑5 → int/long  
COMP‑3 → Decimal  
DISPLAY numeric → Decimal  
Boolean → bool  
Object reference → object

3.2 Group items
---------------
Group items map to:
- byte[] (raw buffer)
- Or custom struct if annotated

3.3 OCCURS tables
-----------------
Mapped to:
- T[] arrays
- Or List<T> if annotated

3.4 NULL handling
-----------------
COBOL has no null:
- Object references default to null
- DISPLAY/NATIONAL default to spaces
- Numeric default to zero

------------------------------------------------------------
SECTION 4 — .NET → COBOL TYPE MAPPING
------------------------------------------------------------

4.1 string → DISPLAY/NATIONAL
-----------------------------
DISPLAY:
- ASCII only
- Non‑ASCII → runtime error

NATIONAL:
- UTF‑16 copied directly

4.2 int/long → numeric
----------------------
Converted to Decimal then to PIC.

4.3 Decimal → numeric
---------------------
Scaled and rounded per PIC.

4.4 bool → DISPLAY
-------------------
"TRUE" / "FALSE".

4.5 object → COBOL object reference
-----------------------------------
Stored as:
- Reference in ExecutionContext.ObjectTable

------------------------------------------------------------
SECTION 5 — INVOKE ARCHITECTURE
------------------------------------------------------------

5.1 Static method
-----------------
INVOKE System.Math::Sqrt USING x RETURNING y.

Lowering:
- y = System.Math.Sqrt(x)

5.2 Instance method
-------------------
INVOKE sb Append USING "Hello".

Lowering:
- sb.Append("Hello")

5.3 Constructor
---------------
INVOKE System.Text.StringBuilder NEW RETURNING sb.

Lowering:
- sb = new StringBuilder()

5.4 Property GET
----------------
INVOKE obj::Length RETURNING len.

Lowering:
- len = obj.Length

5.5 Property SET
----------------
INVOKE obj::Capacity = 100.

Lowering:
- obj.Capacity = 100

5.6 Field access
----------------
INVOKE obj::SomeField RETURNING v.

Lowering:
- v = obj.SomeField

------------------------------------------------------------
SECTION 6 — METHOD OVERLOAD RESOLUTION
------------------------------------------------------------

6.1 Compile‑time resolution
---------------------------
Compiler selects overload based on:
- Number of USING parameters
- COBOL → .NET type mapping
- Best match rules

6.2 Ambiguous overloads
-----------------------
Compile‑time error.

6.3 Generic methods
-------------------
Supported only if:
- Type arguments inferred from parameters
- Or explicitly specified via:
  INVOKE Type::Method<T> USING …

------------------------------------------------------------
SECTION 7 — PARAMETER PASSING RULES
------------------------------------------------------------

7.1 BY VALUE
------------
COBOL BY VALUE → .NET value parameter.

7.2 BY REFERENCE
----------------
COBOL BY REFERENCE → ref/out parameter.

Lowering:
- ref for input/output
- out for output‑only

7.3 BY CONTENT
--------------
COBOL BY CONTENT → .NET value parameter (copy).

7.4 Object references
---------------------
Passed as:
- .NET object reference

------------------------------------------------------------
SECTION 8 — EXCEPTION TRANSLATION
------------------------------------------------------------

8.1 .NET exception → COBOL exception
------------------------------------
Caught and translated to:
- ExceptionState
- ON EXCEPTION block

8.2 ExceptionState fields
-------------------------
- ExceptionType
- Message
- StackTrace (optional)
- TargetMethod

8.3 Rethrow behavior
--------------------
If no ON EXCEPTION:
- ExceptionState set
- Execution continues

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 Static call
---------------
call Type::Method

9.2 Virtual call
----------------
callvirt obj::Method

9.3 Constructor
---------------
newobj Type::.ctor

9.4 Property GET
----------------
callvirt get_Property

9.5 Property SET
----------------
callvirt set_Property

9.6 ref/out parameters
----------------------
ldloca.s temp  
call Method

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- .NET object references
- Method parameters
- Return values
- Property values
- ExceptionState
- Type names
- Assembly names

------------------------------------------------------------
SECTION 11 — AOT/WASM‑SAFE INTEROP
------------------------------------------------------------

11.1 No reflection
------------------
All calls static or virtual.

11.2 No dynamic codegen
-----------------------
No IL emit.

11.3 Deterministic binding
--------------------------
All types/methods resolved at compile time.

11.4 WASM interop
-----------------
Supported for:
- String
- Numeric types
- Boolean
- Arrays
- Simple objects

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 INVOKE on null object
--------------------------
Runtime error:
OBJECT-REFERENCE-NOT-SET

12.2 Passing NATIONAL to .NET
-----------------------------
Converted to UTF‑16 string.

12.3 Passing DISPLAY with non‑ASCII
-----------------------------------
Runtime error.

12.4 Returning null to DISPLAY
------------------------------
Converted to spaces.

12.5 Returning null to object reference
---------------------------------------
Stored as null.

12.6 Generic method with no inference
-------------------------------------
Compile‑time error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Interop Architecture:
- Provides full COBOL → .NET interop via INVOKE, NEW, properties, and fields
- Maps COBOL types to .NET types deterministically
- Resolves assemblies, types, and methods at compile time
- Generates clean, verifiable, AOT/WASM‑safe CIL
- Integrates tightly with ExecutionContext and ObjectTable
