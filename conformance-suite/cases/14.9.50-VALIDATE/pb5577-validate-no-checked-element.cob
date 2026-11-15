      *>--- conformance-case
      *> id: pb5577-validate-no-checked-element
      *> rules: [GR-14.9.50.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:14.9.50.4 GR4: each stage is executed in order; an operand none of whose elements has a stage-one check
      *> (all alphanumeric) leaves every stage with nothing to do, so the statement compiles, raises nothing and execution
      *> continues. Checking is ON, so a wrongly emitted empty test would fail to compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5577N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-X.
          05 X-NAME PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-VALIDATE CHECKING ON
           MOVE "ABC" TO REC-X
           VALIDATE REC-X
           DISPLAY "after " REC-X
           STOP RUN.
