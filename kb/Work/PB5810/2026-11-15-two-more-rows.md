**2026-11-15, two more rows join this mechanism (the Mythos lane's batch-7 code reads, mechanism 4; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-7.md`).**

**Ruling: UPHELD (b), harm under_rejects and wrong_answer.** The mechanism of this note appears in two more files: a relative file's WRITE with RECORD VARYING DEPENDING ON a table element gives status 00 where the length 9 requires 44, and a sort file's RETURN delivers all 8 characters and leaves the length item `00`. The rows are `1985-GR-VIII.3.2.4-5` (relative) and `1985-GR-XI.3.2.4-2` (sort-merge). The checker's confidence: high.

**The checker's evidence.** The relative and sort-merge RECORD clauses are the sequential one by their own GR5 and GR2, and this note's reasoning (`§1985:IV.4.3.8.2.3 SR4`) applies unchanged. `p4rel`: a relative FD, VARYING FROM 2 TO 8 DEPENDING ON TL, TL PIC 99 OCCURS 2, TL(1) and TL(2) both 9: compiles, WRITE gives 00 (an elementary TL of 9 would give 44, this note's control) at `--std 85` and at `--std 2023`. `p5sd`: an SD of the same form, TL(1) = 3: compiles, RELEASE of 8 characters, RETURN yields all 8 and TL(1) stays 00 (the GR8 length store and the 3-character release are not done) at `--std 85` and at `--std 2023`. `1985-GR-XI.3.2.4-2` is not a new mechanism: the dispatch called it new, but its refuter's failure is this one; every other part of its stored claim was confirmed by the refuter's q1a to q1h probes, which the checker did not redo.

**Where** (the checker's reading, this note's sites): `src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.Resolve.cs#ResolveFiles`; `src/WiseOwl.Cobol.Compiler/Binding/RecordClauseRules.cs#Screen`.

**Rows added.** `1985-GR-VIII.3.2.4-5` and `1985-GR-XI.3.2.4-2`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the rows are named here.

**2023 rows whose stored verdict is wrong** (the checker's list): `FMT-13.18.43.2`, `SR-13.4.5.3-5`, `SR-13.4.5.3-7`, `FMT-13.4.6.2`; a registrar re-records them non-conforming.

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VIII.3.2.4 GR5`: "The RECORD clause for the Relative I-O module is the same as the RECORD clause for the Sequential I-O module"
  `OK  §1985:VIII.3.2.4 GR5  (3.2.4 General Rules)  the text is in GR5`
- `§1985:XI.3.2.4 GR2`: "The RECORD clause for the Sort-Merge module is the same as the RECORD clause for the Sequential I-O module"
  `OK  §1985:XI.3.2.4 GR2  (3.2.4 General Rules)  the text is in GR2`
- `§1985:IV.4.3.8.2.3 SR4`: "Each table element reference must be subscripted except when such reference appears"
  `OK  §1985:IV.4.3.8.2.3 SR4  (4.3.8.2.3 Syntax Rules)  the text is in SR4`
- `§1985:VII.3.8.3 SR4`: "Data-name-1 must describe an elementary unsigned integer in the Working-Storage or Linkage Section"
  `OK  §1985:VII.3.8.3 SR4  (3.8.3 Syntax Rules)  the text is in SR4`
- `§1985:VII.3.8.4 GR8`: "the number of character positions in the record must be placed into the data item referenced by data-name-1"
  `OK  §1985:VII.3.8.4 GR8  (3.8.4 General Rules)  the text is in GR8`

**Expected:** refused at compile time (`§1985:IV.4.3.8.2.3 SR4` with `§1985:VII.3.8.3 SR4`). **Observed** at `--std 85` and at `--std 2023`: compiles; the length register is ignored at run time.

**Repro 1** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b7/src/p4rel.cob`; `--std 85` and `--std 2023`: exit 0, `W9 00`; `p4rel23.cob` is the 2023 copy):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4REL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL ASSIGN TO "p4rel.rel"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RL LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS
               DEPENDING ON TL.
       01 RL-R PIC X(8).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 G.
           05 TL PIC 99 OCCURS 2.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT RL.
           MOVE "12345678" TO RL-R.
           MOVE 9 TO TL (1). MOVE 9 TO TL (2).
           WRITE RL-R INVALID KEY DISPLAY "IK".
           DISPLAY "W9 " FS.
           CLOSE RL.
           STOP RUN.
```

**Repro 2** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b7/src/p5sd.cob`; `--std 85` and `--std 2023`: exit 0, `R [ABCDEFGH] TL1 00`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5SD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5sd.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SF RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS
               DEPENDING ON TL.
       01 SF-R.
           05 SF-K PIC X(2).
           05 SF-D PIC X(6).
       WORKING-STORAGE SECTION.
       01 G.
           05 TL PIC 99 OCCURS 2.
       PROCEDURE DIVISION.
       M SECTION.
       P.
           SORT SF ON ASCENDING KEY SF-K
               INPUT PROCEDURE IS IP OUTPUT PROCEDURE IS OP.
           STOP RUN.
       IP SECTION.
       IP1.
           MOVE "ABCDEFGH" TO SF-R.
           MOVE 3 TO TL (1). MOVE 3 TO TL (2).
           RELEASE SF-R.
       OP SECTION.
       OP1.
           MOVE 0 TO TL (1). MOVE 0 TO TL (2).
           RETURN SF AT END DISPLAY "AE".
           DISPLAY "R [" SF-R "] TL1 " TL (1).
```
