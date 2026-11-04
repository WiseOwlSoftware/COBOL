      *>--- conformance-case
      *> id: pb4577-search-all-enclosing-subscript-below-2023
      *> rules: [SR-14.9.37.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4609 (PB4577) - the SEARCH ALL twin of pb4577-search-enclosing-subscript-below-2023: both formats print the same
      *> identifier-1 operand (14.9.37.3 SR1-SR3 are ALL-FORMATS rules) and resolve it in the one place, so SEARCH ALL H (GI)
      *> is a COBOL-2023 permission as SEARCH H (GI) is. Refused at COBOL-85, COBOL-2002 and COBOL-2014 with COBOL0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4577N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-VALS PIC X(6) VALUE "123789".
       01  T REDEFINES T-VALS.
           05  G OCCURS 2 TIMES INDEXED BY GI.
               10  H PIC 9 OCCURS 3 TIMES ASCENDING KEY IS H
                   INDEXED BY HI.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET GI TO 2.
           SEARCH ALL H (GI)
               AT END DISPLAY "NOT FOUND"
               WHEN H (GI, HI) = 9
                   DISPLAY "FOUND " H (GI, HI)
           END-SEARCH.
           STOP RUN.
