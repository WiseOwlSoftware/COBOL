CobolSharp Packaging & Distribution Architecture (CIL‑Only)
==========================================================

> **STATUS BANNER — DESIGN REFERENCE, NOT YET IMPLEMENTED.**
> This is a **TARGET design** for the CobolSharp packaging / build / release / distribution
> surface. **Actual implementation status: DESIGN-ONLY (~0–5%).** Today the product builds with a
> plain `dotnet build` / `dotnet test` against `CobolSharp.sln`; the iteration loop is
> `scripts/guard-fast.sh` (+ `guard.sh` / `guard-verify.sh`). The only release-pipeline asset that
> exists is a **disabled** GitHub Actions workflow `.github/workflows/build-and-test.yml.disabled`
> (already on `.NET 10`, builds Release, runs unit + integration tests). NuGet packages, standalone
> archives, native installers, package-manager recipes, signing/SBOM/provenance, multi-channel
> releases, the documentation pipeline, and the WASM/AOT distribution bundles described below **do
> not exist yet**. This work lives in **Phase E (production hardening)** and the final rename phase
> of the plan.
>
> **CURRENT TRUTH (2026-06-07, DEVLOG 441):**
> - **Stack:** **.NET 10 / C# 14** (`<TargetFramework>net10.0</TargetFramework>`, `<LangVersion>14</LangVersion>`).
>   Any `net8.0` / `net9.0` / "C# 13" target moniker below is **stale** — read it as `net10.0`.
> - **Backend:** **CIL-only via Mono.Cecil** (`CobolSharp.Compiler` emits a .NET assembly with Cecil).
>   There is **NO custom VM**, **NO bytecode interpreter**, and **NO WASM / LLVM / VM backend**. The
>   Roslyn C# backend is a **future additive Stage-5** (Cecil stays the oracle). Treat every "VM
>   backend / VM bytecode / LLVM backend / WASM backend" mention as aspirational/removed.
> - **Pointers:** ONE `ManagedPointer` (no 8-byte handle, no PointerRegistry; the former
>   `CobolDataPointer` was renamed). GC-tracked managed refs, no native heap, no `unsafe`.
> - **Data model:** migrating to typed-native (CORE done through Stage-4; `EnableTypedFields`
>   default OFF; byte engine being islanded).
> - **Code organization:** already decomposed — NOT god classes (M001 IrExpression, M003
>   `CilEmitter` → 11 emitters, M004 `BoundTreeBuilder` → 9 binders).
> - **Conformance:** M1 (COBOL-85) COMPLETE; guard **1196 unit / 509 integration / 364 NIST** green.
> - **Real artifact/assembly names today:** CLI exe assembly = **`cobolsharp`** (`src/CobolSharp.CLI`),
>   libraries = **`CobolSharp.Compiler`** + **`CobolSharp.Runtime`**. The end-state rename is
>   `CobolSharp` → **COBOL.NET** with the produced executable named **`cobol.exe`** (final phase).
> - **Plan SSOT:** `docs/MASTER_PLAN.md` (this surface = Phase E + final rename). Doctrine: `PROMPT.md`.
>
> _Consolidated from 4 prior docs, 2026-06-07:_ "CobolSharp Packaging & Distribution Architecture",
> "CobolSharp Build & Release Pipeline Architecture", "CobolSharp Build System — Project Structure,
> Multi‑Targeting, AOT-WASM Pipelines & Deterministic Packaging", and "CobolSharp Distribution
> Architecture — Packaging, Versioning, Backward Compatibility & Long‑Term Stability".

High‑level goals
----------------
- Provide a clean, modular, cross‑platform distribution model for a **CIL‑only** COBOL compiler.
- Package all components required to:
  - Compile COBOL to .NET assemblies (via Mono.Cecil)
  - Run COBOL programs on .NET
  - Provide LSP/IDE integration
  - Support debugging via .NET PDBs
- Ensure deterministic builds, reproducible artifacts, and secure distribution.
- .NET CIL is the single execution target. (All VM/LLVM/WASM-backend artifacts from earlier drafts
  are removed; the optional WASM/AOT *publish* paths below rely on the standard .NET toolchain, not
  on a CobolSharp-specific backend.)

Distribution artifacts
----------------------
CobolSharp produces the following artifacts (TARGET — names normalized to the real CIL-only build):

