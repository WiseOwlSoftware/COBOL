---
title: Lookup — Runtime Behavior Mapping
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Runtime/Values/Numeric/CobolNum.cs
  - src/Cobol.Net.Runtime/IO/CobolFile.cs
  - src/Cobol.Net.Runtime/Control/ManagedPointer.cs
  - src/Cobol.Net.Runtime/Exceptions/ExceptionCatalog.cs
tags:
  - cobolsharp
  - spec
  - lookup
  - runtime
---

# Lookup — Runtime Behavior Mapping

Maps observable **runtime behaviors** to their ISO rule, the bound-tree node that produces them, the governing
semantic rule, and the [[kb/Runtime/Execution Model]] section. Runtime types live in `Cobol.Net.Runtime`;
generated C# calls them (no byte `ProgramState`).

## Numerics
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| Unscaled fixed-point store | native `long`/`Int128` holds the unscaled value; scale is compile-time metadata | §8.8.1.2 | `BoundCompute`, `BoundNumRef` | numeric category | [[kb/Runtime/Execution Model]] |
| ROUNDED (8 modes) | `CobolRounding` applies the selected mode in `TryStore` | §14.7.4 rounding | arithmetic nodes | ROUNDED modes | [[kb/Runtime/Execution Model]] |
| ON SIZE ERROR | `TryStore` returns false on overflow; receiver unchanged | §14.6.6 | `SizeErrorPhrase` | conditional store | [[kb/Runtime/Execution Model]] |
| Signed-DISPLAY overpunch | `NumericSign` produces the zoned sign image | §13.16 sign | `BoundMove`/`BoundDisplay` | SIGN clause | [[kb/Runtime/Execution Model]] |
| STANDARD-DECIMAL arithmetic | `CobolDec` (Int128 significand) exact mode | §8.8.1.3 | `BoundCompute` | ARITHMETIC IS | [[kb/Runtime/Execution Model]] |
| COMP-1/2 float bypass | `CobolFloat` uses IEEE `float`/`double` | §13.16 usage | numeric nodes | USAGE float | [[kb/Runtime/Execution Model]] |

## Strings
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| INSPECT cycle | one comparison pass for TALLYING/REPLACING/CONVERTING | §14.9.21 | `BoundInspect*` | ALL/LEADING/FIRST | [[kb/Runtime/Execution Model]] |
| STRING assembly | concatenation with POINTER/OVERFLOW | §14.9.38 | `BoundStringStmt` | overflow handling | [[kb/Runtime/Execution Model]] |
| UNSTRING split | delimiter split with TALLYING/COUNT | §14.9.42 | `BoundUnstringStmt` | delimiter rules | [[kb/Runtime/Execution Model]] |
| Reference modification | `RefMod` read / `SpliceInto` write; OOB → EC-BOUND-REF-MOD | §8.5.1.2 ref-mod | `Place` (RefModPlace) | bounds check | [[kb/IR/Data Flow]] |
| UTF-16 = 1 char | one COBOL char = one UTF-16 code unit | §8.5.1.4 | string operands | national/alnum | [[kb/Runtime/Execution Model]] |

## Files & I/O
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| FILE STATUS codes | two-char `FileStatusCode` set after each I/O verb | §9.1.13 I-O status | file verb nodes | status semantics | [[kb/Semantics/Validation Rules]] |
| OPEN/READ/WRITE state machine | connector enforces mode/position (43/46, 21, 02/26) | §14.9 I-O verbs | `BoundOpen`/`Read`/`Write`/… | open-mode legality | [[kb/Runtime/Execution Model]] |
| Organizations | sequential / line-seq / indexed(+alt keys) / relative | §12.4 ORGANIZATION | keyed I/O nodes | key rules | [[kb/Runtime/Execution Model]] |
| Record codec at edge | generated `IRecordCodec` serializes at the disk boundary | §13.4 records | file verb nodes | layout codec | [[kb/IR/Data Flow]] |
| SORT/MERGE ordering | typed `CobolSort.Key`; numeric-by-value / alnum-by-image | §14.9 SORT | `BoundSort`/`BoundMerge` | key collation | [[kb/Runtime/Execution Model]] |
| LINAGE / print stream | page geometry on the sequential print stream | §13.16 LINAGE | `BoundWrite`/`BoundLinageCounterRef` | LINAGE-COUNTER | [[kb/Runtime/Execution Model]] |

