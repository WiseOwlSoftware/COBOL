      *>--- conformance-case
      *> id: pb4858-debug-counter-qualified
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4858, review round 1 R1-1. X3.23-1985 USE FOR
      *> DEBUGGING general rule 13 (XV.3.2.4): DEBUG-NAME holds the
      *> name that caused the section, "all qualifiers" included,
      *> separated by IN or OF. Two LINAGE files make LINAGE-COUNTER
      *> need its file-name (ISO/IEC 1989:2023 8.4.2.2.3 SR8), so the
      *> USE FOR DEBUGGING operand LINAGE-COUNTER OF LF is written with
      *> its qualifier and DEBUG-NAME is LINAGE-COUNTER OF LF, not the
      *> qualifier LF alone. Only the read of LF's counter runs the
      *> section; the read of LG's does not. A binder that takes the
      *> qualifier word as the name shows LF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4858CQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LF ASSIGN TO "pb4858-cq-l.tmp".
           SELECT LG ASSIGN TO "pb4858-cq-g.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD LF LINAGE IS 10 LINES.
       01 LREC PIC X(4).
       FD LG LINAGE IS 12 LINES.
       01 GREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF LINAGE-COUNTER OF LF.
       DB1-B.
           DISPLAY "  DB1 [" DEBUG-NAME "] [" DEBUG-CONTENTS (1:2) "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT LF LG.
           DISPLAY "G: MOVE LINAGE-COUNTER OF LG TO N".
           MOVE LINAGE-COUNTER OF LG TO N.
           DISPLAY "F: MOVE LINAGE-COUNTER OF LF TO N".
           MOVE LINAGE-COUNTER OF LF TO N.
           DISPLAY "END".
           CLOSE LF LG.
           STOP RUN.