1. `cobolsharp` (CLI; final name `cobol.exe`)
   - The main compiler CLI
   - Includes:
     - Preprocessor
     - Lexer
     - Parser
     - Semantic analyzer / Binder
     - IL generator (Mono.Cecil CIL backend)
     - Optimization pipeline
     - CIL backend

2. `cobolsharp-lsp` (LSP server — design-only)
   - Language Server Protocol implementation
   - Used by VS Code, JetBrains, Visual Studio, Vim/Neovim

3. `CobolSharp.Runtime.dll`
   - Runtime library for executing COBOL programs.
   - Design-level engine roster (note: the SHIPPING runtime today exposes these as concrete files
     such as `FileRuntime`, `SortRuntime`, `InspectRuntime`, `ReportWriterRuntime`, `Numeric/*`,
     `Text/*`, `ManagedPointer`, `StorageArea`, `CobolProgramRegistry` — the names below are the
     design-doc grouping, ~70–90% present):
     - NumericEngine
     - FileManager
     - SortMergeEngine
     - JsonEngine
     - XmlEngine
     - StringEngine
     - DateTimeEngine
     - CollationEngine
     - ExceptionEngine

4. `CobolSharp.Compiler.Tasks.dll` (MSBuild integration — design-only)
   - Enables `<CobolSharpCompile>` tasks in .csproj/.cobproj

5. `CobolSharp.Tools` (optional utilities — design-only)
   - IL viewer
   - CFG visualizer
   - Data layout inspector
   - Copybook analyzer

6. Documentation bundle (design-only)
   - HTML docs
   - Man pages
   - API reference
   - Language reference (ISO/IEC 1989:2023 alignment notes)

7. Templates (design-only)
   - Project templates for:
     - COBOL console apps
     - COBOL class libraries
     - COBOL + .NET interop
     - COBOL AOT/native publish (via `dotnet`)

Directory layout
----------------
Standard distribution layout (TARGET):

```
/cobolsharp/
  bin/
    cobolsharp          (final: cobol)
    cobolsharp-lsp
  lib/
    CobolSharp.Runtime.dll
    CobolSharp.Compiler.Tasks.dll
  tools/
    ilview
    cfgview
    datalayout
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
```

Versioning strategy
-------------------
CobolSharp uses semantic versioning: `MAJOR.MINOR.PATCH`

- **MAJOR:** Breaking changes (rare); new language features requiring a new runtime.
- **MINOR:** New features (new intrinsics, new optimizations, backward‑compatible runtime features).
- **PATCH:** Bug fixes, performance improvements, no behavior changes.

Additionally:
- ISO/IEC 1989:2023 compliance level is tracked separately.
- Dialect support (85/2002/2014/2023) is versioned independently.

### Compiler / runtime / standard-library lockstep
*(from the Distribution Architecture draft, §2.2–2.3)*
- Compiler version `X.Y.Z` requires runtime version `X.Y.*`.
- Standard-library version matches the compiler & runtime major/minor.

Build system
------------
TARGET pipeline (the SHIPPING build today is just `dotnet build` + `scripts/guard*.sh`):

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
   - Conformance tests (ISO/IEC 1989:2023) + NIST CCVS suites

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
CobolSharp ships as several NuGet packages (TARGET):

1. `CobolSharp.Compiler` — contains the `cobolsharp` CLI; for build servers and CI pipelines.
2. `CobolSharp.Runtime` — runtime library for executing COBOL programs.
3. `CobolSharp.Compiler.Tasks` — MSBuild integration.
4. `CobolSharp.LSP` — language server.
5. `CobolSharp.Templates` — project templates.

Installation methods
--------------------
1. **Standalone ZIP/TAR** — unzip and run `cobolsharp` (cross‑platform).
2. **NuGet** — `dotnet tool install --global CobolSharp.Compiler`.
3. **Native installers** — Windows MSI, macOS PKG, Linux DEB/RPM.
4. **Package managers** — `brew install cobolsharp`, `choco install cobolsharp`, `scoop install cobolsharp`.
5. **AOT/native distribution** — `dotnet publish /p:PublishAot=true`. CobolSharp does **not** implement
   its own native backend; it relies on .NET AOT.

