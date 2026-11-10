      *>--- conformance-case
      *> id: 1985-GR-XI_4_1_4-13
      *> rules: [1985-GR-XI.4.1.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR13 (THE MERGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGSEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "IN1".
           SELECT IN2 ASSIGN TO "IN2".
           SELECT MW ASSIGN TO "MW".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  R1 PIC X(3).
       FD  IN2.
       01  R2 PIC X(3).
       SD  MW.
       01  MREC.
           05  MK PIC X(3).
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           OPEN OUTPUT IN1.
           MOVE "AAA" TO R1.
           WRITE R1.
           MOVE "CCC" TO R1.
           WRITE R1.
           CLOSE IN1.
           OPEN OUTPUT IN2.
           MOVE "BBB" TO R2.
           WRITE R2.
           CLOSE IN2.
           MERGE MW ON ASCENDING KEY MK
               USING IN1 IN2
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       OUT-PROC SECTION 60.
       O1.
           RETURN MW AT END GO TO O9.
           DISPLAY MK.
           GO TO O1.
       O9.
           EXIT.
