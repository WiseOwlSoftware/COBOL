      *>--- conformance-case
      *> id: g85_f33_01
      *> rules: [1989-AR-A.2.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.2 AR3 - an alphanumeric argument may be a data item of
      *> class alphabetic, a data item of class alphanumeric (an elementary
      *> item and a group item, whose class is alphanumeric) or a nonnumeric
      *> literal, and the size of the argument is used: REVERSE of each.
      *> Line 2 fails if the size of W-ALNUM (5, trailing spaces) is not
      *> used; line 3 fails if the group is not taken as alphanumeric; a
      *> refusal of any argument fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3301.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-ALPHA PIC A(4) VALUE "ABCD".
       01 W-ALNUM PIC X(5) VALUE "XY".
       01 W-GRP.
          05 W-G1 PIC X(2) VALUE "PQ".
          05 W-G2 PIC 9(2) VALUE 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" FUNCTION REVERSE (W-ALPHA) "]".
           DISPLAY "[" FUNCTION REVERSE (W-ALNUM) "]".
           DISPLAY "[" FUNCTION REVERSE (W-GRP) "]".
           DISPLAY "[" FUNCTION REVERSE ("LIT") "]".
           STOP RUN.
