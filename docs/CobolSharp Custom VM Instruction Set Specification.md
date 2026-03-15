CobolSharp Custom VM Instruction Set Specification - Proposed but not currently to be implemented
==================================================

High-level goals
----------------
- Provide a lightweight, deterministic, portable execution environment for COBOL programs.
- Serve as an alternative backend to CIL/WASM/LLVM.
- Support all COBOL-85 → COBOL-2023 semantics:
  - Packed decimal arithmetic
  - File I/O
  - PERFORM/GO TO control flow
  - REDEFINES aliasing
  - OCCURS DEPENDING ON dynamic arrays
  - OO and generics (via metadata + dispatch tables)
  - JSON/XML operations (via runtime calls)
- Enable:
  - Fast startup
  - Small binaries
  - Easy debugging
  - Sandboxed execution
  - Embedding in other applications

VM architecture overview
------------------------
The CobolSharp VM is:
- Stack-based (like WASM or JVM)
- Register-assisted (optional fast-path registers)
- Deterministic (no undefined behavior)
- Memory-safe (bounds-checked)
- Designed for COBOL’s data model

Core components:
- VMInstruction (bytecode)
- VMFunction (compiled method)
- VMModule (compiled program)
- VMRuntime (execution engine)
- VMHeap (data areas)
- VMStack (operand + call stack)
- VMDispatcher (method invocation)
- VMFileSubsystem (file I/O)
- VMNumericSubsystem (packed decimal)
- VMJsonSubsystem / VMXmlSubsystem

Memory model
------------
Memory is divided into:
1. Static data area (WORKING-STORAGE, CLASS-DATA)
2. Stack frames (LOCAL-STORAGE, parameters)
3. Heap objects (OO instances, dynamic OCCURS)
4. File buffers (record areas)

All memory accesses are bounds-checked.

Instruction format
------------------
Each instruction is:
- 1 byte opcode
- 0–8 bytes of operands (depending on instruction)
- Optional metadata index for debugging

Example:
  0x10 00 02   → LOAD_LOCAL 2
  0x21 01      → ADD_INT8
  0x50 00 10   → CALL 16

Instruction categories
----------------------

1. Stack operations
-------------------
PUSH_CONST <value>
PUSH_ADDR <offset>
POP
DUP
SWAP

2. Load/store operations
------------------------
LOAD_LOCAL <index>
STORE_LOCAL <index>

LOAD_FIELD <offset>
STORE_FIELD <offset>

LOAD_STATIC <offset>
STORE_STATIC <offset>

LOAD_ARRAY <index>
STORE_ARRAY <index>

3. Arithmetic operations
------------------------
ADD_INT
SUB_INT
MUL_INT
DIV_INT
MOD_INT

ADD_DECIMAL
SUB_DECIMAL
MUL_DECIMAL
DIV_DECIMAL

NEG
ABS

COMPARE_INT
COMPARE_DECIMAL
COMPARE_STRING

4. Logical operations
---------------------
AND
OR
NOT

5. Branching and control flow
-----------------------------
BR <label>
BR_TRUE <label>
BR_FALSE <label>
BR_EQ <label>
BR_NE <label>
BR_GT <label>
BR_LT <label>
BR_GE <label>
BR_LE <label>

SWITCH <table>

RETURN
EXIT_PROGRAM
STOP_RUN

6. Procedure/method invocation
------------------------------
CALL <functionId>
CALL_VIRTUAL <methodId>
CALL_STATIC <methodId>
CALL_SUPER <methodId>

NEW_OBJECT <typeId>
INIT_OBJECT <constructorId>

7. String operations
--------------------
STR_CONCAT
STR_SUBSTR
STR_LENGTH
STR_COMPARE
STR_TO_NUM
NUM_TO_STR

8. Packed decimal operations
----------------------------
DEC_PACK
DEC_UNPACK
DEC_ADD
DEC_SUB
DEC_MUL
DEC_DIV
DEC_COMPARE
DEC_ROUND

9. File I/O operations
----------------------
FILE_OPEN <fileId>
FILE_CLOSE <fileId>
FILE_READ_NEXT <fileId>
FILE_READ_PREV <fileId>
FILE_READ_KEY <fileId>
FILE_WRITE <fileId>
FILE_REWRITE <fileId>
FILE_DELETE <fileId>
FILE_START <fileId>
FILE_RETURN <fileId>
FILE_RELEASE <fileId>

10. JSON/XML operations
-----------------------
JSON_PARSE <mappingId>
JSON_GENERATE <mappingId>

XML_PARSE <mappingId>
XML_GENERATE <mappingId>

11. Object-oriented operations
------------------------------
LOAD_METHOD_TABLE <typeId>
LOAD_METHOD <methodId>
INVOKE_METHOD <methodId>
INVOKE_STATIC <methodId>

12. Generics operations
-----------------------
LOAD_GENERIC_PARAM <index>
INSTANTIATE_GENERIC <typeId>

13. Runtime calls
-----------------
CALL_RUNTIME <runtimeFunctionId>

Used for:
- SORT/MERGE
- Date/time
- Collation
- Intrinsic functions

14. Debugging instructions
--------------------------
BREAKPOINT
TRACE <level>
NOP

Execution model
---------------
The VM executes:
- One instruction at a time
- On a single-threaded interpreter loop
- With optional JIT compilation (future extension)

Call stack:
- Each CALL pushes a frame:
  - Return address
  - Locals
  - Parameters
  - Exception handlers

Exception model:
- VM exceptions map to COBOL exceptions:
  - INVALID KEY
  - AT END
  - ON EXCEPTION
- Handlers stored in frame metadata

Module format
-------------
VMModule contains:
- Header (version, flags)
- Constant pool
- Type table
- Method table
- Field table
- Function bodies (bytecode)
- Static data layout
- Debug info (optional)

Binary format is compact and endian-neutral.

Debugging support
-----------------
VM supports:
- Breakpoints
- Step in/out/over
- Stack traces
- Variable inspection
- Memory inspection
- Instruction pointer tracking
- Source mapping (via debug metadata)

Performance strategy
--------------------
- Hot-path caching of decoded instructions
- Optional register caching for top-of-stack
- Peephole optimization during load
- Inline numeric helpers
- Fast dispatch table for opcodes

Testing strategy
----------------
- Instruction-level tests
- Bytecode round-trip tests
- VM state snapshot tests
- Cross-backend equivalence tests (CIL/WASM/LLVM vs VM)
- Stress tests with large COBOL programs
- Packed decimal correctness tests
- File I/O behavior tests

Summary
-------
The CobolSharp VM instruction set:
- Provides a compact, deterministic, portable execution environment
- Supports all COBOL-2023 semantics
- Uses a stack-based bytecode with optional registers
- Integrates with the runtime library for complex operations
- Enables fast startup, sandboxing, and easy embedding
- Serves as a robust alternative backend to CIL/WASM/LLVM
