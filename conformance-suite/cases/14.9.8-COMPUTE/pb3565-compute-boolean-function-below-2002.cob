*>--- conformance-case
*> id: pb3565-compute-boolean-function-below-2002
*> rules: [GR-14.9.8.4-3, GR-15.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3565 - the negative below the introducing edition of
      *> pb3565_compute_boolean_sole_identifier. ISO 15.2 2) boolean
      *> functions, and the function BOOLEAN-OF-INTEGER, belong to the 2002
      *> standard; at COBOL-85 the program is refused (the intrinsic's
      *> introduction, COBOL1502, and the boolean item's, COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3565B85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8 USAGE BIT PICTURE 1(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE B8 = FUNCTION BOOLEAN-OF-INTEGER(5, 8)
           DISPLAY B8
           STOP RUN.
