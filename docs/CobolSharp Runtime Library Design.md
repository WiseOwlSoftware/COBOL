CobolSharp Runtime Library Design (CIL‑Only)
===========================================

High‑level goals
----------------
- Provide a complete, deterministic implementation of COBOL semantics for programs compiled to .NET CIL.
- Serve as the *only* runtime target for CobolSharp‑generated assemblies.
- Implement all COBOL‑85 → COBOL‑2023 features:
  - Packed decimal arithmetic
  - File I/O (sequential, indexed, relative)
  - SORT/MERGE
  - JSON/XML
  - STRING/UNSTRING/INSPECT
  - Date/time and environment functions
  - Collation
  - Exception and condition handling
  - OO and generics
- Be fully managed, cross‑platform, and compatible with:
  - CoreCLR
  - .NET AOT
  - .NET WASM (via dotnet publish)
- Integrate tightly with the CIL backend.

Overall structure
-----------------
Namespace: CobolSharp.Runtime

Core components:
- ExecutionContext
- StorageManager
- FileManager
- SortMergeEngine
- NumericEngine
- StringEngine
- JsonEngine
- XmlEngine
- DateTimeEngine
- CollationEngine
- ExceptionEngine
- IntrinsicFunctionLibrary

ExecutionContext
----------------
Represents the per‑thread execution state of a COBOL program.

Fields:
- CallStack: stack of ActivationRecord
- CurrentProgram: ProgramDescriptor
- Environment: EnvironmentDescriptor (command‑line args, environment variables)
- FileTable: Dictionary<FileId, FileHandle>
- Collation: CollationDescriptor
- DiagnosticsSink: optional logging/diagnostics

ActivationRecord:
- ProgramOrMethod: ProgramDescriptor or MethodDescriptor
- Locals: StorageBlock (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE, OBJECT‑DATA)
- Parameters: object[] (BY VALUE, BY REFERENCE, BY CONTENT)
- ReturnAddress: IL offset (for stepping/debugging)
- ExceptionHandlers: stack of handler descriptors (ON EXCEPTION, INVALID KEY, AT END)

StorageManager
--------------
Responsible for allocating and manipulating COBOL data areas.

Key concepts:
- StorageBlock:
  - byte[] Buffer
  - List<FieldLayout> Layout
- FieldLayout:
  - Offset
  - Length
  - PicDescriptor
  - UsageKind
  - OccursDescriptor (optional)
  - RedefinesGroup (optional)

APIs:
- AllocateBlock(DataRootDescriptor) → StorageBlock
- ReadField(StorageBlock, FieldLayout) → Value
- WriteField(StorageBlock, FieldLayout, Value)
- Move(srcBlock, srcField, dstBlock, dstField, MoveOptions)

Supports:
- PIC/USAGE conversions
- REDEFINES aliasing
- OCCURS DEPENDING ON dynamic bounds
- RENAMES (synthetic field ranges)

FileManager
-----------
Implements COBOL file semantics using .NET streams or pluggable backends.

FileHandle:
- FileId
- Organization (Sequential, Indexed, Relative)
- AccessMode (Input, Output, I‑O, Extend)
- RecordLength
- RecordBuffer (StorageBlock)
- StatusField (reference to COBOL FILE STATUS)
- Backend: IFileBackend

APIs:
- Open(FileDescriptor, mode)
- Close(FileHandle)
- ReadNext / ReadPrevious
- ReadKey
- Write
- Rewrite
- Delete
- Start
- Return
- Release

Backends:
- SequentialFileBackend (System.IO)
- IndexedFileBackend (B+‑tree or database)
- RelativeFileBackend (record‑indexed)

SortMergeEngine
---------------
Implements COBOL SORT and MERGE.

SortContext:
- Keys
- USING files
- GIVING files
- InputProcedure callback
- OutputProcedure callback

APIs:
- Sort(SortContext)
- Merge(SortContext)

Implements:
- In‑memory sort for small datasets
- External sort for large datasets
- Stable sorting
- Key comparison using CollationEngine

NumericEngine
-------------
Implements COBOL numeric semantics.

Responsibilities:
- Packed decimal (COMP‑3)
- Binary and display numeric arithmetic
- Rounding and truncation
- Size error detection
- Sign handling