## Control flow & termination
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| PC dispatch | paragraphs run as PC cases; GO TO sets pc | §14.4 flow | `BoundGoTo`/`BoundParagraph` | fall-through | [[kb/IR/Control Flow]] |
| PERFORM return-address | recursive `Dispatch(entry,exit)` = return stack | §14.9.28 | `BoundOutOfLinePerform` | bounded range | [[kb/IR/Control Flow]] |
| STOP RUN unwind | `StopRun` exception caught at run-unit Main | §14.9.39 | `BoundStop` | run-unit end | [[kb/Runtime/Execution Model]] |
| GOBACK / EXIT PROGRAM | `ProgramReturn` caught at program Entry | §14.9.16 | `BoundGoback`/`BoundExitProgram` | program end | [[kb/Runtime/Execution Model]] |
| Method return | `MethodReturn` signal for method-only GOBACK | §11 methods | `BoundMethodReturn` | method scope | [[kb/Runtime/Execution Model]] |

## Interprogram
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| CALL pass modes | BY REFERENCE/CONTENT/VALUE threaded as pass modes | §14.9.7 CALL | `BoundCallProgram` | linkage rules | [[kb/Runtime/Execution Model]] |
| Managed reference | one `ManagedPointer<T>` carrier for ref/pointer/based | §8.5.1.7 pointer | `BoundAddressOf`/`BoundSetPointer` | pointer typing | [[kb/Runtime/Execution Model]] |
| Dynamic/cross-assembly CALL | opaque ABI `ICobolProgram.Call(CobolArgs)` via registry | §14.9.7 fmt | `BoundCallProgram` | dynamic resolution | [[kb/Runtime/Execution Model]] |
| EXTERNAL persistence | `ExternalStore`/`ExternalTable` survive CANCEL | §13.16 EXTERNAL | data model | EXTERNAL clause | [[kb/Runtime/Execution Model]] |

## OO
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| INVOKE dispatch | new / virtual / static / `__CobolInvoke` (AOT-safe) | §14.9.22 | `BoundInvoke` | method resolution | [[kb/Runtime/Execution Model]] |
| Class → `CobolObject` | every CLASS-ID is a real C# class | §11 classes | `BoundMethod` | inheritance | [[kb/Runtime/Execution Model]] |
| FACTORY singleton | per-class factory object | §11 factory | OO nodes | factory scope | [[kb/Runtime/Execution Model]] |

## Conditions & Exceptions
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| EC engine | `ExceptionCatalog` (Table 13) + `ExceptionState` registers | §14.6.12 | `BoundEcChecked` | EC hierarchy/fatality | [[kb/Runtime/Execution Model]] |
| >>TURN checking | EC checking compiled in/out at compile time | §7.3.13 | `EcFeatures` | directive fold | [[kb/Semantics/Validation Rules]] |
| USE declaratives | run as bounded pc-ranges returning a resume action | §14.5 USE | `BoundDeclarative` | resume action | [[kb/IR/Control Flow]] |
| Classic phrases | ON SIZE ERROR / AT END / INVALID KEY always active | §14.6 | phrase wrappers | always-on | [[kb/Runtime/Execution Model]] |

## Report Writer
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| Compose-at-presentation | each line a `Func<string>` invoked after LINE-COUNTER | §13.18 RW | `BoundGenerate` | line composition | [[kb/Runtime/Execution Model]] |
| CONTROL breaks / SUM | prior-value CF composition, accumulate/reset/UPON | §13.18 CONTROL | `BoundReportSumRef` | control hierarchy | [[kb/Runtime/Execution Model]] |

## Determinism
| Runtime Behavior | Description | Spec Rule | IR Node | Semantic Rule | Execution Model |
|---|---|---|---|---|---|
| Injectable clock | `IClock`/`SystemClock` for CURRENT-DATE/WHEN-COMPILED | §15 date fns | `BoundIntrinsicCall` | function window | [[kb/Runtime/Execution Model]] |

## See also
- [[kb/Runtime/Execution Model]] — the full runtime model.
- [[kb/Spec/Lookup/IR Mapping]] — the nodes that drive these behaviors.
- [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Diagrams/Runtime Behavior Flow]]


## Flow Diagrams
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — each runtime behavior traced back to its IR node and semantic rule.

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/Runtime/Execution Model]] — link here.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Runtime integration phase.
