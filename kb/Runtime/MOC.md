---
title: Runtime — Map of Content
area: runtime
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - runtime
  - moc
---

# 🗺 Runtime — Map of Content

The typed-native runtime kernel (`Cobol.Net.Runtime`) that generated COBOL programs call into. No byte-array State.

## Notes in this domain

- [[kb/Runtime/Execution Model]] — the full runtime: `CobolNum` numerics, string/INSPECT verbs, file I/O state machines, ~70 intrinsics, CALL/interprogram linkage, OO, the EC exception model, and the Report Writer engine.

- [[kb/Runtime/Runtime-Class-to-IR]] — reverse index: each runtime class → the IR nodes that call it.

## Subsystems (sections within Execution Model)

| Subsystem | Key types |
|---|---|
| Numerics | `CobolNum`, `CobolRounding`, `NumProfile`, `CobolEdit`, `CobolDec`, `CobolFloat` |
| Strings | `CobolInspect`, `CobolStringOps`, `CobolString` |
| Files & I/O | `CobolFile`, `SequentialConnector`, `RelativeConnector`, `IndexedConnector`, `CobolSort`, `FileStatus` |
| Intrinsics | `CobolIntrinsics` (.Text/.Float/.Exact), `CobolDate`, `IntrinsicCatalog` |
| Interprogram | `ManagedPointer<T>`, `ICobolProgram`, `ProgramRegistry`, `CobolModule`, `RunUnit` |
| OO | `CobolObject`, `__CobolInvoke`, `CobolInvokeArg` |
| Conditions & EC | `ExceptionCatalog`, `ExceptionState`, `EcFunctions`, `CobolFatalException`, `ResumeSignal` |
| Report Writer | `CobolReport` |

## See also

- [[kb/Architecture/MOC]] — the Runtime assembly & backend neutrality.
- [[kb/IR/MOC]] — the bound tree rendered into runtime calls.
- [[kb/Spec/MOC]] — the ISO behavior the runtime implements.
- [[kb/Index]] — knowledge-base home.
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — traces each runtime behavior back to the IR node and semantic rule that produce it.
