CobolSharp Packaging & Distribution Architecture (CIL‑Only)
==========================================================

High‑level goals
----------------
- Provide a clean, modular, cross‑platform distribution model for a **CIL‑only** COBOL compiler.
- Package all components required to:
  - Compile COBOL to .NET assemblies
  - Run COBOL programs on .NET
  - Provide LSP/IDE integration
  - Support debugging via .NET PDBs
- Ensure deterministic builds, reproducible artifacts, and secure distribution.
- Remove all VM‑related artifacts; .NET IL is the single execution target.

Distribution artifacts
----------------------
CobolSharp produces the following artifacts:

1. cobolsharp.exe  
   - The main compiler CLI  
   - Includes:
     - Preprocessor
     - Lexer
     - Parser
     - Semantic analyzer
     - IL generator
     - Optimization pipeline
     - CIL backend

2. cobolsharp-lsp.exe  
   - Language Server Protocol implementation  
   - Used by VS Code, JetBrains, Visual Studio, Vim/Neovim

3. CobolSharp.Runtime.dll  
   - Runtime library for executing COBOL programs  
   - Contains:
     - NumericEngine
     - FileManager
     - SortMergeEngine
     - JsonEngine
     - XmlEngine
     - StringEngine
     - DateTimeEngine
     - CollationEngine
     - ExceptionEngine

4. CobolSharp.Compiler.Tasks.dll  
   - MSBuild integration (optional)  
   - Enables `<CobolSharpCompile>` tasks in .csproj/.cobproj

5. CobolSharp.Tools  
   - Optional utilities:
     - IL viewer
     - CFG visualizer
     - Data layout inspector
     - Copybook analyzer

6. Documentation bundle  
   - HTML docs
   - Man pages
   - API reference
   - Language reference (ISO/IEC 1989:2023 alignment notes)

7. Templates  
   - Project templates for:
     - COBOL console apps
     - COBOL class libraries
     - COBOL + .NET interop
     - COBOL AOT/native publish (via dotnet)

Directory layout
----------------
Standard distribution layout:

/cobolsharp/
  bin/
    cobolsharp.exe
    cobolsharp-lsp.exe
  lib/
    CobolSharp.Runtime.dll
    CobolSharp.Compiler.Tasks.dll
  tools/
    ilview.exe
    cfgview.exe
    datalayout.exe
  docs/
    index.html
    api/
    language/
  templates/
    console/
    classlib/
    aot/
  licenses/
    LICENSE.txt
    THIRD-PARTY-NOTICES.txt

Versioning strategy
-------------------
CobolSharp uses semantic versioning:

MAJOR.MINOR.PATCH

- MAJOR: Breaking changes (rare)
- MINOR: New features (e.g., new optimizations, new runtime features)
- PATCH: Bug fixes, performance improvements

Additionally:
- ISO/IEC 1989:2023 compliance level is tracked separately.
- Dialect support (85/2002/2014/2023) is versioned independently.

Build system
------------
Build pipeline:

1. Pre-build:
   - Generate parser/lexer from ANTLR grammar
   - Generate documentation from source comments
   - Generate templates

2. Build:
   - Compile compiler, runtime, tools, LSP
   - Run IL verification
   - Run static analysis

3. Test:
   - Unit tests
   - Golden tests
   - Integration tests
   - Regression tests
   - Conformance tests

4. Package:
   - Create NuGet packages
   - Create ZIP/TAR archives
   - Create native installers (optional)

5. Sign:
   - Code signing for executables and DLLs
   - Package signing for NuGet

6. Publish:
   - NuGet feed
   - GitHub Releases
   - Package repositories (optional)

NuGet packaging
---------------
CobolSharp ships as several NuGet packages:

1. CobolSharp.Compiler  
   - Contains cobolsharp.exe  
   - For build servers and CI pipelines

2. CobolSharp.Runtime  
   - Runtime library for executing COBOL programs

3. CobolSharp.Compiler.Tasks  
   - MSBuild integration

4. CobolSharp.LSP  
   - Language server

5. CobolSharp.Templates  
   - Project templates

Installation methods
--------------------
1. Standalone ZIP/TAR:
   - Unzip and run cobolsharp.exe
   - Cross‑platform

2. NuGet:
   - dotnet tool install --global CobolSharp.Compiler

3. Native installers:
   - Windows: MSI
   - macOS: PKG
   - Linux: DEB/RPM

4. Package managers:
   - Homebrew: brew install cobolsharp
   - Chocolatey: choco install cobolsharp
   - Scoop: scoop install cobolsharp

5. AOT/native distribution:
   - dotnet publish /p:PublishAot=true  
   - CobolSharp does not implement its own native backend; it relies on .NET AOT.

Reproducible builds
-------------------
CobolSharp supports deterministic builds:

- Fixed timestamps
- Locked dependency versions
- Reproducible ANTLR output
- Reproducible IL generation
- Hash-based build artifacts

This ensures:
- Build reproducibility
- Security auditing
- Long-term archival

Security considerations
-----------------------
- All binaries are code-signed
- COPY/REPLACE preprocessor is hardened against path traversal
- File I/O backend can be restricted or virtualized
- Optional “safe mode” disables:
  - CALL to external programs
  - File I/O
  - Environment access

Documentation system
--------------------
Documentation is generated from:

- XML doc comments
- Markdown sources
- ANTLR grammar
- Semantic model definitions

Output formats:
- HTML
- PDF (optional)
- Man pages
- VS Code help pages

Release channels
----------------
CobolSharp supports:

1. Stable channel  
   - Fully tested  
   - Recommended for production  

2. Preview channel  
   - New features  
   - May include experimental optimizations  

3. Nightly channel  
   - Latest commits  
   - For contributors and testers  

Summary
-------
The CobolSharp packaging & distribution architecture:
- Provides clean, modular, cross‑platform distribution
- Supports NuGet, standalone archives, installers, and package managers
- Ensures reproducible builds and strong versioning
- Ships compiler, runtime, LSP, tools, templates, and docs
- Enables easy installation, CI integration, and long‑term maintenance
- Is fully aligned with the CIL‑only execution model
