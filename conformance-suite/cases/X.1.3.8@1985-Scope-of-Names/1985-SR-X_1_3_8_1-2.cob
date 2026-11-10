      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8_1-2
      *> rules: [1985-SR-X.1.3.8.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.1 SR2 (Scope of Names). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G-FIRST IS GLOBAL PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "MID".
           CALL "P9".
           DISPLAY "OUTER DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MID.
       PROCEDURE DIVISION.
       MID-PARA.
           DISPLAY "MID CALLS C1".
           CALL "C1".
           CALL "M2".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C1 IS COMMON PROGRAM.
       PROCEDURE DIVISION.
       C1-PARA.
           DISPLAY "IN C1".
           IF G-FIRST = "Y"
               MOVE "N" TO G-FIRST
               CALL "C1SUB".
       C1-EXIT.
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C1SUB.
       PROCEDURE DIVISION.
       C1SUB-PARA.
           CALL "C1"
               ON EXCEPTION DISPLAY "C1 NOT IN C1SUB SCOPE"
               NOT ON EXCEPTION DISPLAY "C1 REACHED FROM C1SUB"
           END-CALL.
           EXIT PROGRAM.
       END PROGRAM C1SUB.
       END PROGRAM C1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. M2.
       PROCEDURE DIVISION.
       M2-PARA.
           DISPLAY "M2 CALLS C1".
           CALL "C1".
           CALL "M3".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. M3.
       PROCEDURE DIVISION.
       M3-PARA.
           DISPLAY "M3 CALLS C1".
           CALL "C1".
           EXIT PROGRAM.
       END PROGRAM M3.
       END PROGRAM M2.
       END PROGRAM MID.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P9.
       PROCEDURE DIVISION.
       P9-PARA.
           CALL "C1"
               ON EXCEPTION DISPLAY "C1 NOT IN P9 SCOPE"
               NOT ON EXCEPTION DISPLAY "C1 REACHED FROM P9"
           END-CALL.
           EXIT PROGRAM.
       END PROGRAM P9.
       END PROGRAM OUTER.
