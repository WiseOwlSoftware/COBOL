---
title: Spec — Map of Content
area: spec
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - moc
  - spec
---

# 🗺 Spec — Map of Content

The ISO/IEC 1989:2023 specification and how COBOL.NET targets it. The spec ([[specs/ISO_COBOL]]) is the **single
authority** for all syntax, semantics, and behavior.

## Notes in this domain

- [[kb/Spec/Overview]] — what ISO/IEC 1989:2023 is, its 16 clauses + annexes, the §4.2.16 conformance record, the four-editions mission.
- [[kb/Spec/Language Features]] — the implemented language surface by division + intrinsics + OO/post-85.
- [[kb/Spec/Version Targeting]] — "four compilers in one," edition gating (COBOLNET0900–0903), the VCR checklist, the (construct × edition) test matrix, the GnuCOBOL register.
- [[kb/Spec/Constraints]] — conformance clause §4, reference format §6, the §7 directive facility, documented non-support.


### 🔎 ISO COBOL Lookup subsystem (`Spec/Lookup/`)
A lookup & cross-reference layer — from a keyword/construct/rule/IR node/runtime behavior/constraint, jump across
Spec → Compiler → IR → Semantics → Runtime.
- [[kb/Spec/Lookup/Index]] — the master lookup index + cross-domain map.
- [[kb/Spec/Lookup/Keywords]] — curated keyword cross-reference.
- [[kb/Spec/Lookup/Grammar]] — divisions/sections/statements/expressions/data types.
- [[kb/Spec/Lookup/Semantic Rules]] — rule → validation pass → IR → runtime.
- [[kb/Spec/Lookup/IR Mapping]] — every `Bound*` node → spec/rule/phase/diagram.
- [[kb/Spec/Lookup/Runtime Mapping]] — runtime behavior → spec/IR/execution model.
- [[kb/Spec/Lookup/Constraints]] — language limits + engineering doctrine.

- [[kb/Spec/Lookup/Diagnostics]] — the diagnostic-code lookup (`COBOLNET####` → meaning / § / phase / construct).

## See also

- [[kb/Semantics/MOC]] — how spec rules become enforced diagnostics & edition gates.
- [[kb/Runtime/MOC]] — how spec behavior executes at runtime.
- [[kb/Modernization/MOC]] — the spec-first conformance campaign and audits.
- [[kb/Context/Goals]] — the spec-is-authority process rule.
- [[kb/Index]] — knowledge-base home.
- [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the bridge from each compiler phase to the ISO constructs/rules it handles.
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — IR node → ISO construct → semantic rule → runtime behavior, per node family.
