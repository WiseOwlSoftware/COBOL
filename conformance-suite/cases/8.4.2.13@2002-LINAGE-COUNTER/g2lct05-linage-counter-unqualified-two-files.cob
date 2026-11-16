      *>--- conformance-case
      *> id: g2lct05-linage-counter-unqualified-two-files
      *> rules: [2002-SR-8.4.2.13.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.13.2 SR3 defers to §2002:8.4.1.1.2 SR6:
      *> "LINAGE-COUNTER shall be qualified if more than one file
      *> description entry containing a LINAGE clause has been specified
      *> in the source element." F1 and F2 both carry LINAGE in this
      *> source element, so the bare LINAGE-COUNTER is refused on the
      *> MOVE line; the same reference qualified is accepted in g2lct04.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LCT05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2LCT05A.TXT".
           SELECT F2 ASSIGN TO "G2LCT05B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LINAGE IS 20 LINES.
       01  R1 PIC X(20).
       FD  F2 LINAGE IS 30 LINES.
       01  R2 PIC X(20).
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1 F2.
           MOVE LINAGE-COUNTER TO N.
           DISPLAY "N=" N.
           CLOSE F1 F2.
           STOP RUN.