Reproducible builds
-------------------
CobolSharp supports deterministic builds:
- Fixed timestamps
- Locked dependency versions
- Reproducible ANTLR output
- Reproducible IL generation
- Hash-based build artifacts

This ensures build reproducibility, security auditing, and long-term archival.

Security considerations
-----------------------
- All binaries are code-signed.
- COPY/REPLACE preprocessor is hardened against path traversal.
- File I/O backend can be restricted or virtualized.
- Optional "safe mode" disables CALL to external programs, File I/O, and environment access.

### Supply-chain security (from the Build & Release Pipeline draft)
- Supply-chain security target: **SLSA level 3+** with provenance metadata.
- SBOM (Software Bill of Materials) generated per release.
- Dependency scanning + vulnerability scanning.
- Static analysis (Roslyn analyzers, warnings-as-errors).

Documentation system
--------------------
Documentation is generated from:
- XML doc comments
- Markdown sources
- ANTLR grammar
- Semantic model definitions

Output formats: HTML, PDF (optional), Man pages, VS Code help pages.

Release channels
----------------
1. **Stable** — fully tested; recommended for production.
2. **Preview** — new features; may include experimental optimizations.
3. **Nightly** — latest commits; for contributors and testers.

Each channel has its own versioning rules, stability guarantees, and update cadence.

------------------------------------------------------------
APPENDIX A — RELEASE PIPELINE STAGES (from "Build & Release Pipeline Architecture")
------------------------------------------------------------

> Note: the original draft listed CIL / WASM / LLVM / VM as parallel backends and a "VM bytecode
> interpreter bundle". **Those backends do not exist** — CobolSharp is CIL-only via Mono.Cecil.
> The stage structure below is retained as the TARGET release-automation shape; backend-specific
> bullets are scoped to CIL (+ optional `dotnet`-driven AOT/WASM publish).

The end-to-end release pipeline is eight deterministic, isolated stages:

1. **Source acquisition** — pull from Git (main/release branch); verify commit signatures
   (GPG / GitHub-verified); resolve submodules; lock dependency versions (NuGet lock files);
   generate a build manifest (commit hash, build timestamp, version, targets, configuration).
2. **Pre-build generation** — generate ANTLR lexer/parser; generate docs from XML comments +
   Markdown; generate project templates; generate runtime metadata tables; validate grammar
   consistency (no left-recursion / ambiguity); validate semantic-model schemas.
3. **Build** — build all components in dependency order:
   - Core libraries (`CobolSharp.Compiler`, `CobolSharp.Runtime`, and their sub-namespaces:
     AST, Semantics/Binder, IL, Runtime).
   - Compiler (`cobolsharp` CLI): preprocessor, lexer, parser, semantic analyzer, IL generator,
     optimization pipeline, CIL backend.
   - Tools (IL viewer, CFG viewer, data-layout inspector, copybook analyzer).
   - LSP server.
   - Debugger (debug adapter; optional UI).
   - Documentation (HTML docs, API reference, language reference).
   Build characteristics: deterministic compilation, reproducible IL, warnings-as-errors, static
   analysis, optional coverage instrumentation.
4. **Test** — full suite: unit, golden, integration, cross-compiler (GnuCOBOL/Micro Focus),
   fuzzing, performance, conformance (ISO/IEC 1989:2023), regression. Results are stored as
   artifacts, published to a CI dashboard, and gate the release.
5. **Package** — NuGet packages, standalone archives
   (`cobolsharp-win-x64.zip`, `cobolsharp-linux-x64.tar.gz`, `cobolsharp-macos-arm64.tar.gz`),
   native installers (MSI / PKG / DEB / RPM), and a documentation bundle (HTML / PDF / man pages).
6. **Sign** — code-sign executables/DLLs; NuGet package signing; platform installer signing;
   generate SBOM + SLSA-3+ provenance.
7. **Publish** — NuGet.org, GitHub Releases, package repositories (Homebrew/Chocolatey/Scoop),
   optional internal feeds, documentation website, optional Docker images. Channels: Stable /
   Preview / Nightly.
8. **Post-release validation** — cross-platform smoke tests; LSP tested in VS Code + JetBrains;
   debugger breakpoint/stepping tests; performance benchmarks; optional telemetry/crash analysis.

### Cross-cutting concerns
- **Reproducibility** — deterministic builds; manifests stored with artifacts; published hashes;
  reproducible ANTLR + IL output.
