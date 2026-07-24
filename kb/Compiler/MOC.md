---
title: Compiler — Map of Content
area: compiler
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - moc
  - compiler
---

# 🗺 Compiler — Map of Content

How a `.cob` file becomes a running `.dll`: the 6-phase pipeline, its ANTLR-based frontend, and the build machinery.

## Notes in this domain

- [[kb/Compiler/Pipeline]] — the 6 phases end-to-end; no lowered IR; the loud-failure invariant.
- [[kb/Compiler/Phases]] — frontend internals: preprocessor, 4-mode lexer, superset grammar (9 fragments), CST, version-conformance mechanism.
- [[kb/Compiler/Build System]] — .NET 10 / C# 14, central package management, ANTLR regen, guard/gen scripts.

- [[kb/Compiler/Pipeline-to-ISO-Mapping]] — **the bridge note**: for every phase, exactly which ISO COBOL constructs, rules, and constraints it implements or enforces (with IR-node and semantic-rule links).

- [[kb/Compiler/ISO-Clause-to-Phase]] — reverse index: ISO clause → the phase(s) that implement/enforce it.

## See also

- [[kb/Architecture/MOC]] — the assemblies the pipeline lives in.
- [[kb/IR/MOC]] — the bound tree the pipeline produces.
- [[kb/Semantics/MOC]] — the bind & conformance passes.
- [[kb/Diagrams/Compiler Pipeline Diagram]] — visual pipeline.
- [[kb/Index]] — knowledge-base home.
