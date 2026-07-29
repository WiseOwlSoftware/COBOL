---
title: Diagrams — Map of Content
area: diagrams
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - diagram
  - moc
---

# 🗺 Diagrams — Map of Content

Visual models of the compiler — Mermaid flowcharts, ASCII sketches, and tables. (Mermaid renders natively in Obsidian.)

## Pipeline & structure
- [[kb/Diagrams/Compiler Pipeline Diagram]] — the 6-phase source→assembly flow; invariants (no lowered IR, edition-gate-halts-before-emit).
- [[kb/Diagrams/IR Graph Overview]] — the bound-node hierarchy, the `Place` lvalue, the source-generated visitor.
- [[kb/Diagrams/Semantic Validation Flow]] — the two-arm edition gate, the §8.9 reserved-word funnel, the diagnostic descriptor anatomy.

## Lookup diagrams (ISO cross-reference)
- [[kb/Diagrams/Grammar Hierarchy]] — program→division→section→paragraph→statement; the 9 grammar fragments.
- [[kb/Diagrams/IR Node Hierarchy]] — `[BoundNode]` roots→leaves; the source-generated exhaustive visitor.
- [[kb/Diagrams/Semantic Rule Flow]] — invariant vs edition-conditional rule routing to diagnostics.
- [[kb/Diagrams/Runtime Behavior Flow]] — emit→runtime call, the signal model, the numeric store funnel.

- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — per IR node family: ISO construct → semantic rule → runtime behavior, with ASCII flows (17 families, source-verified).

## See also

- [[kb/Compiler/MOC]] · [[kb/IR/MOC]] · [[kb/Semantics/MOC]] · [[kb/Runtime/MOC]]
- [[kb/Spec/Lookup/Index]] — the lookup tables these diagrams support.
- [[kb/Index]] — knowledge-base home.
