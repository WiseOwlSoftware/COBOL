---
title: Diagram — Runtime Behavior Flow
area: diagrams
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - diagram
---

# Diagram — Runtime Behavior Flow

How a bound statement becomes observable runtime behavior via the typed-native runtime. Feeds
[[kb/Spec/Lookup/Runtime Mapping]] and [[kb/Runtime/Execution Model]].

## Emit → runtime call

```mermaid
flowchart TD
    BT["bound node (e.g. BoundCompute)"] --> EMIT["CodeGen emitter renders a call"]
    EMIT --> CS["generated C# (.g.cs)"]
    CS --> RT["Cobol.Net.Runtime kernel"]
    RT --> NUM["Values: CobolNum · CobolString · CobolDynTable"]
    RT --> VERB["Verbs: CobolInspect · CobolStringOps"]
    RT --> IOK["IO: CobolFile · connectors · CobolSort"]
    RT --> CTL["Control: ManagedPointer · RunUnit · signals"]
    RT --> EXC["Exceptions: ExceptionCatalog · ExceptionState"]
    RT --> INT["Intrinsics: CobolIntrinsics · CobolDate"]
    NUM --> OUT["observable behavior\n(value / DISPLAY / file record)"]
    IOK --> OUT
    VERB --> OUT
```

## Signal model (control transfer as exceptions)

```mermaid
flowchart LR
    STOP["STOP RUN"] -->|StopRun| MAIN["caught at run-unit Main → unwind all"]
    GOBACK["GOBACK / EXIT PROGRAM"] -->|ProgramReturn| ENTRY["caught at program Entry"]
    METH["method GOBACK"] -->|MethodReturn| MCALL["caught at method call site"]
    RESUME["RESUME"] -->|ResumeSignal| DECL["USE declarative resume action"]
    EXITP["EXIT PERFORM"] -->|ExitPerformSignal / break| LOOP["inline PERFORM loop"]
```

## Numeric store funnel (ROUNDED + ON SIZE ERROR)

```
operands (long/Int128) ─▶ widen to Int128 ─▶ scale-align ─▶ compute
                                                   │
                                          TryStore(receiver profile)
                                          • CobolRounding (8 modes)
                                          • bounds check → false = ON SIZE ERROR (receiver unchanged)
                                          • MODE PROHIBITED + inexact → SIZE ERROR
```

## See also
- [[kb/Runtime/Execution Model]] · [[kb/Spec/Lookup/Runtime Mapping]]
- [[kb/IR/Control Flow]] · [[kb/Diagrams/Compiler Pipeline Diagram]]

## Backlinks
- [[kb/Diagrams/MOC]] · [[kb/Spec/Lookup/Runtime Mapping]] — link here.
