      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_8_4_4-4
      *> rules: [1985-GR-IV.4.3.8.4.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.4 GR4 (Reference Modification). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witness-drafting
      *> pass (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFMOD.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X PIC X(10) VALUE "ABCDEFGHIJ".
       01  WS-L PIC X(10) VALUE "klmnopqrst".
       01  WS-P PIC 99 VALUE 2.
       01  WS-N PIC 99 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-X (3:4).
           DISPLAY WS-X (8:).
           DISPLAY WS-X (1:1).
           DISPLAY WS-X (10:1).
           DISPLAY WS-X (WS-P + 1:WS-N * 2).
           DISPLAY WS-X (WS-P * 4:).
           DISPLAY FUNCTION UPPER-CASE (WS-L) (WS-P:WS-N).
           DISPLAY FUNCTION UPPER-CASE (WS-L) (9:).
           MOVE "**" TO WS-X (WS-N + 1:WS-P).
           DISPLAY WS-X "|".
           MOVE "#" TO WS-X (9:).
           DISPLAY WS-X "|".
           STOP RUN.
