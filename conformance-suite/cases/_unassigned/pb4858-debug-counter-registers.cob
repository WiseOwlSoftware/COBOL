      *>--- conformance-case
      *> id: pb4858-debug-counter-registers
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4858. X3.23-1985 USE FOR DEBUGGING general format:
      *> identifier-1 is any data item the Procedure Division may
      *> reference except a Report Section item that is no sum counter
      *> (syntax rule 9), and LINAGE-COUNTER, LINE-COUNTER and
      *> PAGE-COUNTER are special-register data items, so each is a
      *> legal identifier-1 (ISO/IEC 1989:2023 8.4.3.14.4 GR1 and
      *> 8.4.3.15.4 GR1: "a temporary unsigned integer data item").
      *> General rule 5 d: the ALL REFERENCES OF section runs
      *> "immediately after execution of" any statement that
      *> explicitly references identifier-1; rule 6 c: identifier-1
      *> alone runs it after a statement that changes the item;
      *> rule 24: DEBUG-NAME holds the name of identifier-1 and
      *> DEBUG-CONTENTS the content of the item with no conversion
      *> (rule 11). LINAGE-COUNTER is a DISPLAY integer of the
      *> LINAGE page size's digits (LINAGE 10: two), so its content is
      *> the two digits of the value.
      *> A: MOVE 7 TO PAGE-COUNTER changes it (DB2, rule 6 c), and the
      *> counter is not read. B: MOVE PAGE-COUNTER TO N reads it and
      *> changes nothing (DB2 does not run). C: MOVE LINAGE-COUNTER TO N
      *> reads it (DB1 runs; OPEN OUTPUT sets the counter to one,
      *> ISO/IEC 1989:2023 13.18.34.4 GR7 d, and the WRITE AFTER
      *> ADVANCING 2 LINES raises it by two, GR7 c 2: content 03, and
      *> the WRITE references no counter, so it runs no section). D: the IF reads
      *> LINE-COUNTER (DB3 runs after the IF, and its imperative
      *> statement, a statement of its own, runs first).
      *> Why each leg can fail: a binder that refuses a counter as a
      *> target runs no section; a planner that matches places alone
      *> never sees the reads, which are register nodes; a register
      *> with no DEBUG-CONTENTS image fills the section's DISPLAY with
      *> the wrong digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4858CT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LF ASSIGN TO "pb4858-ct-l.tmp".
           SELECT PRT ASSIGN TO "pb4858-ct.lst".
       DATA DIVISION.
       FILE SECTION.
       FD LF LINAGE IS 10 LINES.
       01 LREC PIC X(4).
       FD PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       01 AMT PIC 9 VALUE 3.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 18.
       01 DET TYPE DETAIL LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF LINAGE-COUNTER.
       DB1-B.
           DISPLAY "  DB1 " DEBUG-NAME (1:14) " " DEBUG-CONTENTS (1:2).
       DB2 SECTION.
           USE FOR DEBUGGING ON PAGE-COUNTER.
       DB2-B.
           DISPLAY "  DB2 " DEBUG-NAME (1:12).
       DB3 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF LINE-COUNTER.
       DB3-B.
           DISPLAY "  DB3 " DEBUG-NAME (1:12).
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT LF PRT.
           INITIATE R1.
           GENERATE DET.
           DISPLAY "A: MOVE 7 TO PAGE-COUNTER".
           MOVE 7 TO PAGE-COUNTER.
           DISPLAY "B: MOVE PAGE-COUNTER TO N".
           MOVE PAGE-COUNTER TO N.
           MOVE "X" TO LREC.
           WRITE LREC AFTER ADVANCING 2 LINES.
           DISPLAY "C: MOVE LINAGE-COUNTER TO N".
           MOVE LINAGE-COUNTER TO N.
           DISPLAY "D: IF LINE-COUNTER > 0".
           IF LINE-COUNTER > 0 DISPLAY "LINE".
           DISPLAY "END".
           TERMINATE R1.
           CLOSE LF PRT.
           STOP RUN.
