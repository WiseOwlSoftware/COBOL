      *>--- conformance-case
      *> id: pb5638-external-linage-one-describer
      *> rules: [1985-GR-VII.3.7.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5638. §1985:VII.3.7.4 GR11: the file description
      *> entries associated with an external file connector "must have:
      *> a. A LINAGE clause, if any file description entry has a LINAGE
      *> clause." The first describer writes none and the second writes
      *> LINAGE IS 10 LINES: refused (COBOL6847, at the first entry,
      *> though the entry that writes the clause comes after it). Before
      *> the fix an entry with no LINAGE was skipped, so this compiled
      *> and one program paged the file the other saw as unformatted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5638L1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5638l1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           CALL "P5638L1S"
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5638L1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5638L1S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5638l1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LINAGE IS 10 LINES.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           EXIT PROGRAM.
       END PROGRAM P5638L1S.
