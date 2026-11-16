**2026-11-15, this note's trigger also fails at `--std 85`, and has a second symptom this note lacks (the Mythos lane's batch-6 code reads, mechanism 6; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-6.md`).**

**Ruling: UPHELD (b), harm crashes and rejects_legal_source.** The row `1985-GR-VII.4.4.4-6` is this note's mechanism at `--std 85` as at 2023. An FD's record descriptions implicitly redefine one record area (`§1985:VII.4.4.4 GR6`), and a REDEFINES of a sibling within the second record is ordinary. The trigger is the one this note records: a REDEFINES subordinate to the second or later record of a multi-record FD. It has two symptoms, decided by the redefined item (data-name-2). The order of the records matters, as in this note (the refuter's `r4min2`, with the redefining record first, runs). The checker's confidence: high.

1. **Data-name-2 alphanumeric** (`fdred2.cob`, `fdred3.cob`): the compiler overflows its stack in `StorageFormPass.Classify`, this note's own symptom (exit 3221225725 = 0xC00000FD, an unhandled .NET StackOverflow with the repeated frame; harm crashes).
2. **Data-name-2 numeric display** (`fdred.cob`, `RG-1 PIC 99`, and `fdred4.cob`, `A PIC 9(5)`): the binder passes and the generated C# fails, `error: backend compilation failed ... CS0103: The name 'RG' does not exist in the current context`, exit 70, with no COBOL diagnostic. The access is written `RG._redef_RG_1` where `RG` is folded into `RA`'s area. This second symptom is not in this note, and its fix must cover it. Harm: rejects_legal_source.

**Where:** this note's sites (`StorageFormPass.cs#Classify`, `ClassifyRedefinesClassesPass.cs#Run`, `DataBinder.cs#AssignClassBitOffsets`), plus the code generator's place renderer for the numeric arm: it emits `<record>._redef_<item>` for a record folded into the anchor.

**Row added.** `1985-GR-VII.4.4.4-6`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here.

**2023 rows whose stored verdict is wrong.** `GR-14.9.30.4-3` and `GR-13.18.33.4-3` (the cause is (b); a registrar re-records them non-conforming). They are not in this note's `inventory_rows` today.

**Rules (checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VII.4.4.4 GR6`: "these records automatically share the same record area in storage; this is equivalent to an implicit redefinition of the area"
  `OK  §1985:VII.4.4.4 GR6  (4.4.4 General Rules)  the text is in GR6 (checked on the fragment 'this is equivalent to an implicit redefinition of the area')`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b6/src/`). Each ran at `--std 85` and (`fdred.cob`, `fdred2.cob`) at `--std 2023`.

`fdred.cob` (data-name-2 numeric display, `RG-1 PIC 99`; the redefining record second):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDRED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "fdred.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  RA PIC X(4).
       01  RG.
           05 RG-1 PIC 99.
           05 RG-1X REDEFINES RG-1 PIC XX.
           05 RG-2 PIC XX.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT F. MOVE "12AB" TO RA. WRITE RA. CLOSE F.
           OPEN INPUT F. READ F AT END DISPLAY "EOF".
           DISPLAY "RG-1=" RG-1 " RG-1X=" RG-1X " RG-2=" RG-2.
           CLOSE F.
           STOP RUN.
```

`fdred2.cob` (data-name-2 alphanumeric, nothing after the REDEFINES; this note's `m3.cob` shape):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDRED2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "fdred2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R1 PIC X(10).
       01  R2.
           05 A PIC X(5).
           05 H REDEFINES A PIC X(5).
       PROCEDURE DIVISION.
       P0.
           MOVE "AB" TO A.
           DISPLAY "H=[" H "]".
           STOP RUN.
```

`fdred3.cob` (as `fdred2.cob` with another item, `B PIC XX`, after the REDEFINES):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDRED3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "fdred3.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R1 PIC X(10).
       01  R2.
           05 A PIC X(5).
           05 H REDEFINES A PIC X(5).
           05 B PIC XX.
       PROCEDURE DIVISION.
       P0.
           MOVE "AB" TO A.
           DISPLAY "H=[" H "]".
           STOP RUN.
```

`fdred4.cob` (this note's `m3.cob` with `A PIC 9(5)`: data-name-2 numeric display):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDRED4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "fdred4.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R1 PIC X(10).
       01  R2.
           05 A PIC 9(5).
           05 H REDEFINES A PIC X(5).
       PROCEDURE DIVISION.
       P0.
           MOVE "AB" TO A.
           DISPLAY "H=[" H "]".
           STOP RUN.
```

| Program | Shows | `--std 85` | `--std 2023` |
|---|---|---|---|
| `fdred.cob` | the numeric symptom | exit 70, `error: backend compilation failed (generated C# at fdred.g.cs): (99,129): error CS0103: The name 'RG' does not exist in the current context` | the same CS0103, exit 70 |
| `fdred2.cob` | the stack overflow, this note's own shape | exit 3221225725, `Stack overflow. Repeated 5323 times: at WiseOwl.Cobol.Binding.Passes.StorageFormPass.Classify(DataItem, HashSet`1)` | exit 3221225725, `Stack overflow.` at `DataItem.get_IsDynamicTable()` and `StorageFormPass.Classify` |
| `fdred3.cob` | the stack overflow with an item after the REDEFINES | exit 3221225725, stack overflow | not run |
| `fdred4.cob` | the numeric symptom on this note's `m3.cob` shape | exit 70, `CS0103: The name 'R2' does not exist` | not run |

**Expected:** each compiles; `fdred.cob` prints `RG-1=12 RG-1X=12 RG-2=AB` (`§1985:VII.4.4.4 GR6`). **Observed:** exit 70 CS0103 (`fdred.cob`, `fdred4.cob`) or exit 0xC00000FD stack overflow (`fdred2.cob`, `fdred3.cob`).