- **Scalability** — parallel builds, distributed test execution, caching of preprocessor expansions,
  incremental documentation builds.
- **Developer-workflow integration** — PR/branch/release builds, automated changelog generation,
  automated version bumping.

### Existing asset
The repo already contains `.github/workflows/build-and-test.yml.disabled` — a matrix
(`ubuntu-latest`, `windows-latest`) workflow that sets up **.NET 10**, restores, builds Release, and
runs the unit + integration test projects. Enabling/expanding this is the first concrete step of
this appendix.

------------------------------------------------------------
APPENDIX B — BUILD SYSTEM INTERNALS (from "Build System — Project Structure, Multi-Targeting…")
------------------------------------------------------------

> Stale notes corrected: target frameworks are **`net10.0`** (the draft said `net8.0`); there is **no
> custom VM** and **no parallel WASM/AOT backend** — WASM/AOT are standard-`dotnet` publish targets,
> not CobolSharp backends. Deterministic-emission, registry, and PDB design below is the real intent.

### B.1 Project / source layout (a *compiled COBOL* project)
- `src/` — `*.cbl` sources, `*.cpy` copybooks.
- `obj/` — intermediate AST/CFG/DFG, semantic metadata, IL fragments.
- `bin/` — final assemblies, (optional) WASM bundles, (optional) AOT artifacts.

Multi-module projects support multiple programs per project, multiple ENTRY points per program,
shared copybooks, and shared COMMON programs.

### B.2 Multi-targeting
- `net10.0` (CoreCLR) — the primary target.
- `net10.0`-AOT (Native AOT via `dotnet publish /p:PublishAot=true`) — optional.
- `net10.0`-WASM (WASM AOT via the .NET WASM toolchain) — optional.

Cross-target consistency guarantees: identical semantics, identical numeric behavior, identical
file-I/O semantics (WASM uses a virtual FS).

### B.3 Deterministic IL generation & assembly emission
- Emit **verifiable CIL** with deterministic method / field / type ordering (via Mono.Cecil).
- Deterministic metadata writer, stable GUIDs, stable MVID (unless source changes).
- **PDB** carries sequence points, local-variable names, paragraph/method names, and
  StorageBlock/StorageArea field names.

### B.4 Program registry generation (REAL — `CobolProgramRegistry`)
- Maps program name → .NET type, ENTRY name → method, COMMON/INITIAL flags, metadata hashes.
- Deterministically ordered (alphabetically by program name, then ENTRY name).
- Used instead of `Type.GetType` / assembly scanning / runtime reflection (AOT-friendly).

### B.5 AOT pipeline (optional, via .NET AOT)
- Emit IL + metadata using AOT-friendly patterns; the .NET AOT toolchain produces the native binary.
- AOT constraints: no reflection, no dynamic codegen, no runtime type discovery, no dynamic assembly
  loading.
- Link-time trimming: all ENTRY methods, all DECLARATIVES, and all subsystems are marked as roots;
  everything else is trimmed.

### B.6 WASM pipeline (optional, via .NET WASM AOT)
- Emit WASM-safe IL + metadata; the .NET WASM toolchain produces the `.wasm` binary, JS glue, and a
  virtual FS.
- WASM host integration: ConsoleEngine → JS console; FileManager → virtual FS; timers → JS timers;
  async interop → JS promises.
- WASM output bundle: `index.html`, `runtime.js`, `dotnet.wasm`, `app.wasm`, `ProgramRegistry.json`.

### B.7 Deterministic packaging & build hash
- Same source → same IL → same AOT/WASM output; same metadata + PDB ordering.
- Build hash = SHA-256 of source tree + SHA-256 of metadata + SHA-256 of IL.
- Build manifest contains target frameworks, build hash, program registry, and file list.

### B.8 Copybook handling
- Copybooks expanded at parse time with stable ordering + stable whitespace normalization.
- Compiler caches AST fragments + semantic metadata.

### B.9 Build error / warning catalog
- **Errors:** lexical, syntax, semantic, PIC mismatch, file-definition, OO-inheritance,
  AOT/WASM incompatibility.
- **Warnings:** unused variable, unreachable code, implicit conversion, truncation.

