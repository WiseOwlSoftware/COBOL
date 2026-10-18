*>--- conformance-case
*> id: pb674-floating-point-literal-85
*> rules: [SR-8.3.3.3.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB674 - the floating-point numeric literal (8.3.3.3.3 1), "formed from two fixed-point numeric
      *> literals separated by the letter 'E' without intervening spaces") is a COBOL-2002 introduction (VCR row 7.37,
      *> derived): COBOL-85's numeric literal is the fixed-point form alone. This program declares no floating-point
      *> data item, so nothing but the literal can draw the diagnostic: at --std 85 each of the two literals
      *> below (one in a VALUE clause, one as a MOVE source) draws COBOLNET0900. The
      *> accepted twin is pb674_floating_point_literal_2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB674FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(5)V99 VALUE 1.5E2.
       01 B PIC 9(5)V99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 2.5E1 TO B
           STOP RUN.
