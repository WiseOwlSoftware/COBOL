---
title: Data-Flow Traces
area: ir
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Compiler/Binding/ReferenceResolver.cs
  - src/Cobol.Net.Compiler/Binding/Model/Place.cs
  - src/Cobol.Net.Runtime/Values/Numeric/CobolNum.cs
  - src/Cobol.Net.Runtime/IO/CobolFile.cs
tags:
  - cobolsharp
  - ir
---

# Data-Flow Traces

End-to-end traces following **one data item's `Place`** as it threads through a sequence of verbs — the concrete,
node-by-node view of [[kb/IR/Data Flow]]. Each trace shows the `Place` (the single lvalue, built once by
`ReferenceResolver`) and the type transitions between native `long`/`Int128`, `string`, and the transient byte/char
image at the disk edge. Runtime classes per node: [[kb/Runtime/Runtime-Class-to-IR]].

## Trace A — a numeric field: MOVE → arithmetic → edited MOVE → file WRITE
The flagship: `01 WS-TOTAL PIC S9(5)V99.` (native `long`, unscaled, scale = 2) accumulates, then prints.

```cobol
01 WS-TOTAL   PIC S9(5)V99.        *> long, unscaled, scale 2
01 WS-ITEM    PIC 9(4)V99.
01 PRINT-AMT  PIC $$$,$$9.99.       *> edited -> string
FD REPORT-FILE. 01 REPORT-REC PIC X(80).
...
MOVE 0 TO WS-TOTAL.                 *> BoundMove
ADD WS-ITEM TO WS-TOTAL.           *> BoundAddTo
MOVE WS-TOTAL TO PRINT-AMT.        *> BoundMove (numeric -> edited)
WRITE REPORT-REC FROM PRINT-AMT.   *> BoundWrite
```

```text
 declaration        Place                     verb / IR node            runtime            representation
 ───────────        ─────                     ──────────────            ───────            ──────────────
 WS-TOTAL PIC S9(5)V99
   └─ ReferenceResolver ─► MemberPlace(WsTotal, NumProfile{digits5,scale2,signed})
                                              BoundMove 0 ──────────► CobolNum.Store     long = 0        (unscaled)
       WS-ITEM MemberPlace ──operand──►       BoundAddTo ───────────► CobolNum.TryStore  long += item·10^(2-2)
                                                (ROUNDED/SIZE via Receiver)  CobolRounding  → long (unscaled, scale 2)
   WS-TOTAL Place ──sender──►                  BoundMove ────────────► CobolNum→CobolEdit  long → "$ 12,345.67" (string)
       PRINT-AMT MemberPlace(string) ◄─receiver┘                                            edited string, width 12
   PRINT-AMT Place ──FROM sender──►            BoundWrite ───────────► CobolFile.Write     string image → REPORT-REC
                                                                        (SequentialConnector) → 80-char record on disk
```

**What threads:** the *same* `MemberPlace` for `WS-TOTAL` is the receiver of the MOVE, the receiver of the ADD, and
the sender of the edited MOVE — built once, consumed three times. The value stays a native `long` (unscaled, scale 2)
through all arithmetic; the decimal point is compile-time `NumProfile` metadata, never stored. Only at the
numeric→edited MOVE does `CobolEdit` render a `string`; only at `WRITE` does the char image reach the disk. No byte
`ProgramState` anywhere. Nodes: [[kb/Spec/Lookup/IR Mapping]]; flows: [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]].

## Trace B — a whole group referenced as alphanumeric
`01 WS-REC` with numeric leaves, MOVEd as a whole → the leaves must materialize as their zoned char image (MOVE GR4).

```text
 WS-REC (group)                     StorageForm decision           materialization
 ─────────────                      ────────────────────           ───────────────
 01 WS-REC.  05 A PIC 9(3).  05 B PIC X(5).
   StorageFormPass ─► A gets StorageForm.CharImage(Numeric)   (because WS-REC is used whole)
 MOVE WS-REC TO OUT-REC ─► BoundMove(group) ─► WsRec.AsImage()  → string(3+5)   (A zoned + B chars)
                                              ─► OutRec = that string           (Encoding.Latin1 at the byte edge only)
 MOVE 42 TO A          ─► BoundMove(elem)   ─► CobolNum.Store → A.long = 42; A.AsImage() rebuilt on demand
```
**Key:** a numeric leaf is a native `long` **until** its group is referenced whole; then `StorageFormPass` marks it
`CharImage` and `AsImage()`/`FromImage()` build the zoned image on demand — never persisted. See
[[kb/IR/Data Flow]] (whole-group-as-alphanumeric).

## Trace C — a table element through SEARCH then MOVE
`05 ENTRY OCCURS 100 INDEXED BY IDX.` — subscript resolution, binary SEARCH, element store.

```text
 ReferenceResolver: ENTRY(IDX) ─► MemberPlace(Table[IDX-1])   (1-based COBOL → [expr-1])
 SEARCH ALL TAB ─► BoundSearch(FromStart) ─► EmitSearchScan: IDX walks the CobolTable; WHEN key test
   WHEN ENTRY-KEY(IDX)=X ─► BoundSearchWhen ─► CobolString.Compare / scaled-int compare
 MOVE ENTRY-VAL(IDX) TO RESULT ─► BoundMove ─► CobolNum/CobolString via Table[IDX-1] Place
```
The subscript `IDX` is a native `long` (1-based occurrence number, layout-free); the `Place` carries the subscript
expression and is re-evaluated on each access. See [[kb/IR/Control Flow]] (SEARCH).

## Trace D — a REDEFINES view (one canonical backing)
`05 AMT-N PIC 9(6). 05 AMT-X REDEFINES AMT-N PIC X(6).` — Tier B (all-DISPLAY): one `string` backing, two views.

```text
 redefines class {AMT-N, AMT-X}  ─►  canonical = string(6)   (Tier B StringCanonical)
 MOVE 123456 TO AMT-N ─► BoundMove ─► RedefViewPlace(numeric): FormatDisplay(long) → substring(0,6) of the string
 MOVE AMT-X TO OUT    ─► BoundMove ─► RedefViewPlace(alnum): substring(0,6) read directly
```
Both views are computed accessors over the **one** stored `string`; no two typed reps are simultaneously live, and no
shared `byte[]` (that is only Tier C, for genuine mixed-USAGE puns). See [[kb/IR/Data Flow]] (4-tier model).

## Reading a trace
- **Place** = the lvalue built once by `ReferenceResolver`; every verb consumes the same Place.
- **long/Int128** = the unscaled fixed-point value; scale is `NumProfile` metadata.
- **string** appears only when a value is imaged (edited MOVE, DISPLAY, group-as-alphanumeric, file record).
- **byte/char image** exists only transiently at the file/CODE-SET boundary (`Encoding.Latin1`), never persisted.

## See also
- [[kb/IR/Data Flow]] — the model these traces instantiate.
- [[kb/IR/Node Types]] · [[kb/Spec/Lookup/IR Mapping]]
- [[kb/Runtime/Runtime-Class-to-IR]] · [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]]

## Backlinks
- [[kb/IR/MOC]] · [[kb/Index]] — link here.
