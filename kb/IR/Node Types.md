---
title: IR — Bound Tree Node Types
area: ir
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs
  - src/Cobol.Net.Compiler.SourceGen/BoundVisitorGenerator.cs
  - src/Cobol.Net.Compiler/Binding/Model/Place.cs
  - docs/rearchitecture/DESIGN-binder-bound-tree.md
tags:
  - cobolsharp
  - ir
---

# IR — Bound Tree Node Types

> The COBOL.NET compiler has **no separate lowered IR**. The "IR" *is* the typed **bound semantic tree** produced by
> the binder and defined in `Binding/Bound/BoundTree.cs`.

It is the single backend-neutral source of truth: the binder resolves every reference to a `Place`, every literal to
typed text, and every condition/expression to a bound node **once**, so the backend (and the PC dispatcher) walk this
tree without ever re-touching the ANTLR parse tree. No bound node holds a raw parse context. The owner-locked decision
(`COBOLNET_DESIGN` §1.1 / §18.23) keeps this: the node carries the fully-resolved classification
(e.g. `BoundMove.Kind`, `StorageForm`) so no consumer re-derives semantics at emit. See
[[kb/Architecture/High-Level Design]].

## Categories (each abstract root is `[BoundNode]`)
- **`BoundExpr`** — numeric expressions: `BoundNumLiteral`, `BoundNumRef`, `BoundBinary`, `BoundNegate`, `BoundPower`,
  `BoundIndexRef`, `BoundIntrinsicCall`, `BoundExprError`.
- **`BoundOperand`** — string/DISPLAY operands: `BoundStringLiteral`, `BoundFieldOperand`, `BoundFigurative`,
  `BoundAllLiteral`, `BoundComputedOperand`, `BoundOperandError`.
- **`BoundBoolExpr`** — 2002 boolean/bit expressions: `BoundBoolRef`, `BoundBoolBinary`, `BoundBoolNot`,
  `BoundBoolShift`, `BoundBoolError`.
- **`BoundCondition`** — conditions: `BoundRelational`, `BoundLogical`, `BoundNot`, `BoundCondition88`,
  `BoundRangeMembership`, `BoundSignCondition`, `BoundClassCondition`, `BoundConditionError`.
- **`BoundStatement`** — ~130 leaves: `BoundMove`, `BoundIf`, `BoundDisplay`, `BoundCompute`, `BoundAddGiving`,
  `BoundGoTo`, `BoundGoToDepending`, `BoundInlinePerform`, `BoundOutOfLinePerform`, `BoundStop`, `BoundGoback`,
  `BoundCallProgram`, `BoundSearch`, `BoundSort`, `BoundStringStmt`, `BoundInspect`, `BoundInitialize`,
  `BoundOpen`/`BoundRead`/`BoundWrite`/`BoundRewrite`, `BoundKeyedRead`, `BoundEcChecked`, `BoundUnsupported` (error node), …
- **`BoundPerformControl`** (loop descriptors: once/TIMES/UNTIL/VARYING) and **`BoundSetTarget`** (SET receivers) are
  additional roots.

Error-node families (`BoundUnsupported`, `*Error`) keep the tree **total** — every input yields a node.

## The exhaustive visitor
`Cobol.Net.Compiler.SourceGen/BoundVisitorGenerator.cs` is a Roslyn incremental source generator. For every
`[BoundNode]` root it discovers the sealed leaves *through the semantic model* (`INamedTypeSymbol.BaseType`, never a
text scan) and emits `I{Root}Visitor<out T>` plus a `BoundVisitor.Accept<T>` dispatch. Adding a leaf regenerates the
interface and makes every consumer **fail to compile** until it handles the node — replacing the ~205 hand-maintained
`case Bound…` arms and killing the old duplicated-switch bug. It also emits `BoundStatementTree.StatementChildren` —
the one drift-proof enumeration of nested statements. See [[kb/Architecture/Module Overview]].

## Place lvalue
`Binding/Model/Place.cs` is the one typed-lvalue model — **structural addressing that replaces byte offsets**. A
`Place` is backend-neutral (carries an `AccessPath` + resolved `DataItem`s, never C# text). Kinds: leaf `MemberPlace`,
`DynTablePlace`, `RedefViewPlace`, `RenamesPlace`, `CapacityRegisterPlace`, `DebugRegisterPlace`; decorators
(`PlaceDecorator`): `RefModPlace`, `OdoGroupPlace`, `NumericImagePlace`. Every Place is built once by
`ReferenceResolver` and consumed identically by MOVE, arithmetic, INSPECT/STRING, file I/O, and CALL-by-reference;
`CodeGen.PlaceRenderer` renders read/write text. See [[kb/IR/Data Flow]].

## Key concepts
- Bound tree = the IR; no lowered branch IR (owner-locked §18.23).
- Semantic classification lives on the node (bind-once), not re-derived at emit.
- `[BoundNode]` roots → source-generated exhaustive `IVisitor<T>` + `Accept`; missing arm = compile error.
- Error-node families keep the tree total.
- `Place` = structural lvalue; `ReferenceResolver` is the single operand entry point.

## See also
- [[kb/IR/Control Flow]] · [[kb/IR/Data Flow]]
- [[kb/Compiler/Pipeline]] — where the bound tree sits.
- [[kb/Semantics/Passes]] — the passes that walk & gate it.
- [[kb/Diagrams/IR Graph Overview]]


- [[kb/Reference/Bound/_Index]] — the generated per-type reference (one note per `Bound*` type, from the source `///` comments).

## Flow Diagrams
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — traces each bound-node family from ISO construct → semantic rule → runtime behavior, with per-node ASCII flows.

## Backlinks
- [[kb/IR/MOC]] · [[kb/Index]] — link here.
- [[kb/Architecture/High-Level Design]] · [[kb/Compiler/Phases]] — reference it.
- Lookup: [[kb/Spec/Lookup/IR Mapping]] · [[kb/Diagrams/IR Node Hierarchy]] — map every `Bound*` node here.
