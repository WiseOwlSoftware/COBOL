      *>--- conformance-case
      *> id: pb5643_sum_size_error_raise_time
      *> rules: [GR-13.18.54.4-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5643 - when a size error in a SUM addition raises EC-REPORT-SUM-SIZE. §2023:13.18.54.4 GR3 (cite.py: OK; the 2014 text,
      *> 13.18.54.3 GR3, the same): "Each addition is tested for size error; if a size error occurs, the EC-REPORT-SUM-SIZE exception
      *> condition is set to exist". §2002:13.16.52.3 GR3 (cite.py: OK): "if a size error occurs, the associated size error indicator is set"
      *> and nothing more; its GR4 (cite.py: OK): "If the associated size error indicator is set, an EC-REPORT-SUM-SIZE exception condition is
      *> set to exist and the printable item is filled with spaces", when the entry is presented.
      *> DERIVATION. The counter S1 is PIC 9. GENERATE with WS-B 5 adds 5; GENERATE with WS-B 6 adds 6, a total of 11 that does not fit one
      *> digit: a size error. The condition is fatal and checked (>>TURN), so the declarative runs and the run unit ends after it.
      *>   COBOL-2014 and later: the condition exists at the second GENERATE's addition: the declarative shows RAISED 1 and GEN2 DONE is never reached.
      *>   COBOL-2002: the second GENERATE only sets the indicator (GEN2 DONE), and TERMINATE presents the control footing, whose entry S1 holds
      *>   the indicator: RAISED 1 follows, and TERM DONE is never reached.
      *> Each leg can fail: a raise at the addition in 2002 loses GEN2 DONE; no raise at the addition from 2014 shows GEN2 DONE.
       >>TURN EC-REPORT-SUM-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5643T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5643T.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WS-B    PIC 9     VALUE 0.
       01  WS-N    PIC 9     VALUE 0.
       REPORT SECTION.
       RD  R CONTROL IS FINAL.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE WS-B.
       01  CFT TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  S1 COLUMN 1 PIC 9 SUM WS-B.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-SUM-SIZE.
       H-P.
           ADD 1 TO WS-N
           DISPLAY "RAISED " WS-N.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 5 TO WS-B. GENERATE DL.
           DISPLAY "GEN1 DONE"
           MOVE 6 TO WS-B. GENERATE DL.
           DISPLAY "GEN2 DONE"
           TERMINATE R.
           DISPLAY "TERM DONE"
           CLOSE PRT.
           STOP RUN.
