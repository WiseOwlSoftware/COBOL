      *>--- conformance-case
      *> id: pb5721-test-numval-c-category-rules-2014
      *> rules: [AR-15.68.3-1, AR-15.68.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5721 - §2014:15.87.2 AR1 (cite.py --check: OK): "The argument rules for the TEST-NUMVAL-C function are the
      *> same as those specified in 15.63, NUMVAL-C function, Arguments", and §2023:15.94.3 AR1 imports §2023:15.68.3 the same way.
      *> So from 2014 argument-1 shall be of CATEGORY alphanumeric or national (NE is numeric-edited: COBOL1627) and
      *> argument-2 shall hold no digit (the literal "U1": COBOL1627). COBOL-2002 asks only the class
      *> (pb5721-test-numval-c-class-rules-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5721N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE  PIC $$9.99.
       01 T   PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO NE
           MOVE FUNCTION TEST-NUMVAL-C(NE) TO T
           MOVE FUNCTION TEST-NUMVAL-C("U1 12.5" "U1") TO T
           STOP RUN.
