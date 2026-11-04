      *>--- conformance-case
      *> id: pb4577-search-enclosing-subscript-below-2023
      *> rules: [SR-14.9.37.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4609 (PB4577) - SEARCH H (GI), where GI subscripts the table G that encloses H, is a COBOL-2023 permission
      *> (ISO/IEC 1989:2023 14.9.37.3 SR2, SR3). The 2002 text, the 2009 committee draft and the 2014 FCD 1.0 (14.9.34.2 ALL FORMATS rule 1) say
      *> "Identifier-1 shall not be subscripted or reference modified", so at COBOL-85, COBOL-2002 and COBOL-2014 the program is
      *> refused with COBOL0900 (search-identifier-1-enclosing-subscript-2023).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4577N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-VALS PIC X(6) VALUE "123789".
       01  T REDEFINES T-VALS.
           05  G OCCURS 2 TIMES INDEXED BY GI.
               10  H PIC 9 OCCURS 3 TIMES INDEXED BY HI.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET GI TO 2.
           SET HI TO 1.
           SEARCH H (GI)
               AT END DISPLAY "NOT FOUND"
               WHEN H (GI, HI) > 7
                   DISPLAY "FOUND " H (GI, HI)
           END-SEARCH.
           STOP RUN.
