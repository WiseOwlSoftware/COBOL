      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-16
      *> rules: [1985-GR-X.5.2.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR16 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-FLAG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P-MAIN.
           IF WS-FLAG = 1
               CALL "SUBPGM"
                   ON OVERFLOW DISPLAY "OVERFLOW"
               END-CALL
               DISPLAY "AFTER CALL"
           END-IF.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM MAINPGM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       PROCEDURE DIVISION.
       P-SUB.
           DISPLAY "IN SUB".
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
