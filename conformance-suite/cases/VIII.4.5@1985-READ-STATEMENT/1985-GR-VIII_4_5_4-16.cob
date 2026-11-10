      *>--- conformance-case
      *> id: 1985-GR-VIII_4_5_4-16
      *> rules: [1985-GR-VIII.4.5.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR16 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RLCONF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LONG-FILE ASSIGN TO "RELDATA"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT-W.
           SELECT SHORT-FILE ASSIGN TO "RELDATA"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT-R.
       DATA DIVISION.
       FILE SECTION.
       FD  LONG-FILE.
       01  LONG-REC         PIC X(20).
       FD  SHORT-FILE.
       01  SHORT-REC        PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT-W        PIC XX.
       01  WS-STAT-R        PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LONG-FILE.
           MOVE "ABCDEFGHIJKLMNOPQRST" TO LONG-REC.
           WRITE LONG-REC.
           CLOSE LONG-FILE.
           DISPLAY "WRITTEN " WS-STAT-W.
           OPEN INPUT SHORT-FILE.
           DISPLAY "OPEN " WS-STAT-R.
           STOP RUN.
