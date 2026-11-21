      *>--- conformance-case
      *> id: pb5924-same-name-separate-programs-85
      *> rules: [1985-SR-X.1.3.8.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.1 (cite.py: OK): "at least one of those two programs must be directly or indirectly contained"
      *> Two separately compiled programs named DUPP, neither contained in another program, are refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       PROCEDURE DIVISION.
       P1.
           CALL "DUPP".
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DUPP.
       PROCEDURE DIVISION.
       P2.
           DISPLAY "FIRST DUPP".
           EXIT PROGRAM.
       END PROGRAM DUPP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DUPP.
       PROCEDURE DIVISION.
       P3.
           DISPLAY "SECOND DUPP".
           EXIT PROGRAM.
       END PROGRAM DUPP.