APIs:
- Add/Subtract/Multiply/Divide
- Compare
- Convert (PIC/USAGE conversions)
- Pack/Unpack decimal

All arithmetic is implemented in managed code.

StringEngine
------------
Implements:
- STRING
- UNSTRING
- INSPECT TALLYING
- INSPECT REPLACING

APIs:
- StringConcat(List<StringSegment>, PointerDescriptor)
- Unstring(string input, UnstringDescriptor)
- InspectTallying
- InspectReplacing

Supports:
- Delimiters
- ALL literal
- POINTER and TALLYING
- OVERFLOW handling

JsonEngine
----------
Implements JSON PARSE and JSON GENERATE using System.Text.Json.

APIs:
- ParseJson(string jsonText, JsonMappingDescriptor, ExecutionContext)
- GenerateJson(JsonMappingDescriptor, ExecutionContext)

Supports:
- WITH DETAIL
- Mapping COBOL data items to JSON objects/arrays
- Exception handling

XmlEngine
---------
Implements XML PARSE and XML GENERATE using System.Xml.

APIs:
- ParseXml(string xmlText, XmlParseDescriptor, ExecutionContext)
- GenerateXml(XmlGenerateDescriptor, ExecutionContext)

Supports:
- Processing procedures
- Exception handling
- COUNT IN

DateTimeEngine
--------------
Implements COBOL date/time intrinsics.

APIs:
- CurrentDate
- CurrentTime
- FormatDate
- ParseDate
- DateDiff

CollationEngine
---------------
Implements COBOL collating sequences.

APIs:
- CompareStrings(string a, string b, CollationDescriptor)
- SortKey(string input, CollationDescriptor)

Supports:
- Standard ASCII
- EBCDIC‑compatible tables
- Custom collations

ExceptionEngine
---------------
Implements COBOL exception model.

Maps runtime exceptions to:
- ON EXCEPTION
- INVALID KEY
- AT END

APIs:
- RaiseFileException
- RaiseJsonException
- RaiseXmlException
- RaiseGenericException

IntrinsicFunctionLibrary
------------------------
Implements COBOL intrinsic functions.

Examples:
- NUMVAL, NUMVAL‑C
- INTEGER, INTEGER‑PART, FRACTION‑PART
- LENGTH, TRIM, LOWER‑CASE, UPPER‑CASE
- RANDOM
- CURRENT‑DATE
- WHEN‑COMPILED

APIs:
- EvaluateIntrinsic(string name, List<Value> args, ExecutionContext)

Integration with CIL backend
----------------------------
The CIL backend emits calls into the runtime for:

- Packed decimal arithmetic
- File I/O
- SORT/MERGE
- JSON/XML
- STRING/UNSTRING/INSPECT
- Date/time
- Collation
- Intrinsic functions

Runtime methods are:
- Public
- Static
- Purely managed
- Fully verifiable

Debugging integration
---------------------
Runtime exposes:
- Sequence points (via PDB)
- Local variable names
- Exception boundaries
- File I/O state
- Packed decimal inspection helpers

Debugger can:
- Inspect StorageBlocks
- Decode PIC/USAGE values
- Visualize OCCURS arrays
- Visualize REDEFINES overlays

Performance strategy
--------------------
- Packed decimal optimized with lookup tables
- File I/O buffered
- SORT/MERGE uses efficient algorithms
- String operations optimized for slicing
- Collation tables cached
- Minimal allocations in hot paths

Testing strategy
----------------
- Unit tests for each engine
- Golden tests for numeric and string behavior
- File I/O conformance tests
- JSON/XML round‑trip tests
- SORT/MERGE correctness tests
- Cross‑compiler behavior tests (GnuCOBOL/Micro Focus)
- Regression suite

Summary
-------
The CobolSharp runtime library:
- Is the single execution engine for all CobolSharp‑generated .NET assemblies
- Implements all COBOL semantics in managed code
- Provides packed decimal, file I/O, SORT/MERGE, JSON/XML, string ops, date/time, collation, and exceptions
- Integrates tightly with the CIL backend and debugger
- Is portable across all .NET environments (CoreCLR, AOT, WASM)
