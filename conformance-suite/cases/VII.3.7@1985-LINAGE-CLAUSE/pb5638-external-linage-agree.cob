      *>--- conformance-case
      *> id: pb5638-external-linage-agree
      *> rules: [1985-GR-VII.3.7.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5638, the control of its two refusals. §1985:VII.3.7.4
      *> GR11: every entry of an external file connector has a LINAGE
      *> clause (a.) with "The same corresponding external data items
      *> referenced by data-name-1" (c.) and the same integer-2 (b.).
      *> Both entries here write LINAGE IS LN1 LINES WITH FOOTING AT 8,
      *> LN1 the one item of the external record X1 at the same
      *> character positions in both programs: they agree, so the run
      *> unit translates and runs, and the main program displays DONE
      *> after the called one returns (it must fail if GR11 c. refused
      *> one item described in two programs as two items).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5638L3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5638l3.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LINAGE IS LN1 LINES WITH FOOTING AT 8.
       01  SF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  X1 IS EXTERNAL.
           05 LN1 PIC 99.
           05 LN2 PIC 99.
       PROCEDURE DIVISION.
       P.
           MOVE 10 TO LN1
           CALL "P5638L3S"
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5638L3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5638L3S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5638l3.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LINAGE IS LN1 LINES WITH FOOTING AT 8.
       01  SF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  X1 IS EXTERNAL.
           05 LN1 PIC 99.
           05 LN2 PIC 99.
       PROCEDURE DIVISION.
       P.
           EXIT PROGRAM.
       END PROGRAM P5638L3S.
