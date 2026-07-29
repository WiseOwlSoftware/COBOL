---
title: COBOL.NET — Knowledge Base Index
area: index
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - index
---

# COBOL.NET — Knowledge Base
> ⚠️ **Role: derived navigational layer — NOT authoritative.** This vault is a paraphrased "second brain" over
> `docs/*` and the source. The canonical SSOTs are the repo docs (design → [[docs/COBOLNET_DESIGN]]; plan →
> [[docs/COBOLNET_REARCHITECTURE_PLAN]] §0; map → [[docs/DOC_INDEX]]). **When a note and a doc disagree, the doc
> wins.** No verbatim ISO/IEC 1989:2023 text lives here (it stays in the private [[specs/ISO_COBOL]] submodule).
> Notes are best-effort with a `last_updated` stamp — refresh, don't diverge. Tracking & docs-integration setup:
> [[kb/Context/Vault & Docs Integration]].

> A cross-linked "second brain" for the **CobolSharp / COBOL.NET** compiler: a commercial-quality,
> decades-sustainable COBOL compiler that translates standard COBOL into **idiomatic, typed-native C#**
> compiled by **Roslyn** into a .NET assembly. Built from the **ISO/IEC 1989:2023** specification with
> correct support for all prior editions (**1985 / 2002 / 2014**).

**Repo root:** `E:\CobolSharp\`  ·  **Design SSOT:** [[docs/COBOLNET_DESIGN]]  ·  **The one plan:** [[docs/COBOLNET_REARCHITECTURE_PLAN]] §0

---

## ⭐ Start here

1. [[kb/Context/Goals]] — the North Star and non-negotiable process rules.
2. [[kb/Architecture/High-Level Design]] — what the compiler *is* and its locked invariants.
3. [[kb/Compiler/Pipeline]] — how a `.cob` file becomes a running `.dll`.
4. [[kb/Diagrams/Compiler Pipeline Diagram]] — the pipeline at a glance.
5. [[kb/Search/Glossary]] — vocabulary of the project.

6. [[kb/Spec/Lookup/Index]] — the **ISO COBOL lookup** system (keyword → grammar → rule → IR → runtime).

## 🗺 Maps of Content (MOCs)

| Domain | Hub | Covers |
|---|---|---|
| Spec | [[kb/Spec/MOC]] | ISO/IEC 1989:2023, language surface, editions, conformance |
| Compiler | [[kb/Compiler/MOC]] | Pipeline phases, ANTLR frontend, build system |
| Architecture | [[kb/Architecture/MOC]] | High-level design, module/assembly topology |
| IR | [[kb/IR/MOC]] | Bound tree (no lowered IR), control flow, data flow |
| Semantics | [[kb/Semantics/MOC]] | Validation rules, semantic passes, edition gating |
| Runtime | [[kb/Runtime/MOC]] | Typed-native runtime kernel + execution model |
| Modernization | [[kb/Modernization/MOC]] | Rearchitecture plan, conformance queue, audits |
| Context | [[kb/Context/MOC]] | Project history, goals, doctrine |
| Diagrams | [[kb/Diagrams/MOC]] | Pipeline / IR / validation visualizations |
| Search | [[kb/Search/MOC]] | Key concepts, glossary, FAQ |

## 📌 The one-paragraph model

A COBOL **record IS a .NET `record struct`**; an **elementary item IS a native field**; a **program IS a class**.
There is **no byte-array storage substrate** — fixed-point numerics are native `long`/`Int128` holding the *unscaled*
value with scale as compile-time metadata. Source flows through **preprocess → lex → parse → bind → version-conformance
gate → desugar → backend**, producing a single typed **bound tree** (there is **no separate lowered IR**) that a
selectable `ICodeGenBackend` (**Roslyn C# primary**, CIL future) renders. See [[kb/Architecture/High-Level Design]].

## 🔢 Status snapshot (as of 2026-07-23)

- ~3166 conformance tests · 281 unit tests · 33 characterization tests passing; 353 NIST programs match byte-for-byte.
- Greenfield build order **G0–G6 complete**, **G7 (per-edition correctness) in progress**; legacy cut-over is G8.
- Active arc: branch `phase-14`, the spec-first **CONFORMANCE-FIX-QUEUE** campaign (30 landed / 16 remain).
- See [[kb/Modernization/Tasks]] and [[kb/Context/Project History]].

## 🧭 Major notes

- Spec: [[kb/Spec/Overview]] · [[kb/Spec/Language Features]] · [[kb/Spec/Version Targeting]] · [[kb/Spec/Constraints]]
- Compiler: [[kb/Compiler/Pipeline]] · [[kb/Compiler/Phases]] · [[kb/Compiler/Build System]]
- Architecture: [[kb/Architecture/High-Level Design]] · [[kb/Architecture/Module Overview]]
- IR: [[kb/IR/Node Types]] · [[kb/IR/Control Flow]] · [[kb/IR/Data Flow]]
- Semantics: [[kb/Semantics/Validation Rules]] · [[kb/Semantics/Passes]]
- Runtime: [[kb/Runtime/Execution Model]]
- Modernization: [[kb/Modernization/Tasks]] · [[kb/Modernization/Audit Artifacts]]
- Context: [[kb/Context/Project History]] · [[kb/Context/Goals]]
- Diagrams: [[kb/Diagrams/Compiler Pipeline Diagram]] · [[kb/Diagrams/IR Graph Overview]] · [[kb/Diagrams/Semantic Validation Flow]]
- Search: [[kb/Search/Key Concepts]] · [[kb/Search/Glossary]] · [[kb/Search/Frequently Asked Questions]]
- **ISO Lookup** (`Spec/Lookup/`): [[kb/Spec/Lookup/Index]] · [[kb/Spec/Lookup/Keywords]] · [[kb/Spec/Lookup/Grammar]] · [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Spec/Lookup/IR Mapping]] · [[kb/Spec/Lookup/Runtime Mapping]] · [[kb/Spec/Lookup/Constraints]]
- Lookup diagrams: [[kb/Diagrams/Grammar Hierarchy]] · [[kb/Diagrams/IR Node Hierarchy]] · [[kb/Diagrams/Semantic Rule Flow]] · [[kb/Diagrams/Runtime Behavior Flow]]
- **Bridge**: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — every compiler phase → the ISO COBOL constructs, rules, and constraints it implements or enforces.
- **IR flow**: [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — for each IR node family, the path from ISO construct → semantic rule → runtime behavior.
- **Reverse indexes & trackers**: [[kb/Compiler/ISO-Clause-to-Phase]] · [[kb/Runtime/Runtime-Class-to-IR]] · [[kb/Spec/Lookup/Diagnostics]] · [[kb/IR/Data-Flow-Traces]] · [[Remaining Work Tracker]]
