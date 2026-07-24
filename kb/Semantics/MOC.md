---
title: Semantics — Map of Content
area: semantics
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - semantics
  - moc
---

# 🗺 Semantics — Map of Content

How the bound tree is validated and how edition rules are enforced — the diagnostics layer of COBOL.NET.

## Notes in this domain

- [[kb/Semantics/Validation Rules]] — edition-invariant vs edition-conditional checks; FILE STATUS consistency; MOVE/category rules; the §8.9 reserved-word funnel; the strict/permissive severity seam; the diagnostic catalog & code bands.
- [[kb/Semantics/Passes]] — the `parse → bind → VersionConformancePass → FlagConformancePass → emit` order; the two-arm edition gate; the `EditionInfo` framework; why one pass not scattered checks.
- [[kb/Semantics/Version Gating & Reserved Words]] — the **decision record**: why one superset grammar + a pass + data tables, and why *not* per-rule predicates, lexer keyword-toggling, or four per-edition grammars (recognize-then-diagnose is the deciding axis).

## See also

- [[kb/Spec/Version Targeting]] — the four-editions model the gates enforce.
- [[kb/IR/MOC]] — the bound tree these passes walk.
- [[kb/Compiler/MOC]] — the frontend conformance mechanism.
- [[kb/Diagrams/Semantic Validation Flow]] — visual funnel.
- [[kb/Index]] — knowledge-base home.
