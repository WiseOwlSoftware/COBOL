      *>--- conformance-case
      *> id: pb5924-same-name-nested-programs-85
      *> rules: [1985-SR-X.1.3.8.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.1 (cite.py: OK): "at least one of those two programs must be directly or indirectly contained"
      *> Two nested programs named INNER, each in a different separately compiled program, are legal;
      *> CALL, CANCEL and END PROGRAM reference them and each CALL reaches its own container's INNER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER1.
       PROCEDURE DIVISION.
       P1.
           CALL "INNER".
           CANCEL "INNER".
           CALL "OUTER2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       P2.
           DISPLAY "INNER OF OUTER1".
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM OUTER1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER2.
       PROCEDURE DIVISION.
       P3.
           CALL "INNER".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       P4.
           DISPLAY "INNER OF OUTER2".
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM OUTER2.
