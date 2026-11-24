      *>--- conformance-case
      *> id: pb6073-testnumvalf-alphabetic-2002
      *> rules: [2002-AR-15.77.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6073 - §2002:15.77.2 AR1: "Argument-1 shall be an
      *>   alphanumeric or national literal or a data item of class
      *>   alphanumeric or national"; §2002:8.5.2.1: "An
      *>   alphabetic data item may be specified wherever an elementary
      *>   alphanumeric data item is permitted as an operand." So the PIC
      *>   A item TV is an admitted argument at COBOL-2002. TV holds
      *>   ABC, whose first character cannot begin a NUMVAL-F argument,
      *>   so the function returns the position of that first character
      *>   in error (§2002:15.77.3 RV1b): 1. It fails if the PIC A
      *>   argument is refused. COBOL-2014 withdrew the sentence
      *>   (§2014:E.2): l1-test-numval-f-alphabetic-item refuses the
      *>   source there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6073F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TV  PIC A(3) VALUE "ABC".
       01  N   PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION TEST-NUMVAL-F (TV) TO N
           DISPLAY N
           STOP RUN.
