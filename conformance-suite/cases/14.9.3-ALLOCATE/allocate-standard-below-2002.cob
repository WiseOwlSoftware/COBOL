      *>--- conformance-case
      *> id: allocate-standard-below-2002
      *> rules: [GR-14.9.3.4-1]
      *> source-format: fixed
      *>---
      *> The OPTIONS paragraph (ARITHMETIC IS STANDARD) and the >>TURN
      *> directive are COBOL-2002 introductions, so COBOL-85 refuses
      *> the program that the 2002 golden allocate-standard-sdidi-amount
      *> accepts (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALLOCSB.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
           ALLOCATE 3 CHARACTERS RETURNING P.
           STOP RUN.
