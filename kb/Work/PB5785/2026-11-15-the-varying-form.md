**2026-11-15, the RECORD VARYING ... DEPENDING ON form joins this mechanism (the Mythos lane's batch-6 code reads, mechanism 7; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-6.md`).**

**Ruling: UPHELD (b), harm wrong_answer (silent).** The row `1985-GR-VII.4.4.4-13` is this note's mechanism in format 2 of the RECORD clause: does the RECORD clause or the record descriptions size the record? `FR` is `RECORD IS VARYING FROM 2 TO 20 DEPENDING ON DR` with record descriptions of 6 and 10 characters, which is legal (`§1985:VII.3.8.3 SR2`). The ruling beside this note (`2026-11-15-the-expected-result.md`) held that when a RECORD clause is present it gives the file's record sizes, and that the record sizes in GR13's text are those sizes. Under that ruling a 15-character record is within 2 to 20: status 00, `DR` = 15 (`§1985:VII.3.8.4 GR11`), and READ INTO sends the number of character positions in the current record, given by `DR` (`§1985:VII.3.8.4 GR12`), all 15. The compiler gives 00 and `DR` = 15, the clause's view (`RecordLengthConflicts` tests `VaryMin`/`VaryMax`), but it truncates the record to the largest description, so `W` receives 10 characters. The other reading, GR13 taken literally, would give 04 with a 10-character record. Either way the observed result is wrong: a status of 00 that says the record conforms, with 10 characters moved under a DEPENDING ON value of 15. Under the ruled reading the INTO length is what is wrong; here GR12 names the DEPENDING ON value as the INTO length. The checker's confidence: medium.

**Where:** the record area is capped at the largest record description (runtime `FileConnector.MaxRecordSize` / `RecordWidth`, the compiler's `RecordMax`) and the INTO sender reads that area (`src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/MoveBinder.cs#BindIntoPhrase`, this note's site); the conflict test `src/WiseOwl.Cobol.Runtime/IO/FileConnector.cs#RecordLengthConflicts` already takes the clause's bounds. `MoveBinder.cs` is among the pin's changed files; the refuter's output on 1df234fa72 (`R2 00 15 [ABCDEFGHIJ]`) matches the pin.

**Row added.** `1985-GR-VII.4.4.4-13`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here.

**2023 rows whose stored verdict is wrong.** `GR-14.9.30.4-14` and `GR-9.1.13.2-3` (the cause is (b): the output is identical at 2023; a registrar re-records them non-conforming). They are not in this note's `inventory_rows` today.

**Rules (checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VII.3.8.4 GR12`: "the number of character positions in the current record that participate as the sending data items in the implicit MOVE statement is determined by the following conditions"
  `OK  §1985:VII.3.8.4 GR12  (3.8.4 General Rules)  the text is in GR12`
- `§1985:VII.3.8.4 GR11`: "will indicate the number of character positions in the record just read"
  `OK  §1985:VII.3.8.4 GR11  (3.8.4 General Rules)  the text is in GR11`
- `§1985:VII.3.8.3 SR2`: "Record descriptions for the file must not describe records which contain a lesser number of character positions than that specified by integer-2"
  `OK  §1985:VII.3.8.3 SR2  (3.8.3 Syntax Rules)  the text is in SR2`
- `§1985:VII.4.4.4 GR13`: "the record is truncated on the right to the maximum size"
  `OK  §1985:VII.4.4.4 GR13  (4.4.4 General Rules)  the text is in GR13`
- `§1985:VII.4.4.4 GR8`: "determined by rules specified in the RECORD clause"
  `OK  §1985:VII.4.4.4 GR8  (4.4.4 General Rules)  the text is in GR8`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b6/src/rvar.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RVAR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FW ASSIGN TO "rvar.dat".
           SELECT FR ASSIGN TO "rvar.dat" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD FW RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
             DEPENDING ON DW.
       01 RW PIC X(20).
       FD FR RECORD IS VARYING IN SIZE FROM 2 TO 20 CHARACTERS
             DEPENDING ON DR.
       01 R6 PIC X(6).
       01 R10 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 DW PIC 99.
       01 DR PIC 99.
       01 W PIC X(20).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT FW.
           MOVE "ABCDEFGHIJKLMNOPQRST" TO RW.
           MOVE 15 TO DW. WRITE RW.
           CLOSE FW.
           OPEN INPUT FR.
           MOVE ALL "*" TO W.
           READ FR INTO W AT END DISPLAY "EOF".
           DISPLAY "FS=" FS " DR=" DR " W=[" W "] R10=[" R10 "]".
           CLOSE FR.
           STOP RUN.
```

**Expected** under the ruling beside this note (`§1985:VII.3.8.4 GR11`, `§1985:VII.3.8.4 GR12`): `FS=00 DR=15 W=[ABCDEFGHIJKLMNO     ]`. **Observed** at `--std 85` (exit 0): `FS=00 DR=15 W=[ABCDEFGHIJ          ] R10=[ABCDEFGHIJ]`; at `--std 2023` (exit 0): identical.
