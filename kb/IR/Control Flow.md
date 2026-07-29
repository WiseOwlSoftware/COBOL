---
title: IR — Control Flow (PC Dispatcher)
area: ir
status: draft
last_updated: 2026-07-23
related_files:
  - docs/COBOLNET_CONTROL_FLOW_DESIGN.md
  - src/Cobol.Net.Compiler/CodeGen/DispatchEmitter.cs
  - src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs
  - src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.cs
tags:
  - cobolsharp
  - ir
---

# IR — Control Flow (PC Dispatcher)

Control flow is a single **program-counter (PC) state machine**, implemented as `__Dispatch` in
`CodeGen/DispatchEmitter.cs` and designed in [[docs/COBOLNET_CONTROL_FLOW_DESIGN]]. Every paragraph — and, for
sectioning, every paragraph of every section, flattened into **one source-order PC space** — is a `case` in one
`switch (pc)`, **not** a separate method. This ports the legacy compiler's proven return-address / exit-bounded
dispatch (364 NIST tests) but realizes it in idiomatic C#. The stance: **"correctness over idiomatic for v1"** — the
dispatcher backbone is a deliberate state machine; "idiomatic" applies to case *contents* (IF→if/else,
EVALUATE→switch, inline PERFORM→real loops), not the backbone. See [[kb/Architecture/High-Level Design]].

## The exit-bounded core
```csharp
int Dispatch(int startPc, int exitPc) {
  int pc = startPc;
  while ((uint)pc < (uint)N) {
    bool atExit = pc == exitPc;          // captured BEFORE the body overwrites pc
    switch (pc) {
      case 0: /*MAIN*/  Dispatch(1,4); pc=2; break;   // PERFORM SUB THRU SUB-END
      case 1: /*SUB*/   if (X==1L){pc=3;break;} pc=2; break;  // IF..GO TO / fall-through
      // ...
      default: pc = N; break;
    }
    if (atExit && pc == exitPc + 1) return pc;   // named THRU exit fell through
  }
  return pc;
}
```

## Verbs mapped onto the PC
- **Fall-through** = `pc = i+1; break;` (paragraphs run into the next textually).
- **GO TO** = `pc = idxTarget; break;`. **GO TO … DEPENDING ON** = an inner `switch(selector)`; an out-of-range
  selector sets no pc, so the trailing fall-through runs (ISO §14.9.17 GR2).
- **Out-of-line PERFORM p THRU q** = recursive `Dispatch(idxP, idxQ)` — the C# call stack *is* the return stack, so
  overlapping/recursive ranges, inverted THRU (q physically before p), and GO-TO-out-and-back work **for free**.
- **PERFORM TIMES/UNTIL/VARYING** wrap that call in a real loop; **inline PERFORM** becomes a native `for`/`while`/
  `do-while` (EXIT PERFORM→`break`, EXIT PERFORM CYCLE→`continue`). VARYING…AFTER re-inits inner indices each outer
  increment (ISO §14.9.28).
- **ALTER** = a mutable field `_alter_<para>`; the alterable GO TO emits `pc = _alter_<para>`. ALTER is `--std 85`-only.
- **EXIT PARAGRAPH** = `pc = myIdx+1`. **EXIT SECTION** fires the section's return mechanism (GR7).
- **STOP RUN** throws `StopRun` (caught only at run-unit Main); **GOBACK / EXIT PROGRAM** throw `ProgramReturn` (caught
  at the program's Entry). Integer pc is used for in-program transfers; exceptions only for program/run-unit termination.
  See [[kb/Runtime/Execution Model]].
- **NEXT SENTENCE** = forward `goto __sent_<n>`.
- **DECLARATIVES** occupy the low PC indices `[0..declEnd)`; `EntryParagraphIndex` = first non-declarative; a USE
  procedure runs as its own `Dispatch(useStart,useEnd)` fired from the runtime I/O path.

A per-case `terminated` flag stops emitting after an unconditional transfer, so no unreachable C# is generated
(warnings-as-errors safe). Duplicate paragraph names across sections resolve by **symbol** (bound identity), not name.

## Key concepts
- One `switch(pc)` per unit; paragraphs = PC cases, not methods (owner-locked).
- Exit-bounded recursive `Dispatch` = return-address model → inverted/overlapping ranges free.
- Two termination exceptions (`StopRun` at Main, `ProgramReturn` at Entry) vs integer pc for transfers.
- Inline PERFORM = idiomatic loop; out-of-line = Dispatch call (two distinct mechanisms by design).
- Declaratives first in the pc space; USE fired by event, never fall-through.

## See also
- [[kb/IR/Node Types]] — the `BoundGoTo`/`BoundOutOfLinePerform`/… nodes.
- [[kb/Diagrams/Semantic Validation Flow]] · [[kb/Diagrams/IR Graph Overview]]
- [[kb/Runtime/Execution Model]] — the `StopRun`/`ProgramReturn` signals.
- [[kb/Spec/Language Features]] — the PERFORM/GO TO/ALTER surface.

## Backlinks
- [[kb/IR/MOC]] · [[kb/Index]] — link here.
- [[kb/Architecture/High-Level Design]] — locks this model (invariant 3).
- Lookup: [[kb/Spec/Lookup/IR Mapping]] (control-flow nodes) · [[kb/Spec/Lookup/Semantic Rules]] (flow rules).