### B.10 Debugger support
- PDB mapping lets a debugger show COBOL source, IL, StorageBlocks/StorageAreas, and ExecutionContext.
- WASM debugging via source maps + PDB → WASM mapping.

### B.11 Edge-case build behavior
- Copybook redefinition → compile-time error.
- Duplicate program name → compile-time error.
- ENTRY-name collision → compile-time error.
- AOT-unsafe intrinsic → compile-time error.
- WASM-unsafe file path → runtime error.

------------------------------------------------------------
APPENDIX C — VERSIONING, COMPATIBILITY & LONG-TERM STABILITY (from "Distribution Architecture")
------------------------------------------------------------

### C.1 Backward-compatibility guarantees
Programs compiled with version `X.Y` run on runtime `X.Y+N`, with **no breaking changes** in:
numeric semantics, file-I/O semantics, JSON/XML semantics, SORT semantics, REPORT semantics,
StorageBlock/StorageArea layout rules, and ExceptionState categories.
- **IL stability** — generated IL is stable across patch versions and across minor versions (unless
  new features are used).
- **Metadata stability** — metadata tables remain sorted, deterministic, backward-compatible.
- **ProgramRegistry stability** — versioned, backward-compatible, forward-compatible (unknown fields
  ignored).

### C.2 Forward-compatibility rules
- Programs compiled with an older compiler run on a newer runtime.
- Programs compiled with a newer compiler may require a newer runtime.
- Forward-compatibility constraints (a program built for an older runtime must not use): new PIC
  categories, new numeric types, new file organizations, new declarative types.

### C.3 Strong-naming & assembly identity
- All CobolSharp assemblies are strong-named, versioned, and signed.
- Assembly identity = name + version + public-key token.
- Assemblies include compiler metadata, ProgramRegistry, sequence points, deterministic GUIDs.

### C.4 WASM / AOT distribution packaging
- **WASM bundle:** `dotnet.wasm`, `runtime.js`, `app.wasm`, `ProgramRegistry.json`, `index.html`,
  virtual-FS seed. WASM runtime version must match the compiler & runtime major/minor; guaranteed
  stable: virtual-FS format, JSON/XML behavior, numeric behavior, event-loop behavior.
- **AOT bundle:** native binary, metadata tables, ProgramRegistry, optional debug symbols. AOT
  runtime version must match the compiler major/minor; guaranteed stable: numeric semantics,
  file-I/O semantics, StorageBlock layout.

### C.5 Migration & upgrade rules
- **Minor upgrade** — safe; no code changes, no rebuild (runtime only).
- **Patch upgrade** — always safe.
- **Major upgrade** — requires rebuild, release-notes review, and possible migration of deprecated
  features.

### C.6 Deprecation model
- Stages: (1) Warning → (2) Disabled by default → (3) Removed in next major version.
- Categories: intrinsics, compiler directives, obsolete syntax, runtime APIs.

### C.7 Long-term stability model
- **Goals:** a 10-year backward-compatibility window; no silent behavior changes; no nondeterministic
  changes.
- **Stable domains:** numeric semantics, file I/O, JSON/XML, SORT, REPORT, StorageBlock layout.
- **Evolving domains:** optimizations, debugger features, WASM host integration.

### C.8 Version edge cases
- Runtime older than compiler → runtime error `INCOMPATIBLE RUNTIME VERSION`.
- Compiler older than runtime → allowed.
- WASM bundle missing ProgramRegistry → runtime error.
- AOT binary missing metadata → runtime error.
- Deprecated feature used → compiler warning.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp packaging & distribution architecture (TARGET, Phase E + final rename):
- Provides clean, modular, cross‑platform distribution, fully aligned with the **CIL‑only** (Mono.Cecil)
  execution model on **.NET 10 / C# 14**.
- Supports NuGet, standalone archives, installers, and package managers; ships compiler, runtime,
  LSP, tools, templates, and docs.
- Ensures deterministic, reproducible builds, strong semantic versioning, a 10-year backward-compat
  window, and an 8-stage signed/SBOM'd release pipeline.
- Today, none of this is built: the live build is `dotnet build` + `scripts/guard-fast.sh`, with one
  disabled CI workflow (`.github/workflows/build-and-test.yml.disabled`, already on .NET 10) as the
  seed. See `docs/MASTER_PLAN.md` (Phase E) for sequencing.
</content>
</invoke>
