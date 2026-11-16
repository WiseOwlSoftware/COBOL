      *>--- conformance-case
      *> id: pb5615-alphabetic-operands-2002
      *> rules: [2002-SR-14.8.4.2-1, 2002-SR-14.8.44.2-2, 2002-AR-15.75.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5615 - §2002:8.5.2.1: "An alphabetic data item may be
      *>   specified wherever an elementary alphanumeric data item is
      *>   permitted as an operand." Three rules name the alphanumeric
      *>   operand and none prohibits an alphabetic one at COBOL-2002:
      *>   §2002:14.8.4.2 SR1 "Identifier-1 shall be defined as an
      *>   alphanumeric, national, or program-pointer data item" (CALL NM,
      *>   NM PIC A naming a program that does not exist, so the ON
      *>   EXCEPTION phrase runs: NOT-FOUND); §2002:14.8.44.2 SR2
      *>   "Identifier-1, identifier-2, identifier-3, and identifier-5
      *>   shall reference data items of category alphanumeric or
      *>   national" (UNSTRING NM DELIMITED BY "S" INTO P1 P2 splits
      *>   NOSUCHPG at the S: "NO" then "UCHPG"); §2002:15.75.2 AR1
      *>   "Argument-1 shall be an alphanumeric or national literal or a
      *>   data item of class alphanumeric or national" (TEST-NUMVAL of
      *>   the PIC A item ABC: the first character is invalid, so 1).
      *>   Expected: NOT-FOUND, NO  |UCHPG, 1. It fails if any of the
      *>   three operands is refused. COBOL-2014 withdrew the sentence
      *>   (§2014:E.2): the diagnostics file
      *>   pb5615-alphabetic-operands-refused refuses the source there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5615OP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NM  PIC A(8) VALUE "NOSUCHPG".
       01  TV  PIC A(3) VALUE "ABC".
       01  P1  PIC X(4).
       01  P2  PIC X(5).
       01  N   PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL NM
               ON EXCEPTION DISPLAY "NOT-FOUND"
               NOT ON EXCEPTION DISPLAY "FOUND"
           END-CALL
           UNSTRING NM DELIMITED BY "S" INTO P1 P2
           DISPLAY P1 "|" P2
           MOVE FUNCTION TEST-NUMVAL (TV) TO N
           DISPLAY N
           STOP RUN.
