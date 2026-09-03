---
title: Reverse Index — Runtime Class → IR Nodes
area: runtime
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Runtime
  - src/Cobol.Net.Compiler/CodeGen/Roslyn/RuntimeApi.cs
tags:
  - cobolsharp
  - runtime
  - ir
---

# Reverse Index — Runtime Class → IR Nodes

Companion to [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] read in the other direction: for each
`Cobol.Net.Runtime` class, the bound-tree (`Bound*`) nodes whose emitted C# calls into it. Node names are real records
in `src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs`; call sites verified against the emitters in
`src/Cobol.Net.Compiler/CodeGen/` (the shared name table is `Roslyn/RuntimeApi.cs`). Where the emitter reaches a class
only through a facade or supertype, that path is marked *(transitive)*. See [[kb/Runtime/Execution Model]] and
[[kb/Spec/Lookup/Runtime Mapping]].

## Numerics
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `CobolNum` | scaled-integer store / rescale / convert | all arithmetic `Bound*` (`BoundCompute`, `BoundAddTo`/`AddGiving`, `BoundSubtractFrom`/`Giving`, `BoundMultiplyBy`/`Giving`, `BoundDivideInto`/`Giving`/`BoundDivideRemainder`), `BoundMove`, `BoundCorresponding`, `BoundInitialize`, `BoundComputedOperand`, `BoundFieldOperand` (numeric) | §14.7, §14.9.2/.8/.12/.25/.26/.44 |
| `CobolRounding` | 8 ROUNDED modes passed to stores | all arithmetic `Bound*` via `Receiver.Rounding`; any rounded `BoundMove`/`BoundSetTo`/INSPECT-to-number store | §14.7.4 |
| `CobolSizeError` | SIZE ERROR / divide-zero / overflow raise | `SizeErrorPhrase`, `BoundDivideInto`/`Giving`, `BoundCompute`, `BoundPower` | §14.7.5 |
| `CobolDec` | 128-bit decimal intermediate + compare | `BoundBinary`, `BoundNegate`, `BoundPower`, `BoundCompute`, `BoundRelational` (decimal compare) | §8.8.1, §8.8.4.2 |
| `CobolFloat` | IEEE float intermediate / USAGE store | `BoundBinary`/`Negate`/`Power` (float), `BoundMove` (float receiver), `BoundComputedOperand` | §8.8.1, §13.18 |
| `CobolEdit` | numeric-edited PIC formatting | `BoundMove` (edited receiver), edited render in DISPLAY | §13.18.40, §14.9.25 |
| `CobolString` | alphanumeric store / justify / compare | `BoundMove`, `BoundCorresponding`, `BoundInitialize`, `BoundRelational`, `BoundCondition88`, `BoundRangeMembership` | §14.9.25, §8.8.4.2/.5 |
| `CobolDynString` | dynamic-length item resize in place | dynamic-length receiver stores; `BoundSetUpDown`/SET-size | §13.18.20 |
| `CobolBool` | boolean operators + simple boolean test | `BoundBoolBinary`, `BoundBoolNot`, `BoundBoolShift`, `BoundBoolRef`, `BoundBooleanCondition` | §8.7.2, §8.8.2, §8.8.4.3 |
| `CobolClass` | class-condition membership tests | `BoundClassCondition`, `BoundUserClassCondition` | §8.8.4.4, §12.3.7 |
| `NationalCollation` | national repertoire collation weights | `BoundIntrinsicCall` (national CHAR/ORD, collated MAX/MIN), ALPHABET setup, national `BoundRelational` | §8.3.3.5, §15.15/.17 |

## Tables
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `CobolTable` | fixed OCCURS element access + ODO count | `BoundFieldOperand` (subscripted, via `PlaceRenderer`), ODO extent reads | §13.18.38, §8.4.2.3 |
| `CobolDynTable` | DYNAMIC-capacity OCCURS backing + register ops | dynamic-table value-init (`BoundProgram` data), dynamic-table register operations | §13.18.38 Fmt 4 (D9) |

