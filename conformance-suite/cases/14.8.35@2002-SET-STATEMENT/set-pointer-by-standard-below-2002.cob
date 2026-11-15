      *>--- conformance-case
      *> id: set-pointer-by-standard-below-2002
      *> rules: [2002-GR-14.8.35.3-16]
      *> source-format: fixed
      *>---
      *> The OPTIONS paragraph (ARITHMETIC IS STANDARD) and the >>TURN
      *> directive are COBOL-2002 introductions, so COBOL-85 refuses
      *> the program that the 2002 golden
      *> set-pointer-by-standard-sdidi-amount accepts (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETPTRSB.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BUF PIC X VALUE "A".
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-RANGE-PTR CHECKING ON
           SET P TO ADDRESS OF BUF.
           SET P UP BY 1.
           STOP RUN.
