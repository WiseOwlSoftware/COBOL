CobolSharp Backend Codegen Architecture (CIL‑Only)
==================================================

High‑level goals
----------------
- Transform the fully‑resolved semantic model into verifiable, efficient .NET CIL assemblies.
- Treat .NET IL as the *only* backend target.
- Produce:
  - .dll or .exe assemblies
  - PDB debug symbols
  - Optional single‑file or AOT‑compiled artifacts (via dotnet tooling)
- Preserve COBOL semantics:
  - Packed decimal arithmetic
  - File I/O semantics
  - PERFORM/GO TO control flow
  - REDEFINES aliasing
  - OCCURS DEPENDING ON dynamic bounds
  - OO and generics
  - JSON/XML operations
- Integrate cleanly with the CobolSharp runtime library.

Overall pipeline
----------------
SemanticModel
    ↓
ILBuilder (CobolSharp IL)
    ↓
Optimization Pipeline
    ↓
CILBackend
    ↓
.NET Assembly (DLL/EXE) + PDB
    ↓
Optional: dotnet publish (AOT, single‑file, WASM, native)

There is **no VM**, no bytecode, no interpreter.

Core backend components
-----------------------
1. CILBackend
2. TypeEmitter
3. MethodEmitter
4. FieldEmitter
5. ControlFlowEmitter
6. InstructionLowerer
7. MetadataEmitter
8. DebugInfoEmitter

CILBackend
----------
The central backend that consumes the optimized ILModule and emits:

- .NET types
- .NET methods
- .NET fields
- IL instructions
- Metadata
- PDB debug symbols

Uses:
- System.Reflection.Metadata
- System.Reflection.Emit (or a Roslyn‑based emitter)
- Portable PDB writer

TypeEmitter
-----------
Maps CobolSharp IL types to .NET types:

ILType → TypeDefinitionHandle

Supports:
- Program types (static classes)
- Class types (OO COBOL)
- Interface types
- Record types (01‑level structures)
- Array types (OCCURS)
- Generic type definitions
- Generic type instantiations

Record layout:
- Each COBOL 01‑level group becomes a .NET class with explicit layout.
- Fields use FieldOffset attributes to match COBOL storage.
- REDEFINES groups share offsets.

MethodEmitter
-------------
Maps ILMethod to .NET methods:

ILMethod → MethodDefinitionHandle

Handles:
- Parameters
- Return types
- Local variables
- Generic parameters
- Attributes (static, virtual, override)

Generates:
- Method body (CIL)
- Local variable signatures
- Exception handling blocks

FieldEmitter
------------
Maps ILField to .NET fields:

ILField → FieldDefinitionHandle

Handles:
- Static fields (WORKING‑STORAGE, CLASS‑DATA)
- Instance fields (OBJECT‑DATA)
- Explicit layout for record fields
- Packed decimal storage (byte arrays)

ControlFlowEmitter
------------------
Consumes ILBasicBlocks and emits:

- CIL branch instructions
- Labels
- Structured exception regions
- PERFORM/GO TO lowering

PERFORM lowering:
- PERFORM paragraph → call to generated method
- PERFORM THRU → call to synthetic wrapper method
- PERFORM UNTIL → while loop
- PERFORM VARYING → for/while loop with induction variable

GO TO lowering:
- Direct branch to label inside method
- If GO TO crosses paragraph boundaries, method splitting is applied

InstructionLowerer
------------------
Maps CobolSharp IL instructions to .NET IL opcodes.

Examples:

Loads/stores:
- LOAD_LOCAL → ldloc
- STORE_LOCAL → stloc
- LOAD_FIELD → ldfld
- STORE_FIELD → stfld

Arithmetic:
- ADD_INT → add
- SUB_INT → sub
- MUL_INT → mul
- DIV_INT → div
- Packed decimal ops → call into CobolSharp.Runtime.NumericEngine

Branches:
- BR → br
- BR_TRUE → brtrue
- BR_FALSE → brfalse
- BR_EQ → beq
- BR_NE → bne.un
- SWITCH → switch

Object creation:
- NEW_OBJECT → newobj
- INIT_OBJECT → call constructor

Method calls:
- CALL → call
- CALL_VIRTUAL → callvirt
- CALL_STATIC → call

String operations:
- STRING/UNSTRING → runtime calls

JSON/XML:
- JSON PARSE/GENERATE → runtime calls
- XML PARSE/GENERATE → runtime calls

File I/O:
- READ/WRITE/REWRITE/DELETE → runtime FileManager calls

MetadataEmitter
---------------
Emits:
- Assembly metadata
- Type metadata
- Method metadata
- Field metadata
- Custom attributes
- Generic parameter constraints

Includes:
- PIC/USAGE metadata (optional)
- Data layout metadata
- Source mapping metadata

DebugInfoEmitter
----------------
Generates PDB debug symbols:

- Sequence points (source line → IL offset)
- Local variable names
- Parameter names
- Scope boundaries
- Async/iterator metadata (if needed)

Source mapping:
- Original source → preprocessed source → IL
- COPYbook lines map back to original files

Exception handling
------------------
CIL exception blocks represent COBOL exception constructs:

- ON EXCEPTION
- INVALID KEY
- AT END

Lowering:
- Surround runtime calls with try/catch
- Catch specific runtime exceptions
- Branch to handler blocks

Generics support
----------------
COBOL generics map to .NET generics:

- TYPEDEF GENERIC → generic type definition
- Generic methods → generic method definitions
- Instantiations → constructed generic types

Constraints:
- OF type → .NET generic constraints

Runtime integration
-------------------
All complex COBOL semantics are implemented in:

CobolSharp.Runtime.dll

CIL backend emits calls to runtime for:
- Packed decimal arithmetic
- File I/O
- SORT/MERGE
- JSON/XML
- String operations
- Date/time
- Collation
- Intrinsic functions

Deployment options
------------------
Since the output is pure .NET assemblies, developers can use:

- dotnet run
- dotnet publish
- dotnet publish /p:PublishAot=true (native AOT)
- dotnet publish -blazorwasm (WASM via .NET)

CobolSharp does not implement its own WASM or native backend.

Testing strategy
----------------
- Golden IL tests
- CIL verification tests
- Reflection-based type/method/field validation
- Runtime behavior tests
- Debug symbol tests
- Cross‑compiler tests (CobolSharp vs GnuCOBOL/Micro Focus)

Summary
-------
The CobolSharp CIL backend:
- Is the **only** backend
- Emits verifiable .NET IL assemblies
- Uses .NET metadata, PDBs, and runtime integration
- Preserves COBOL semantics faithfully
- Supports OO, generics, JSON/XML, file I/O, packed decimal
- Enables deployment to any .NET target (CoreCLR, AOT, WASM)
