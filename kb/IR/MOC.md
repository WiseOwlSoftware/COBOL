---
title: IR — Map of Content
area: ir
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - ir
  - moc
---

# 🗺 IR — Map of Content

The intermediate representation of COBOL.NET is the typed **bound tree** — there is **no separate lowered IR**.

## Notes in this domain

- [[kb/IR/Node Types]] — the bound tree as the single semantic model; `[BoundNode]` roots & leaves; the source-generated exhaustive visitor; the `Place` lvalue.
- [[kb/IR/Control Flow]] — the PC-dispatcher state machine (`__Dispatch`); GO TO / PERFORM THRU / ALTER / declaratives mapped onto a program counter.
- [[kb/IR/Data Flow]] — PIC→.NET type mapping, the two-phase `ReferenceResolver` → `Place`, the 4-tier REDEFINES/RENAMES model, whole-group images.

- [[kb/IR/Data-Flow-Traces]] — end-to-end traces of one `Place` through MOVE → arithmetic → file write (and group/table/REDEFINES).

## See also

- [[kb/Compiler/MOC]] — the pipeline that builds & consumes the bound tree.
- [[kb/Semantics/MOC]] — the passes that walk & gate it.
- [[kb/Runtime/MOC]] — the runtime the rendered tree calls.
- [[kb/Diagrams/IR Graph Overview]] — visual node hierarchy.
- [[kb/Index]] — knowledge-base home.
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — visualizes the path from each IR node to its semantic rules to its runtime behavior.
