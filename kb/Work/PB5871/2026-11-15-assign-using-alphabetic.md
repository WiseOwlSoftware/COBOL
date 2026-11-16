**2026-11-15, the golden-2002-7 integrator (gl2002 wave u, slug g2002u-12-3-4-1-file-control): ASSIGN ... USING is a sibling of this note's mechanism; row added to `inventory_rows`.**

Rows added: `2002-SR-12.3.4.2-7` (the writer's row was `defect-suspected`; its four drafts g2asg02 to g2asg05 stay
out of the tree under `E:/COBOL-coord/scratch/gl2002/lane1/out/g2002u-12-3-4-1-file-control/` until this lands).

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.Resolve.cs#ResolveAssignUsing` (COBOL1810: line 420
asks `ItemCategory.IsAlphanumeric(item)` at every edition).

**Rules (each checked with `python scripts/spec/cite.py --check`, verdict OK).**

- `§2002:12.3.4.2 SR7`: "Data-name-1 shall reference an alphanumeric data item"
- `§2002:8.5.2.1`: "An alphabetic data item may be specified wherever an elementary alphanumeric data item is permitted as an operand"

**Repro, re-run on the golden-2002-7 build (main 5e49d46678).** `E:/COBOL-coord/scratch/gl2002/integrate-7/repro/asg/g2asg06.cob`
(from `lane1/run/g2002u-12-3-4-1-file-control/asg/`), command `cobol.exe g2asg06.cob --std 2002 --run`:

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ASG06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F4 ASSIGN USING WS-ALPHA.
       DATA DIVISION.
       FILE SECTION.
       FD  F4.
       01  R4          PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-ALPHA    PIC A(8) VALUE "GTWOASGA".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F4.
           MOVE "FOUR" TO R4. WRITE R4.
           CLOSE F4.
           MOVE SPACES TO R4.
           OPEN INPUT F4.
           READ F4 AT END DISPLAY "F4 EMPTY".
           DISPLAY "F4 ALPHABETIC " R4.
           CLOSE F4.
           STOP RUN.
```

**Observed.** `g2asg06.cob(6,35): error COBOL1810: file 'F4': ASSIGN … USING 'WS-ALPHA' shall reference an alphanumeric
data item — 'WS-ALPHA' is a category-alphabetic item (ISO §2023:12.4.5.2 SR7)`, rc 65.

**Expected (from the 2002 text).** Accepted; data-name-1 is an operand, so the alphabetic item stands for an
alphanumeric one; the program prints `F4 ALPHABETIC FOUR    ` and exits 0.

**Sweep.** The helper this note's sweep asks for ("alphanumeric operand at this edition") is the fix here too; the
message also cites only the 2023 clause at `--std 2002` (kb/Work PB4950).
