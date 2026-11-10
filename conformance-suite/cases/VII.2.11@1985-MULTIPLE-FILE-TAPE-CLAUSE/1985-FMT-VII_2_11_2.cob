      *>--- conformance-case
      *> id: 1985-FMT-VII_2_11_2
      *> rules: [1985-FMT-VII.2.11.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.11.2 (THE MULTIPLE FILE TAPE CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MFTAPE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO TAPEA.
           SELECT F2 ASSIGN TO TAPEA.
       I-O-CONTROL.
           MULTIPLE FILE F1 POSITION 1 F2 POSITION 2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2 LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MULTIPLE FILE OK".
           STOP RUN.