## Files & Sort
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `CobolFile` | static facade for every I-O verb | `BoundOpen`, `BoundClose`, `BoundRead`, `BoundKeyedRead`, `BoundWrite`+`BoundAdvancing`, `BoundKeyedWrite`, `BoundRewrite`, `BoundKeyedRewrite`, `BoundKeyedStart`, `BoundKeyedDelete`, `BoundKeyedDeleteFile`, `BoundUnlock`, `BoundLinageCounterRef` | §14.9.6/.10/.27/.30/.35/.41/.47/.51 |
| `FileRegistry` | run-unit file table behind the facade *(transitive)* | same I-O nodes as `CobolFile` | §9.1 |
| `SequentialConnector` | sequential/line-seq per-file engine *(transitive)* | `BoundRead`, `BoundWrite`+`BoundAdvancing`, `BoundRewrite` (sequential) | §14.9.30/.35/.51 |
| `RelativeConnector` | relative-organization engine *(transitive)* | `BoundKeyedRead`, `BoundKeyedWrite`, `BoundKeyedRewrite`, `BoundKeyedStart`, `BoundKeyedDelete` (relative) | §14.9.41 |
| `IndexedConnector` | indexed-organization engine *(transitive)* | same keyed nodes (indexed, alt-key windows) | §12.4.5, §14.9.41 |
| `RecordFraming` | variable-length record framing on disk *(transitive)* | `BoundWrite`/`BoundKeyedWrite`/`BoundRead` (RECORD VARYING) | §13.18.43 |
| `CobolSort` | SORT/MERGE image store + key compare | `BoundSort`, `BoundMerge`, `BoundRelease`, `BoundReturn`, `BoundSortMergeKey` (`BoundTableSort`/`BoundTableSortKey` use a typed comparer, bypassing the image store) | §14.9.24/.32/.34/.40 |

## Intrinsics
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `CobolIntrinsics` | FUNCTION library (exact/float/text) | `BoundIntrinsicCall` | §15 |
| `CobolDate` | date/time intrinsic family | `BoundIntrinsicCall` (date fns), `BoundAccept` (temporal kinds) | §15 date/time, §14.9.1 |
| `Clock` / `IClock` | injectable now-source | `BoundIntrinsicCall` (CURRENT-DATE etc.), `BoundAccept` (temporal) — via `RunUnit.Current.Clock` | §14.9.1, §15 |
| `AcceptSource` | ACCEPT device + temporal readings | `BoundAccept` | §14.9.1 |

## Interprogram & pointers
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `ProgramRegistry` | CALL/CANCEL activation + pointer call | `BoundCallProgram`, `BoundCallArg`, `BoundCancel`, `BoundSetProgramPointer` | §14.9.4/.5 |
| `CobolModule` | FUNCTION MODULE-NAME / module identity | `BoundIntrinsicCall` (MODULE-NAME) | §15.65 |
| `RunUnit` | run-unit lifetime, exit status, files, clock | `BoundProgram`, `BoundStop`, `BoundGoback` (exit status), `BoundAccept` (clock) | §14.4, §14.9.18/.42 |
| `ManagedPointer` | single managed-ref pointer carrier | `BoundAddressOf`, `BoundSetPointer`, `BoundSetAddressOfBased`, `BoundAllocate`/`BoundFree` (result) | §8.4.3.11, §14.9.39 |
| `CellPointer` | ManagedPointer subclass (cell+offset) *(transitive)* | `BoundAddressOf` (via `ManagedPointer.At`) | §8.4.3.11 |
| `StorageCell` | backing cell for ADDRESS-OF / ALLOCATE | `BoundAllocate`, `BoundFree`, `BoundAddressOf` (emitted cell field) | §14.9.3/.15, §8.4.3.11 |
| `CobolPtr` | ALLOCATE/FREE heap operations | `BoundAllocate`, `BoundFree` | §14.9.3/.15 |
| `ExternalStore` | EXTERNAL data/file cell storage | EXTERNAL items (`BoundProgram` data), EXTERNAL `BoundFieldOperand` address | §13.18.24 |
| `ExternalSwitches` / `SwitchStore` | SPECIAL-NAMES switch get/set | `BoundSwitchCondition` (read), SET-switch statement | §8.8.4.6, §12.3.7 |

## OO
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `CobolObject` | base of every CLASS-ID; INVOKE dispatch | `BoundInvoke`, `BoundInvokeUniversal`, `BoundMethod`, `BoundSetObjectRef`, `BoundRelational` (object-ref compare) | §11, §14.9.23, §8.4.3.8 |
| `CobolInvokeArg` | descriptor-typed universal-invoke args | `BoundInvokeArg`, `BoundInvokeUniversal` | §14.9.23.3 |

