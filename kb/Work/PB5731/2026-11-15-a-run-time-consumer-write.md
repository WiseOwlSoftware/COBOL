**2026-11-15, a run-time consumer: the WRITE record length (the Mythos lane's batch-2 code reads, mechanism 2; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-2.md`).**

**Ruling: UPHELD (a), harm wrong_answer.** The row `1985-GR-VII.3.8.4-14` is the mechanism of this note (a format 3 record with an OCCURS DEPENDING ON table is sized at the table's minimum at 85); the row adds a run-time consumer: WRITE puts out a 4-character record where `§1985:VII.3.8.4 GR15` makes it 10. **`wrong_answer` is to be added to this note's harm flags (its owner sets them; this file does not edit the note).** The checker's confidence: medium; its evidence was not rebuilt beyond one confirming run.

**Mechanism.** Below 2002 a format 3 clause is bound with BoundsEnforced false and the record's size follows its ODO occurrences, where `§1985:VII.3.8.4 GR15` sizes every format 3 record at the sum of its fixed items plus the MAXIMUM of each variable item, GR14 makes the record description define each record's size, and `§1985:VI.5.8.4 GR5` makes an ODO record variable only with a VARYING phrase. This note holds the compile-time consumers (VaryMin: SORT GIVING and SD checks); this row adds the WRITE length (the image at the current occurrences) to its sweep. At 2023 the current-occurrence size is that edition's rule, so the cause is (a).

**Sites** (the checker's reading): `src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.FileSection.cs#BindRecordClause` (BoundsEnforced false below 2002); `src/WiseOwl.Cobol.Compiler/Binding/Model/FileModel.cs#VaryMin`; the WRITE length follows `FileModel.RecordSizeVaries` (image at its current length, `SequentialIoEmitter.cs`).

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VII.3.8.4 GR15`: "the sum of the maximum number of characters in any variable length item subordinate to the record"
  `OK  §1985:VII.3.8.4 GR15  the text is in GR15`
- `§1985:VII.3.8.4 GR14`: "the size of each data record is completely defined in the record description entry"
  `OK  §1985:VII.3.8.4 GR14  the text is in GR14`
- `§1985:VII.3.8.4 GR13`: "It is implementor defined whether format 3 of the RECORD clause produces fixed length records or variable length records"
  `OK  §1985:VII.3.8.4 GR13  the text is in GR13`
- `§1985:VI.5.8.4 GR5`: "the associated file description or sort-merge description entry contains the VARYING phrase of the RECORD clause, the records are variable length"
  `OK  §1985:VI.5.8.4 GR5  the text is in GR5`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b2/src/f3w.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. F3W.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F3 ASSIGN TO "F3W.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F3 RECORD CONTAINS 4 TO 10 CHARACTERS.
       01 R3.
          05 N3 PIC 99.
          05 T3 PIC XX OCCURS 1 TO 4 TIMES DEPENDING ON N3.
       01 R3B PIC X(10).
       WORKING-STORAGE SECTION.
       01 W PIC X(10).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT F3.
           MOVE 1 TO N3. MOVE "AB" TO T3 (1).
           WRITE R3.
           CLOSE F3.
           OPEN INPUT F3.
           MOVE ALL "*" TO R3B.
           READ F3 AT END DISPLAY "EOF".
           MOVE R3B TO W.
           DISPLAY "R=[" W "]".
           CLOSE F3.
           STOP RUN.
```

**Observed at `--std 85`** (exit 0): the file bytes are `04 00 00 00 30 31 41 42`, a 4-byte record (`R=[01AB      ]` on re-read); `RECORD CONTAINS 4 TO 10`, record `05 N3 PIC 99` + `05 T3 PIC XX OCCURS 1 TO 4 DEPENDING ON N3` (maximum 10), N3=1. **Expected:** a 10-character record (`§1985:VII.3.8.4 GR15`).
