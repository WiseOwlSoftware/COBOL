      *>--- conformance-case
      *> id: pb4858-debug-sum-counter-target
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4858. X3.23-1985 USE FOR DEBUGGING syntax rule 9:
      *> "Identifier-1 must not reference any data item defined in the
      *> Report Section except sum counters", so a sum counter is a legal
      *> identifier-1. General rule 5 d: the ALL REFERENCES OF section
      *> runs "immediately after execution of" any statement that
      *> "explicitly references identifier-1"; rule 24 b: DEBUG-NAME
      *> "contains the name of identifier-1". TOT is the sum counter of
      *> the control footing's entry (ISO/IEC 1989:2023 13.18.54.4 GR12:
      *> procedure division statements may alter it). A: MOVE 7 TO TOT.
      *> B: ADD 1 TO TOT. C: IF TOT = 8 reads TOT in its condition, and
      *> its imperative statement is a statement of its own (rule 7), so
      *> "EIGHT" is shown before the section runs after the IF.
      *> Why each leg can fail: a binder that refuses the counter as a
      *> target, or a target whose item is not the counter's register,
      *> runs no section for these three statements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4858SC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb4858-sc.lst".
       DATA DIVISION.
       FILE SECTION.
       FD PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 AMT PIC 9 VALUE 3.
       REPORT SECTION.
       RD R1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES
           HEADING 1 FIRST DETAIL 2 LAST DETAIL 18.
       01 DET TYPE DETAIL LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE AMT.
       01 TYPE CONTROL FOOTING FINAL LINE PLUS 1.
          02 TOT COLUMN 1 PIC 99 SUM AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF TOT.
       DB1-B.
           DISPLAY "  DB1 " DEBUG-NAME (1:3).
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           DISPLAY "A: MOVE 7 TO TOT".
           MOVE 7 TO TOT.
           DISPLAY "B: ADD 1 TO TOT".
           ADD 1 TO TOT.
           DISPLAY "C: IF TOT = 8".
           IF TOT = 8 DISPLAY "EIGHT".
           DISPLAY "END".
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