## Exceptions & EC engine
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `ExceptionState` | last-exception register set/clear | `BoundRaise`, `BoundRaiseObject`, `BoundSetLastException`, `BoundSearch` (range EC), file-I/O EC, arithmetic EC | §14.6.13, §14.9.29/.33/.39 |
| `ExceptionCatalog` | EC-name catalog + Table-13 hierarchy | `BoundRaise`, `BoundExceptionMatch`, `__EcDispatch` | §14.6.13.1 |
| `EcFunctions` | EXCEPTION-* interrogation intrinsics | `BoundIntrinsicCall` | §15.28–15.33 |
| `CobolFatalException` | throw for fatal EC / statement guard | `BoundEcChecked`, `BoundRaise` (fatal), `BoundCallProgram` (fatal), `BoundInvoke` | §14.6.13.1.6 |
| `PerformFrame` | Format-3 checking-PERFORM frame stack | `BoundExceptionPerform`, `BoundExceptionMatch` | §14.9.28.4 |

## Report Writer
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `CobolReport` | RWCS engine (`IO/ReportWriter.cs`) | `BoundInitiate`, `BoundGenerate`, `BoundTerminate`, `BoundSuppress`, `BoundReportCounterRef`, `BoundReportSumRef`, `BoundReportVaryingRef` | §14.9.16/.21/.45/.46, §13.18 |
| `ReportGroup` / `ReportGroupLine` | report-description scaffolding | report-description data feeding the RWCS nodes | §13.18.54/.64 |

## Control signals
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `StopRun` | STOP RUN unwind to run-unit wrapper | `BoundStop` | §14.9.42 |
| `ProgramReturn` | GOBACK / EXIT PROGRAM unwind | `BoundGoback`, `BoundExitProgram` | §14.9.18, §14.9.14 |
| `MethodReturn` | method-context GOBACK / EXIT METHOD | `BoundMethodReturn` | §14.9.18.4 |
| `ResumeSignal` | RESUME → TargetPc unwind | `BoundResume` | §14.9.33 |
| `ExitPerformSignal` | F3-handler EXIT PERFORM id-matched unwind | `BoundExitPerform` (F3 handler), `BoundExceptionPerform` | §14.9.14.4 |
| `CobolTiming` | CONTINUE AFTER n SECONDS timed pause | `BoundContinueAfter` | §14.9.9 |

## String verbs
| Runtime class | Purpose | IR nodes that call it | ISO area |
|---|---|---|---|
| `CobolStringOps` | STRING transfer / UNSTRING extract | `BoundStringStmt`, `BoundStringSending`, `BoundUnstringStmt`, `BoundUnstringReceiver`, `BoundUnstringDelimiter` | §14.9.43/.48 |
| `CobolInspect` | INSPECT tally / replace / convert | `BoundInspect`, `BoundInspectTally`, `BoundInspectReplace`, `BoundInspectConvert` | §14.9.22 |

## Reconciliation notes (source wins over the flow note)
- `FileRegistry` and the three connectors + `RecordFraming` have **no direct emitter reference** — every I-O node emits a call on the static `CobolFile` facade (per `RuntimeApi.cs`), which delegates to `RunUnit.Current.Files` and dispatches polymorphically to the connectors. Reached transitively by the same node set as `CobolFile`.
- `CellPointer` has zero emitter references; it is a `ManagedPointer` subclass produced inside the runtime, reached transitively from `BoundAddressOf`.
- The runtime file is `IO/ReportWriter.cs` but the callable engine class is `CobolReport`.
- All FUNCTION-library classes (`CobolIntrinsics`, `CobolDate`, `EcFunctions`, `CobolModule`, `NationalCollation`) share the single `BoundIntrinsicCall` node, dispatched by catalog name.
- `CobolRounding` has the widest fan-out (~44 emitter refs) — it is the rounding-mode argument threaded into `CobolNum` stores across MOVE/arithmetic/INSPECT/SET/I-O/sort, not a standalone verb.

## See also
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — the forward per-node flows.
- [[kb/Spec/Lookup/Runtime Mapping]] · [[kb/Spec/Lookup/IR Mapping]]
- [[kb/Runtime/Execution Model]]

## Backlinks
- [[kb/Runtime/MOC]] · [[kb/Index]] — link here.
