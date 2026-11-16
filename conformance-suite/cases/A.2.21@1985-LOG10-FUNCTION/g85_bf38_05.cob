      *>--- conformance-case
      *> id: g85_bf38_05
      *> rules: [1989-AR-A.2.21.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.21.3 AR1 - argument-1 of class numeric: a numeric
      *> literal, DISPLAY items (integer and fraction), a COMPUTATIONAL item
      *> and an arithmetic expression are each accepted and give the base-10
      *> logarithm (§1989:A.2.21.4 RV1), rounded to four places.
      *> A refusal of any numeric form or a wrong value fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3805.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  H           PIC 9(3)    VALUE 100.
       01  F           PIC V99     VALUE .01.
       01  C           PIC 9(4)    COMP VALUE 2.
       01  X           PIC 9       VALUE 1.
       01  R           PIC S9V9(4).
       01  E           PIC -9.9(4).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION LOG10 (1000).
           MOVE R TO E.
           DISPLAY "LOG10(1000) " E.
           COMPUTE R ROUNDED = FUNCTION LOG10 (H).
           MOVE R TO E.
           DISPLAY "LOG10(100)  " E.
           COMPUTE R ROUNDED = FUNCTION LOG10 (X * 10).
           MOVE R TO E.
           DISPLAY "LOG10(X*10) " E.
           COMPUTE R ROUNDED = FUNCTION LOG10 (F).
           MOVE R TO E.
           DISPLAY "LOG10(.01)  " E.
           COMPUTE R ROUNDED = FUNCTION LOG10 (C).
           MOVE R TO E.
           DISPLAY "LOG10(2)    " E.
           STOP RUN.
