      *>--- conformance-case
      *> id: g85_bf38_01
      *> rules: [1989-FMT-A.2.7.2, 1989-AR-A.2.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.7.2 - FUNCTION ASIN ( argument-1 ) is accepted with an
      *> identifier, a literal and an arithmetic expression as argument-1
      *> (§1989:A.2.2 permits each); a compiler refusing the form fails.
      *> §1989:A.2.7.3 AR2 - the argument values -1 and +1 (the closed
      *> bounds of the domain) and values inside it are legal and yield
      *> the arcsine, -pi/2 and +pi/2 at the bounds: a compiler that treats
      *> a bound as outside the domain, or returns a wrong value, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3801.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-NEG1      PIC S9      VALUE -1.
       01  A-POS1      PIC S9      VALUE +1.
       01  A-HALF      PIC S9V9    VALUE +0.5.
       01  R           PIC S9V9(6).
       01  E           PIC -9.9(6).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION ASIN (A-NEG1).
           MOVE R TO E.
           DISPLAY "ASIN(-1)   " E.
           COMPUTE R ROUNDED = FUNCTION ASIN (A-POS1).
           MOVE R TO E.
           DISPLAY "ASIN(+1)   " E.
           COMPUTE R ROUNDED = FUNCTION ASIN (0).
           MOVE R TO E.
           DISPLAY "ASIN(0)    " E.
           COMPUTE R ROUNDED = FUNCTION ASIN (A-HALF).
           MOVE R TO E.
           DISPLAY "ASIN(0.5)  " E.
           COMPUTE R ROUNDED = FUNCTION ASIN (A-HALF - 1).
           MOVE R TO E.
           DISPLAY "ASIN(-0.5) " E.
           STOP RUN.
