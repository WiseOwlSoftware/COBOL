CobolSharp Master Architecture Document (CIL‑Only)
=================================================

Purpose
-------
This document provides a unified, end‑to‑end architectural overview of CobolSharp — a production‑quality COBOL compiler targeting .NET CIL and implementing ISO/IEC 1989:2023 semantics. It consolidates all major subsystems into a single, coherent reference suitable for maintainers, contributors, and system architects.

CobolSharp is designed to:
- Compile COBOL directly to .NET CIL
- Provide a modern development experience (LSP, debugging, refactoring)
- Support full COBOL semantics (85 → 2023)
- Integrate with .NET libraries and tooling
- Enable modernization and migration of legacy COBOL systems
- Produce deterministic, verifiable, cross‑platform .NET assemblies

Top‑Level Architecture
----------------------
CobolSharp consists of the following major subsystems:

1. Preprocessor  
2. Lexer  
3. Parser  
4. Semantic Analyzer  
5. IL Generator  
6. Optimization Pipeline  
7. CIL Backend  
8. Runtime Library  
9. Debugger  
10. LSP/IDE Integration  
11. Packaging & Distribution  
12. Modernization & Migration Toolkit  
13. Interop Architecture (COBOL ↔ .NET)  

These subsystems form a pipeline:

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
  .NET Assembly + PDB
      ↓
  Runtime Execution
      ↓
  Debugger (optional)
      ↓
  IDE/LSP (optional)

Subsystem Summaries
-------------------

1. Preprocessor
---------------
Responsibilities:
- COPY expansion
- REPLACE processing
- Pseudo‑text handling
- COPY REPLACING
- Source mapping (original → expanded)

Outputs:
- Expanded source
- Mapping tables for debugging and diagnostics

2. Lexer
--------
Tokenizes expanded source into:
- Token type
- Lexeme
- Source location
- Dialect‑aware tokenization

Supports:
- Fixed‑form and free‑form
- Continuation lines
- Compiler directives

3. Parser
---------
Builds AST using:
- ANTLR‑generated grammar
- Dialect gates (85/2002/2014/2023)
- Error recovery
- Paragraph/section structure

Outputs:
- AST
- Error nodes
- Structural metadata

4. Semantic Analyzer
--------------------
Builds:
- Symbol tables
- Type graph
- Data description tree
- PERFORM control‑flow graph
- OO/generics model
- File model
- JSON/XML model

Ensures:
- Type correctness
- Data layout correctness
- Control‑flow correctness
- File I/O semantics
- Intrinsic function resolution

5. IL Generator
---------------
Produces CobolSharp IL:
- ILModule
- ILTypes
- ILMethods
- ILBasicBlocks
- ILInstructions

IL is:
- Backend‑agnostic but CIL‑friendly
- Fully typed
- Structured for optimization

6. Optimization Pipeline
------------------------
Applies:
- Control‑flow simplification
- Constant folding
- Constant propagation
- Copy propagation
- Dead code elimination
- Redundant move elimination
- Strength reduction
- Loop optimization
- Branch optimization
- Peephole optimization
- Generic specialization (optional)
- Data layout metadata optimization

Guarantees:
- Semantic preservation
- Verifiable IL
- Improved CIL emission quality

7. CIL Backend
--------------
Consumes optimized IL and emits:
- .NET types
- .NET methods
- .NET fields
- IL instructions
- Metadata
- PDB debug symbols

Responsibilities:
- Explicit layout for COBOL data structures
- REDEFINES overlays
- OCCURS DEPENDING ON dynamic bounds
- PERFORM lowering
- Exception region emission
- Calls into CobolSharp.Runtime

Outputs:
- .dll or .exe
- .pdb
- AOT‑ready assemblies

8. Runtime Library
------------------
Implements COBOL semantics in managed code:

- NumericEngine (packed decimal, arithmetic)
- FileManager (sequential, indexed, relative)
- SortMergeEngine
- JsonEngine
- XmlEngine
- StringEngine
- DateTimeEngine
- CollationEngine
- ExceptionEngine
- IntrinsicFunctionLibrary

Provides:
- Marshaling helpers
- StorageBlock management
- ExecutionContext

9. Debugger
-----------
Uses:
- .NET debugging APIs
- PDB sequence points
- Semantic model metadata

Supports:
- Breakpoints
- Step in/out/over
- Paragraph/section stepping
- PERFORM flow visualization
- Variable inspection (PIC/USAGE aware)
- OCCURS/REDEFINES visualization
- Memory inspection
- Expression evaluation

10. LSP/IDE Integration
-----------------------
Provides:
- Syntax highlighting
- Semantic highlighting
- Completion
- Hover
- Signature help
- Go‑to‑definition
- Find references
- Rename refactoring
- Code actions
- Diagnostics
- Debugger integration

11. Packaging & Distribution
----------------------------
Artifacts:
- cobolsharp.exe
- cobolsharp‑lsp.exe
- CobolSharp.Runtime.dll
- Tools (IL viewer, CFG viewer, data layout inspector)
- Templates
- Documentation

Distribution channels:
- NuGet
- ZIP/TAR archives
- Native installers
- Package managers
- dotnet tool install

Supports:
- Deterministic builds
- Code signing
- Reproducible artifacts

12. Modernization & Migration Toolkit
-------------------------------------
Provides:
- Codebase analysis
- Dependency graphs
- Data layout extraction
- Modernization advisor
- Automated refactoring engine
- Interop layer generation
- File format migration tools
- Modernization reports
- CI/CD modernization gate

Supports incremental modernization of legacy COBOL systems.

13. Interop Architecture (COBOL ↔ .NET)
---------------------------------------
COBOL → .NET:
- CALL "Namespace.Class::Method"
- INVOKE object::Method
- .NET constructors
- Properties
- Generics
- Async methods (via helpers)

.NET → COBOL:
- COBOL classes become .NET classes
- COBOL programs become callable entry points
- Marshaling metadata ensures type safety

Shared type system:
- PIC/USAGE → .NET types
- Group items → explicit layout classes
- OCCURS → arrays/lists
- REDEFINES → overlapping fields
- 88‑levels → enum‑like constants

End‑to‑End Developer Experience
-------------------------------
Workflow:
1. cobolsharp new  
2. Edit with LSP  
3. cobolsharp build  
4. cobolsharp run  
5. cobolsharp debug  
6. cobolsharp test  
7. cobolsharp publish  
8. Deploy to .NET environments  

Supports:
- CoreCLR
- .NET AOT
- .NET WASM (via dotnet publish)

Testing & Validation
--------------------
Test suite includes:
- Unit tests
- Golden tests
- Integration tests
- Cross‑compiler tests
- Fuzzing tests
- Performance tests
- Conformance tests
- Regression suite

Ensures:
- Correctness
- Stability
- Long‑term reliability

Summary
-------
The CobolSharp Master Architecture:
- Defines a complete, modern COBOL compiler targeting .NET CIL
- Provides a unified, modular, extensible system
- Ensures full COBOL semantics and .NET integration
- Supports modernization, debugging, refactoring, and CI/CD
- Produces deterministic, verifiable, cross‑platform .NET assemblies
- Is fully aligned with the CIL‑only execution model
