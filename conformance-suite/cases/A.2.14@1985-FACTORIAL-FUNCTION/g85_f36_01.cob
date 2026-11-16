      *>--- conformance-case
      *> id: g85_f36_01
      *> rules: [1989-AR-A.2.14.3-1, 1989-FMT-A.2.14.2, 1989-RV-A.2.14.4-1, 1989-RV-A.2.14.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.14.3 AR1 - argument-1 an integer >= 0: zero, a
      *> literal, a data item and an integer arithmetic expression
      *> (§1989:A.2.2 AR4) are each accepted and evaluated.
      *> §1989:A.2.14.4 RV1 - zero returns 1 (line 1).
      *> §1989:A.2.14.4 RV2 - a positive argument returns its
      *> factorial: 1!=1, 5!=120, 10!=3628800, (10-7)!=6 (lines 2-5).
      *> A leg fails if 0! is not 1, if a data-item or expression
      *> argument is refused, or if any factorial is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3601.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 99 VALUE 10.
       01  R  PIC 9(7).
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION FACTORIAL (0).
           DISPLAY R.
           COMPUTE R = FUNCTION FACTORIAL (1).
           DISPLAY R.
           COMPUTE R = FUNCTION FACTORIAL (5).
           DISPLAY R.
           COMPUTE R = FUNCTION FACTORIAL (N).
           DISPLAY R.
           COMPUTE R = FUNCTION FACTORIAL (N - 7).
           DISPLAY R.
           STOP RUN.
