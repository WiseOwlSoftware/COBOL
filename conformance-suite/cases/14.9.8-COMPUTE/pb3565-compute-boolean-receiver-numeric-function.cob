*>--- conformance-case
*> id: pb3565-compute-boolean-receiver-numeric-function
*> rules: [GR-14.9.8.4-3, SR-8.8.2-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3565 - the other side of the sole-function rule: only a
      *> function of class boolean is a boolean expression. FUNCTION LENGTH
      *> is an integer function (ISO 15.2 5), so a boolean COMPUTE receiver
      *> may not take it as its whole boolean-expression-1 (ISO 8.8.2: a
      *> boolean expression begins with an identifier that references a
      *> boolean data item, a boolean literal, ...); COBOLNET1511.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3565NF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8 USAGE BIT PICTURE 1(8).
       01 X8 PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE B8 = FUNCTION LENGTH(X8)
           STOP RUN.
