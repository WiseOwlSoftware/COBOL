      *>--- conformance-case
      *> id: 1985-FMT-VI_5_5_2
      *> rules: [1985-FMT-VI.5.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.5.2 (THE DATA-NAME OR FILLER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witness-drafting
      *> pass (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILLER1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  WS-A            PIC X(2) VALUE "AA".
           05  FILLER          PIC X(2) VALUE "--".
           05                  PIC X(2) VALUE "BB".
           05  FILLER          PIC X    VALUE "Y".
               88  IS-YES      VALUE "Y".
           05                  PIC 9    VALUE 3.
               88  IS-THREE    VALUE 3.
           05  WS-Z            PIC X(2) VALUE "ZZ".
       01  FILLER.
           05  WS-INNER        PIC X(3) VALUE "XYZ".
           05  FILLER          PIC X(3) VALUE "123".
       01.
           05  WS-LAST         PIC X(3) VALUE "END".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-REC.
           DISPLAY WS-A WS-Z.
           IF IS-YES
               DISPLAY "FILLER CONDITION TRUE"
           ELSE
               DISPLAY "FILLER CONDITION FALSE".
           IF IS-THREE
               DISPLAY "UNNAMED CONDITION TRUE"
           ELSE
               DISPLAY "UNNAMED CONDITION FALSE".
           MOVE "N4" TO WS-REC (7:2).
           IF IS-YES
               DISPLAY "FILLER CONDITION TRUE"
           ELSE
               DISPLAY "FILLER CONDITION FALSE".
           IF IS-THREE
               DISPLAY "UNNAMED CONDITION TRUE"
           ELSE
               DISPLAY "UNNAMED CONDITION FALSE".
           DISPLAY WS-INNER WS-LAST.
           STOP RUN.
