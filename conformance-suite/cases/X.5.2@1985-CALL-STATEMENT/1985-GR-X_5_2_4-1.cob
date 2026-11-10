      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-1
      *> rules: [1985-GR-X.5.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR1 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPRG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(6) VALUE "SUBTWO".
       PROCEDURE DIVISION.
       P1.
           CALL "SUBTWO".
           CALL "SUBONE".
           CALL WS-NAME.
           MOVE "SUBONE" TO WS-NAME.
           CALL WS-NAME.
           DISPLAY "BACK IN MAIN".
           STOP RUN.
       END PROGRAM MAINPRG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBONE.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "IN SUBONE".
           EXIT PROGRAM.
       END PROGRAM SUBONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBTWO.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "IN SUBTWO".
           EXIT PROGRAM.
       END PROGRAM SUBTWO.
