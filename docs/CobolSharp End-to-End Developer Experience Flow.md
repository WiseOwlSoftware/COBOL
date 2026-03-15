CobolSharp End‑to‑End Developer Experience Flow (CIL‑Only)
==========================================================

High‑level goals
----------------
- Provide a modern, intuitive, incremental development workflow for COBOL targeting .NET CIL.
- Integrate all major CobolSharp subsystems into a seamless experience:
  - Editing
  - Building
  - Running
  - Debugging
  - Testing
  - Packaging
  - Deployment
- Ensure the experience is consistent across:
  - Windows, macOS, Linux
  - VS Code, JetBrains, Visual Studio, Vim/Neovim
  - Local development, CI/CD, and production environments
- Guarantee that **.NET CIL is the single compilation and execution target**.

Developer workflow overview
---------------------------
The typical CobolSharp developer workflow:

1. Create a new project  
2. Edit COBOL source files  
3. Use LSP features (completion, hover, diagnostics)  
4. Build the project  
5. Run the program  
6. Debug the program  
7. Test the program  
8. Package and deploy  
9. Maintain and evolve the codebase  

Each step is supported by dedicated CobolSharp subsystems.

Step 1: Create a new project
----------------------------
Developers create a new project using:

  cobolsharp new console  
  cobolsharp new classlib  
  cobolsharp new aot  

Project structure:

/src  
  Program.cbl  
  Copybooks/  
  Classes/  
  Data/  
  Tests/  
/build  
/docs  
/cobolsharp.json (project configuration)

Project configuration includes:
- Dialect (85/2002/2014/2023)
- Optimization level
- CIL emission options
- Runtime settings
- COPYbook search paths

Step 2: Edit COBOL source files
-------------------------------
Developers write COBOL code in their editor of choice.

LSP provides:
- Syntax highlighting
- Semantic highlighting
- Code completion
- Hover info
- Signature help
- Go‑to‑definition
- Find references
- Rename refactoring
- Code actions
- Diagnostics

COPY/REPLACE expansion is shown inline or on demand.

Step 3: LSP‑driven development
------------------------------
As the developer types:

- Preprocessor expands COPY/REPLACE incrementally
- Lexer tokenizes expanded source
- Parser builds AST with error recovery
- Semantic analyzer updates symbol tables and types
- Diagnostics appear instantly
- Completion suggestions update in real time
- Hover shows PIC/USAGE/OCCURS/REDEFINES
- Outline view shows paragraphs, sections, classes, methods

All of this happens incrementally and efficiently.

Step 4: Build the project
-------------------------
Developer runs:

  cobolsharp build

Build pipeline:

1. Preprocess  
2. Lex  
3. Parse  
4. Semantic analysis  
5. IL generation  
6. Optimization pipeline  
7. CIL backend codegen  
8. Emit .NET assembly + PDB  

Outputs:
- Executable (.exe) or library (.dll)
- PDB debug symbols
- Diagnostics
- Build artifacts
- Optional AOT‑ready assemblies

Step 5: Run the program
-----------------------
Developer runs:

  cobolsharp run

Runtime:
- Loads generated .NET assembly
- Loads CobolSharp.Runtime.dll
- Initializes WORKING‑STORAGE, FILE SECTION, etc.
- Executes program under .NET
- Handles exceptions (INVALID KEY, AT END, etc.)
- Produces output

Execution environments:
- CoreCLR (default)
- .NET AOT (via dotnet publish)
- .NET WASM (via dotnet publish for WASI/Blazor)

CobolSharp does not implement its own WASM or native backend; it relies on .NET.

Step 6: Debug the program
-------------------------
Developer launches debugger:

  cobolsharp debug

Debugger features:
- Breakpoints (line, conditional, hit‑count)
- Step over / into / out
- Inspect variables (PIC/USAGE aware)
- Inspect OCCURS arrays
- Inspect REDEFINES overlays
- Inspect file buffers
- View call stack (paragraphs, sections, methods)
- Evaluate expressions
- View raw memory
- View IL disassembly

Debugger integrates with:
- .NET debugging APIs (ICorDebug, Diagnostics Protocol)
- PDB debug symbols
- CobolSharp semantic model

Step 7: Test the program
------------------------
Developer runs:

  cobolsharp test

Test harness supports:
- Unit tests
- Golden tests
- Integration tests
- Regression tests
- Conformance tests
- Cross‑compiler equivalence tests

Test output includes:
- Pass/fail
- Diagnostics
- Diff output for golden tests
- Coverage (optional)

Step 8: Package and deploy
--------------------------
Developer runs:

  cobolsharp publish

Deployment targets:
- .NET CoreCLR (default)
- .NET AOT (native)
- .NET WASM (WASI/Blazor)
- Cloud
- On‑prem
- Containers
- Batch processing environments

CobolSharp emits:
- .dll/.exe
- PDB
- Optional single‑file bundle
- Optional AOT‑compiled binary

Step 9: Maintain and evolve
---------------------------
CobolSharp supports:
- Refactoring tools
- Workspace‑wide symbol search
- COPYbook dependency graph
- Data layout visualization
- CFG visualization
- IL viewer
- Automatic formatting
- Code actions for modernization

Cross‑cutting developer experience features
-------------------------------------------

Incremental everything
----------------------
- Preprocessing
- Lexing
- Parsing
- Semantic analysis
- Diagnostics
- LSP features

Everything updates as the developer types.

Deep COBOL awareness
--------------------
CobolSharp understands:
- PIC/USAGE
- OCCURS
- REDEFINES
- RENAMES
- 88‑levels
- PERFORM flow
- COPY/REPLACE
- OO classes/methods
- Generics
- JSON/XML
- File I/O semantics

This enables:
- Accurate diagnostics
- Accurate refactoring
- Accurate debugging
- Accurate code actions

CIL‑only transparency
---------------------
Developers can choose deployment mode, but compilation always targets CIL.

Examples:
- dotnet run (CoreCLR)
- dotnet publish /p:PublishAot=true (native)
- dotnet publish -blazorwasm (WASM)

Reproducibility
---------------
- Deterministic builds
- Reproducible IL
- Reproducible PDBs
- Locked dependencies
- Build manifests

CI/CD integration
-----------------
CobolSharp integrates with:
- GitHub Actions
- Azure DevOps
- GitLab CI
- Jenkins
- TeamCity

CI pipeline:
- Build
- Test
- Package
- Publish
- Deploy

Summary
-------
The CobolSharp End‑to‑End Developer Experience Flow:
- Provides a modern, intuitive, incremental workflow
- Integrates editing, building, running, debugging, testing, and deployment
- Works across all .NET environments (CoreCLR, AOT, WASM)
- Leverages the full power of LSP, DAP, and the CobolSharp runtime
- Ensures COBOL development feels first‑class in a CIL‑only world
