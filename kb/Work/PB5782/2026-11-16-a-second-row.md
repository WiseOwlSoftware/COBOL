**2026-11-16, a second row joins this note (the Mythos lane's batch-8 code reads, mechanism 4; source `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-8.md`).**

**Ruling: UPHELD (b), harm wrong_answer.** The row `1985-GR-XIII.3.7.4-2` is the mechanism of this note: moving negative zero into a numeric control item causes a control break though the item equals ZERO. This note claims the sibling row `1985-GR-XIII.3.7.4-3`; this row is named here and is not in its `inventory_rows` (`work.py check` refuses a 1985 rule id there, kb/Work PB5094). The checker's confidence: high. Same text and same output at 2023 (cause b).

**The checker's evidence.** `§1985:XIII.3.7.4 GR2` says that a change of value in any control data item causes a control break, and GR3 makes the test a numeric comparison for a numeric control item; zero is unique regardless of sign, so +0 to -0 is no change. The program moves `00-` into the control item and `IF CTL = ZERO` is true afterwards, yet a second control footing prints. Same at 2023.

**2023 row whose stored verdict is wrong.** `GR-13.18.16.4-3`; a registrar re-records it non-conforming.

**Where** (the checker's reading):
- `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ReportWriterEmitter.cs#EmitReportConstruction`
- `src/WiseOwl.Cobol.Runtime/IO/ReportWriter.cs#ControlEntry.ChangedFrom`

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:XIII.3.7.4 GR2`: "A change of value in any control data item causes a control break to occur"
  `OK  §1985:XIII.3.7.4 GR2  (3.7.4 General Rules)  the text is in GR2`
- `§1985:XIII.3.7.4 GR3`: "If the control data item is a numeric data item, the relation test is for the comparison of two numeric operands"
  `OK  §1985:XIII.3.7.4 GR3  (3.7.4 General Rules)  the text is in GR3`
- `§1985:VI.6.3.1.1.1`: "Zero is considered a unique value regardless of the sign"
  `OK  §1985:VI.6.3.1.1.1  (6.3.1.1.1 Comparison of Numeric Operands)  the text is in the clause`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b8/src/m4negz.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. M4NEGZ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "m4negz.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  CTL-G.
           05  CTL   PIC S99 SIGN TRAILING SEPARATE VALUE ZERO.
           05  CTL-X REDEFINES CTL PIC XXX.
       01  WS-N    PIC 9 VALUE 0.
       REPORT SECTION.
       RD  RP CONTROL IS CTL.
       01  DE-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DTL ".
           02  COLUMN 5 PIC 9 SOURCE WS-N.
       01  CF-1 TYPE CF CTL LINE PLUS 1.
           02  COLUMN 1 PIC X(5) VALUE "BREAK".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRT.
           INITIATE RP.
           MOVE 1 TO WS-N. GENERATE DE-1.
           MOVE "00-" TO CTL-X.
           IF CTL = ZERO DISPLAY "CTL IS ZERO".
           MOVE 2 TO WS-N. GENERATE DE-1.
           TERMINATE RP.
           CLOSE PRT.
           STOP RUN.
```

**Expected** (`m4negz.txt`): `DTL 1` / `DTL 2` / `BREAK`. **Observed** at `--std 85` (exit 0) and at `--std 2023` (exit 0, the same): standard output `CTL IS ZERO`, and `m4negz.txt` holds `DTL 1` / `BREAK` / `DTL 2` / `BREAK`.
