      *>--- conformance-case
      *> id: g85_bf38_04
      *> rules: [1989-AR-A.2.18.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.18.3 AR1 - argument-1 of class numeric: a signed
      *> DISPLAY item, a COMPUTATIONAL item, a numeric literal and an
      *> arithmetic expression (§1989:A.2.2 numeric argument) are each
      *> accepted and give the integer portion (§1989:A.2.18.4 RV1,
      *> §1989:A.2.18.4 RV2, §1989:A.2.18.4 RV3).
      *> A refusal of any numeric form or a wrong value fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3804.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P           PIC S9V9    VALUE +1.5.
       01  N           PIC S9V9    VALUE -1.5.
       01  C           PIC S9(4)   COMP VALUE +42.
       01  R           PIC S9(3).
       01  E           PIC -999.
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION INTEGER-PART (P).
           MOVE R TO E.
           DISPLAY "IP(+1.5)    " E.
           COMPUTE R = FUNCTION INTEGER-PART (N).
           MOVE R TO E.
           DISPLAY "IP(-1.5)    " E.
           COMPUTE R = FUNCTION INTEGER-PART (C).
           MOVE R TO E.
           DISPLAY "IP(COMP 42) " E.
           COMPUTE R = FUNCTION INTEGER-PART (-7.9).
           MOVE R TO E.
           DISPLAY "IP(-7.9)    " E.
           COMPUTE R = FUNCTION INTEGER-PART (P + 2.25).
           MOVE R TO E.
           DISPLAY "IP(P+2.25)  " E.
           COMPUTE R = FUNCTION INTEGER-PART (0).
           MOVE R TO E.
           DISPLAY "IP(0)       " E.
           STOP RUN.
