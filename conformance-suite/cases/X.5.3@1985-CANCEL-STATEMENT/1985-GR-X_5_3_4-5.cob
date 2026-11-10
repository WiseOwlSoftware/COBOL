      *>--- conformance-case
      *> id: 1985-GR-X_5_3_4-5
      *> rules: [1985-GR-X.5.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.4 GR5 (THE CANCEL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "SUBA".
           CALL "SUBA".
           CALL "SUBB".
           CALL "SUBA".
           STOP RUN.
       END PROGRAM MAINPGM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-A.
           ADD 1 TO CNT.
           DISPLAY "A CNT=" CNT.
           CALL "SUBC".
       P-A-EXIT.
           EXIT PROGRAM.
       END PROGRAM SUBA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBB.
       PROCEDURE DIVISION.
       P-B.
           CANCEL "SUBA".
           DISPLAY "B CANCELLED A".
       P-B-EXIT.
           EXIT PROGRAM.
       END PROGRAM SUBB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-C.
           ADD 1 TO CNT.
           DISPLAY "C CNT=" CNT.
       P-C-EXIT.
           EXIT PROGRAM.
       END PROGRAM SUBC.
