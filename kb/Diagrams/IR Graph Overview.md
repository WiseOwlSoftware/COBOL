---
title: Diagram — IR (Bound Tree) Graph
area: diagrams
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - diagram
---

# Diagram — IR (Bound Tree) Graph

Visualizes [[kb/IR/Node Types]]. The bound tree is the single semantic model — there is **no lowered IR**.

## Bound-node hierarchy (Mermaid)

```mermaid
flowchart TD
    ROOT["BoundProgram / BoundCompilation"]
    ROOT --> STMT["BoundStatement (~130 leaves)"]
    ROOT --> DATA["DataItem / RecordLayout"]
    STMT --> S1["BoundMove"]
    STMT --> S2["BoundIf / BoundEvaluate"]
    STMT --> S3["BoundInlinePerform / BoundOutOfLinePerform"]
    STMT --> S4["BoundGoTo / BoundGoToDepending"]
    STMT --> S5["BoundOpen/Read/Write/Rewrite"]
    STMT --> S6["BoundCallProgram · BoundStop · BoundGoback"]
    S1 --> EXPR
    S2 --> COND
    EXPR["BoundExpr\n(NumLiteral, NumRef, Binary, Power, IntrinsicCall)"]
    OPER["BoundOperand\n(StringLiteral, FieldOperand, Figurative)"]
    COND["BoundCondition\n(Relational, Logical, Condition88, Class, Sign)"]
    BOOL["BoundBoolExpr\n(BoolRef, BoolBinary, BoolShift)"]
    EXPR --> PLACE
    OPER --> PLACE
    COND --> PLACE
    PLACE[("Place — the one lvalue\nMemberPlace · RedefViewPlace · RenamesPlace\n+ RefModPlace · OdoGroupPlace decorators")]
    PLACE --> DI["DataItem (PicInfo, StorageForm)"]
```

## The `Place` lvalue (structural addressing, not byte offsets)

```
Place
 ├── leaf kinds:  MemberPlace · DynTablePlace · RedefViewPlace · RenamesPlace
 │                CapacityRegisterPlace · DebugRegisterPlace
 └── decorators:  RefModPlace (a:b) · OdoGroupPlace · NumericImagePlace
        built once by ReferenceResolver → consumed by MOVE / arithmetic /
        INSPECT / STRING / file I/O / CALL-by-reference → rendered by PlaceRenderer
```

## The exhaustive visitor (why no duplicated switches)

```
[BoundNode] roots  ──►  BoundVisitorGenerator (Roslyn source generator)
                        discovers leaves via the semantic model
                        emits  I{Root}Visitor<T>  +  Accept<T>  dispatch
                        ⇒ a NEW leaf makes every consumer fail to COMPILE
                          (replaces ~205 hand-written `case Bound…` arms)
```

## See also
- [[kb/IR/Node Types]] · [[kb/IR/Data Flow]] · [[kb/IR/Control Flow]]
- [[kb/Diagrams/Compiler Pipeline Diagram]]

## Backlinks
- [[kb/Diagrams/MOC]] · [[kb/Index]] — link here.
