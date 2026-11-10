      *>--- conformance-case
      *> id: 1985-GR-X_4_3_4-1
      *> rules: [1985-GR-X.4.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.3.4 GR1 (THE DATA DESCRIPTION ENTRY IN THE
      *> INTER-PROGRAM COMMUNICATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IPCDDE.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  WS-NAME PIC X(5) VALUE "HELLO".
           05  WS-NUM  PIC 9(3) VALUE 42.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-NAME.
           DISPLAY WS-NUM.
           STOP RUN.
