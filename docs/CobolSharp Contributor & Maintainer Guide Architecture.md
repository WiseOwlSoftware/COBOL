CobolSharp Contributor & Maintainer Guide Architecture (CIL‑Only)
=================================================================

Purpose
-------
Define the internal processes, standards, and workflows that ensure CobolSharp remains:
- Deterministic
- Maintainable
- Extensible
- Testable
- Architecturally coherent
- Fully aligned with the CIL‑only execution model

This guide establishes the rules and expectations for contributors and maintainers.

Contributor prerequisites
-------------------------
Contributors should be familiar with:
- COBOL (85 → 2023)
- .NET CIL and metadata
- Roslyn or System.Reflection.Metadata
- ANTLR grammars
- Compiler design (AST, CFG, SSA, IL)
- Debugging concepts (PDB, sequence points)
- Git and GitHub workflows

Optional but helpful:
- .NET AOT
- WASI/Blazor publish pipeline
- Packed decimal arithmetic
- File system internals (indexed/relative/sequential)

Repository structure
--------------------
/src  
  CobolSharp.Compiler/  
  CobolSharp.Runtime/  
  CobolSharp.LSP/  
  CobolSharp.Tools/  
  CobolSharp.Tests/  
  CobolSharp.Templates/  
/docs  
/scripts  
/build  
/.github  

Key directories:
- Compiler: Preprocessor → Lexer → Parser → Semantic → IL → Optimizer → CIL backend
- Runtime: NumericEngine, FileManager, SortMergeEngine, etc.
- LSP: Language server implementation
- Tools: IL viewer, CFG viewer, data layout inspector
- Tests: Unit, golden, integration, regression, conformance

Coding standards
----------------
Language:
- C# 12
- Nullable reference types enabled
- Async where appropriate (LSP, tools)
- No unsafe code unless explicitly justified

Style:
- PascalCase for types and methods
- camelCase for locals and parameters
- ALL_CAPS for constants
- No abbreviations except well‑known ones (IL, CFG, AST)
- Max line length: 120 chars
- Use expression‑bodied members when clear

Documentation:
- XML doc comments required for all public APIs
- Internal components require summary comments
- Complex algorithms require block comments

Error handling:
- Compiler errors → Diagnostics
- Runtime errors → ExceptionEngine
- No throwing exceptions inside compiler pipeline except catastrophic failures

Determinism requirements
------------------------
All compiler stages must be deterministic:
- No randomization
- No time‑dependent behavior
- No environment‑dependent behavior
- No nondeterministic ordering (use stable sorts)
- No parallelism inside semantic or IL generation

Parallelism is allowed only:
- In test harness
- In CIL emission (per method)
- In LSP request handling

Build & test workflow
---------------------
Before submitting a PR:
1. dotnet build  
2. dotnet test  
3. Run golden test updater (if needed)  
4. Run integration tests  
5. Run conformance suite (optional but recommended)  
6. Run static analysis (Roslyn analyzers)  
7. Run IL verification (peverify or ILVerify)  

CI enforces:
- Build success
- Test success
- No regressions
- No new warnings
- Deterministic output

Pull request requirements
-------------------------
Every PR must include:
- Clear description
- Linked issue (if applicable)
- Tests for new behavior
- Updated documentation (if applicable)
- No unrelated changes
- No formatting‑only diffs unless explicitly requested

PRs modifying:
- Grammar → require parser tests
- Semantic model → require semantic tests
- IL generator → require IL golden tests
- Optimizer → require before/after golden tests
- CIL backend → require CIL verification tests
- Runtime → require runtime tests
- LSP → require LSP protocol tests

Review guidelines
-----------------
Reviewers must check:
- Architectural consistency
- Determinism
- Test coverage
- Error handling correctness
- No semantic regressions
- No performance regressions
- No unnecessary complexity
- No duplication of logic

Performance considerations
--------------------------
Performance‑critical areas:
- Preprocessor
- Lexer
- Parser
- Semantic analyzer
- IL generator
- Optimizer
- Runtime numeric operations
- File I/O

Rules:
- Avoid allocations in hot paths
- Avoid LINQ in tight loops
- Avoid reflection in runtime
- Cache where safe
- Use spans where appropriate
- Benchmark before/after changes

Backward compatibility
----------------------
CobolSharp guarantees:
- Stable CLI interface
- Stable runtime API
- Stable LSP protocol
- Stable project configuration schema

Breaking changes require:
- Major version bump
- Migration guide
- Approval from maintainers

Testing philosophy
------------------
CobolSharp uses:
- Unit tests for correctness
- Golden tests for stability
- Integration tests for pipeline validation
- Conformance tests for ISO compliance
- Regression tests for bug prevention
- Cross‑compiler tests for behavioral alignment

Golden tests must be:
- Human‑readable
- Diff‑friendly
- Stable across platforms

Regression tests:
- Every fixed bug gets a test
- Tests must include minimal reproducer

Security guidelines
-------------------
- No arbitrary file execution
- No unsafe external calls
- COPY path sanitization
- No unbounded memory operations
- No unvalidated input to runtime engines
- No dynamic code generation outside CIL backend

Release process
---------------
1. Version bump  
2. Build artifacts  
3. Run full test suite  
4. Generate documentation  
5. Sign binaries  
6. Publish to NuGet  
7. Publish GitHub release  
8. Update website/docs  
9. Announce release  

Supported release channels:
- Stable
- Preview
- Nightly

Maintainer responsibilities
---------------------------
Maintainers must:
- Review PRs
- Enforce architectural consistency
- Maintain documentation
- Manage releases
- Triage issues
- Mentor contributors
- Ensure long‑term project health

Contributor onboarding
----------------------
New contributors should:
- Read this document
- Read subsystem architecture docs
- Build the project locally
- Run tests
- Fix a small issue or add a small feature
- Join discussions on GitHub

Summary
-------
The CobolSharp Contributor & Maintainer Guide:
- Defines coding standards, workflows, and expectations
- Ensures deterministic, maintainable, high‑quality contributions
- Provides a structured process for reviews, testing, and releases
- Supports long‑term evolution of the compiler
- Is fully aligned with the CIL‑only architecture
