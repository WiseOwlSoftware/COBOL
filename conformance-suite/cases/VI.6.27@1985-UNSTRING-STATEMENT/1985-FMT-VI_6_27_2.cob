      *>--- conformance-case
      *> id: 1985-FMT-VI_6_27_2
      *> rules: [1985-FMT-VI.6.27.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.2 (THE UNSTRING STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNSTR85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-SRC   PIC X(8) VALUE "ABC  DEF".
       01  WS-A1    PIC X(5).
       01  WS-A2    PIC X(5).
       01  WS-D1    PIC X(1).
       01  WS-C1    PIC 9.
       01  WS-C2    PIC 9.
       01  WS-T     PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           UNSTRING WS-SRC DELIMITED BY ALL SPACE
               INTO WS-A1 DELIMITER IN WS-D1 COUNT IN WS-C1
                    WS-A2 COUNT IN WS-C2
               TALLYING IN WS-T
               ON OVERFLOW DISPLAY "OVERFLOW"
               NOT ON OVERFLOW DISPLAY "NO OVERFLOW"
           END-UNSTRING
           DISPLAY "[" WS-A1 "]" WS-C1
           DISPLAY "[" WS-A2 "]" WS-C2
           DISPLAY WS-T
           STOP RUN.
