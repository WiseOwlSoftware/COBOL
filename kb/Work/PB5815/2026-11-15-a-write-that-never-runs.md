**2026-11-15, a WRITE ... ADVANCING that never executes joins this mechanism (the Mythos lane's batch-7 code reads, mechanism 5; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-7.md`).**

**Ruling: UPHELD (b), harm wrong_answer.** The mechanism of this note by another trigger: one WRITE ... ADVANCING that never executes makes the file a print file, and two plain records then read back as three wrong ones. The row is `1985-GR-VII.4.7.4-12`. The ruling's author ran it. The checker's confidence: high.

**The checker's evidence.** `p6dead`: two plain WRITEs (WXYZ, PQRS) and one WRITE AFTER ADVANCING behind `IF SW = 1` (never true). The file is LF WXYZ LF PQRS LF (11 bytes: `0A 57 58 59 5A 0A 50 51 52 53 0A`) and reads back as three records `[LF WXY] 00`, `[Z LF PQ] 00`, `[RS LF ] 04`, then AT END: records no WRITE released (`§1985:VII.4.7.4 GR12`). The output is identical at `--std 2023`. The harm is this note's read-back; a fix that returns a print file's records as written (or refuses with 37 at OPEN) resolves this row too.

**Lead for the fix (marked as a lead, from the source text, not yet checked by a fix).** The compiler classifies the file from the source text: `SequentialIoBinder.BindWrite` sets `FileModel.WrittenWithAdvancing` from any WRITE ADVANCING in the program, executed or not (kb/Work PB5201), while `docs/conformance/7/DOC-A.1-146.md` (c') says "a record sequential file written with an ADVANCING phrase"; the fix should either honour or restate that wording.

**Where** (the checker's reading): `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SequentialIoBinder.cs#BindWrite` (static classification); `src/WiseOwl.Cobol.Runtime/IO/SequentialConnector.cs#IsRecordPrintStream` (framing READ does not strip).

**Row added.** `1985-GR-VII.4.7.4-12`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here. The 2023 rows the checker names: `GR-14.9.51.4-17`, `GR-14.9.51.4-18`, `GR-12.4.5.5.3-2`, `GR-12.4.5.10.3-3`; a registrar re-records them non-conforming.

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VII.4.7.4 GR12`: "The successor relationship of a sequential file is established by the order of execution of WRITE statements when the file is created"
  `OK  §1985:VII.4.7.4 GR12  (4.7.4 General Rules)  the text is in GR12`
- `§1985:VII.4.7.4 GR16`: "automatic advancing will be provided by the implementor to act as if the user had specified AFTER ADVANCING 1 LINE"
  `OK  §1985:VII.4.7.4 GR16  (4.7.4 General Rules)  the text is in GR16`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b7/src/p6dead.cob`; `--std 85` and `--std 2023`: exit 0, `O 00`, `REC 01 [LF WXY] 00`, `REC 02 [Z LF PQ] 00`, `REC 03 [RS LF space] 04`, `COUNT 03 10`, a notice that the file is 11 bytes, not a whole multiple of 4):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P6DEAD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p6dead.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F LABEL RECORDS ARE STANDARD.
       01 F-R PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 SW PIC 9 VALUE 0.
       01 N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT F.
           MOVE "WXYZ" TO F-R. WRITE F-R.
           MOVE "PQRS" TO F-R. WRITE F-R.
           IF SW = 1
               WRITE F-R AFTER ADVANCING 1 LINE.
           CLOSE F.
           OPEN INPUT F.
           DISPLAY "O " FS.
       R.
           READ F AT END GO TO E.
           ADD 1 TO N.
           DISPLAY "REC " N " [" F-R "] " FS.
           IF N < 9 GO TO R.
       E.
           DISPLAY "COUNT " N " " FS.
           CLOSE F.
           STOP RUN.
```
