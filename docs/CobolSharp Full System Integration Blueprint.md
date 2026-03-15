CobolSharp Full System Integration Blueprint (CIL‑Only)
=======================================================

High‑level goals
----------------
- Define how all CobolSharp subsystems interlock into a coherent, production‑grade compiler toolchain.
- Ensure every subsystem is modular, testable, replaceable, and independently evolvable.
- Provide a unified architecture centered around **.NET CIL as the single backend**.
- Guarantee deterministic, reproducible behavior across COBOL‑85 → COBOL‑2023.
- Support modern developer workflows: LSP, debugging, CI/CD, AOT, and cross‑platform execution.

Top‑level architecture
----------------------
CobolSharp is composed of the following major layers:

1. Preprocessor  
2. Lexer  
3. Parser  
4. Semantic Analyzer  
5. IL Generator  
6. Optimization Pipeline  
7. CIL Backend  
8. Runtime Library  
9. Debugger (PDB + .NET debugging APIs)  
10. LSP/IDE Integration  
11. Packaging & Distribution  

These layers form a pipeline:

  Source Files
      ↓
  Preprocessor
      ↓
  Lexer
      ↓
  Parser
      ↓
  Semantic Model
      ↓
  IL Generation
      ↓
  Optimization Pipeline
      ↓
  CIL Backend
      ↓
  .NET Assembly (DLL/EXE) + PDB
      ↓
  Runtime Execution (CoreCLR / AOT / WASM via dotnet)
      ↓
  Debugger (optional)
      ↓
  IDE/LSP (optional)

There is **no VM**, no bytecode interpreter, no alternate backend.

Subsystem integration details
-----------------------------

1. Preprocessor → Lexer
-----------------------
- Preprocessor expands COPY/REPLACE and pseudo‑text.
- Produces:
  - Expanded source
  - Source mapping table (original → expanded)
- Lexer consumes expanded source.
- Lexer modes (comment, copy, pseudo‑text) are driven by preprocessor metadata.

2. Lexer → Parser
-----------------
- Lexer produces token stream with:
  - Token type
  - Lexeme
  - Source location (mapped back to original file)
- Parser consumes tokens using:
  - Dialect gates (85/2002/2014/2023)
  - Error recovery sync points
- Parser produces:
  - Concrete AST
  - Error nodes for malformed constructs

3. Parser → Semantic Analyzer
-----------------------------
Semantic analyzer consumes AST and builds:

- Symbol tables
- Type graph
- Data description tree
- PERFORM control‑flow graph
- OO/generics model
- File model
- JSON/XML model

Semantic passes run incrementally and produce:

- Fully resolved semantic model
- Diagnostics
- Metadata for IL generation

4. Semantic Analyzer → IL Generator
-----------------------------------
IL generator consumes semantic model and produces:

- ILModule
- ILTypes
- ILMethods
- ILBasicBlocks
- ILInstructions
- Metadata for debugging and backend emission

IL is backend‑agnostic but designed specifically for lowering to .NET CIL.

5. IL Generator → Optimization Pipeline
---------------------------------------
Optimization pipeline consumes ILModule and applies:

- Control‑flow simplification
- Dead code elimination
- Constant folding
- Strength reduction
- Copy propagation
- Redundant move elimination
- Loop optimization
- Generic specialization
- Peephole optimization

Produces optimized ILModule.

6. Optimization Pipeline → CIL Backend
--------------------------------------
The CIL backend consumes optimized ILModule and emits:

- .NET types
- .NET methods
- .NET fields
- IL instructions
- Metadata
- PDB debug symbols

CIL backend responsibilities:
- Lower CobolSharp IL to System.Reflection.Emit or metadata builder APIs
- Emit explicit layout for COBOL data structures
- Emit calls into CobolSharp.Runtime for complex semantics
- Emit sequence points for debugging
- Emit exception regions for ON EXCEPTION, INVALID KEY, AT END

Output:
- .dll or .exe
- .pdb
- Optional: AOT/native via dotnet publish
- Optional: WASM via dotnet publish (Blazor/WASI)

7. CIL Backend → Runtime Library
--------------------------------
Runtime library provides:

- File I/O
- Packed decimal arithmetic
- SORT/MERGE
- JSON/XML
- String operations
- Date/time
- Collation
- Exception model

CIL backend emits calls into runtime for:
- NumericEngine
- FileManager
- SortMergeEngine
- JsonEngine
- XmlEngine
- StringEngine
- DateTimeEngine
- CollationEngine
- Intrinsic functions

Runtime is:
- Purely managed
- Cross‑platform
- Compatible with CoreCLR, AOT, WASM

8. Runtime Library → Debugger
-----------------------------
Debugger integrates with runtime via:

- PDB sequence points
- Local variable signatures
- Exception boundaries
- File I/O state
- StorageBlock inspection helpers

Debugger supports:
- Breakpoints
- Step in/out/over
- Stack traces
- Variable inspection (PIC/USAGE aware)
- Memory inspection
- Paragraph/section stepping
- PERFORM flow visualization

9. Debugger → LSP/IDE Integration
---------------------------------
LSP server integrates with debugger to provide:

- Breakpoints
- Stepping
- Stack traces
- Variable inspection
- Hover info
- Go‑to‑definition
- Rename refactoring
- Code actions
- Diagnostics

LSP server uses semantic model for:
- Completion
- Hover
- Symbol search
- Refactoring

10. LSP/IDE → Packaging & Distribution
--------------------------------------
Distribution bundles:

- Compiler CLI
- LSP server
- Runtime library
- Tools (IL viewer, CFG viewer)
- Templates
- Documentation

Ensures:
- Cross‑platform installation
- Reproducible builds
- Versioned releases
- CI/CD integration

Cross‑cutting concerns
----------------------

Error handling
--------------
- Preprocessor errors map to original source
- Lexer errors produce recoverable tokens
- Parser errors produce error nodes
- Semantic errors produce diagnostics
- Backend errors produce build failures
- Runtime errors map to COBOL exceptions
- Debugger surfaces all errors to IDE

Source mapping
--------------
Every stage preserves mapping:

Original source → Preprocessed source → Tokens → AST → Semantic nodes → IL → CIL → PDB

This enables:
- Accurate breakpoints
- Accurate stepping
- Accurate diagnostics
- Accurate variable inspection

Performance strategy
--------------------
- Incremental LSP pipeline
- Cached preprocessor expansions
- Cached semantic models
- Parallel CIL emission
- Lazy evaluation of expensive analyses
- Optimized runtime operations

Testing strategy
----------------
- Unit tests for each subsystem
- Golden tests for IL and CIL output
- Integration tests for full pipeline
- Cross‑compiler equivalence tests
- Conformance tests (ISO/IEC 1989:2023)
- Regression suite for all fixed bugs

Security considerations
-----------------------
- Sandboxed execution via .NET WASM/AOT if needed
- Safe file I/O modes
- COPY/REPLACE path sanitization
- No arbitrary code execution
- Optional safe mode (no CALL, no file I/O)

Summary
-------
The CobolSharp Full System Integration Blueprint:
- Defines how all subsystems interlock into a coherent whole
- Ensures modularity, testability, and reproducibility
- Uses .NET CIL as the single backend target
- Provides deep IDE/debugger integration
- Preserves COBOL semantics across the entire pipeline
- Enables long‑term evolution of the compiler architecture
