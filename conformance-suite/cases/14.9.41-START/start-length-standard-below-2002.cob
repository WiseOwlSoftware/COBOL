*>--- conformance-case
*> id: start-length-standard-below-2002
*> rules: [GR-14.9.41.4-14]
*> source-format: free
*>---
      *> The OPTIONS paragraph (ARITHMETIC IS STANDARD) is a COBOL-2002
      *> introduction, so COBOL-85 refuses the program that the 2002
      *> golden start-length-standard-sdidi-amount accepts (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STARTSB.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
